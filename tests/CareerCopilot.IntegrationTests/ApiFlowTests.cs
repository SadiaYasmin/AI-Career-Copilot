using System.Net;
using System.Text;
using System.Text.Json;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Application.Features.Auth.Dtos;
using CareerCopilot.Application.Features.CareerRoadmaps.Dtos;
using CareerCopilot.Application.Features.Copilot.Dtos;
using CareerCopilot.Application.Features.Dashboard.Get;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Application.Features.JobMatching.Dtos;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Application.Features.Profiles.Dtos;
using CareerCopilot.Application.Features.RecruiterReadiness.Get;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Application.Features.SkillGaps.Get;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Domain.Enums;

namespace CareerCopilot.IntegrationTests;

public sealed class ApiFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly ApiClient _client;

    public ApiFlowTests()
    {
        _client = new ApiClient(_factory.CreateClient());
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task UnauthenticatedRequestsAreRejected()
    {
        var (status, _) = await _client.GetAsync<object>("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task AuthFlow_Register_Login_Me()
    {
        var (status, envelope) = await RegisterAsync("register@test.dev", "Password123!", "Test User");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.NotNull(envelope);
        _client.Token = envelope!.Data.Token;

        var (loginStatus, loginEnvelope) = await _client.PostAsync<AuthResponse>("/api/auth/login",
            new { email = "register@test.dev", password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginStatus);
        Assert.Equal("register@test.dev", loginEnvelope?.Data.User.Email);

        var (meStatus, meEnvelope) = await _client.GetAsync<UserDto>("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meStatus);
        Assert.Equal("register@test.dev", meEnvelope?.Data.Email);
    }

    [Fact]
    public async Task DuplicateRegistrationIsRejected()
    {
        await RegisterAsync("dup@test.dev", "Password123!", "Dup User");

        var (status, _) = await _client.PostAsync<AuthResponse>("/api/auth/register",
            new { email = "dup@test.dev", password = "Password123!", fullName = "Dup User" });

        Assert.Equal(HttpStatusCode.Conflict, status);
    }

    [Fact]
    public async Task LoginWithWrongPasswordIsRejected()
    {
        await RegisterAsync("wrong@test.dev", "Password123!", "Wrong User");

        var (status, _) = await _client.PostAsync<AuthResponse>("/api/auth/login",
            new { email = "wrong@test.dev", password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Profile_Update_And_Get()
    {
        await registerDefaultUser();

        var profile = BuildSampleProfile();
        var (putStatus, putEnvelope) = await _client.PutAsync<ProfileDto>("/api/profile", profile);
        Assert.Equal(HttpStatusCode.OK, putStatus);
        Assert.Equal("Test User", putEnvelope?.Data.FullName);
        Assert.Equal(4, putEnvelope?.Data.Skills.Count);

        var (getStatus, getEnvelope) = await _client.GetAsync<ProfileDto>("/api/profile");
        Assert.Equal(HttpStatusCode.OK, getStatus);
        Assert.Equal("Senior Software Engineer", getEnvelope?.Data.TargetRole);
    }

    [Fact]
    public async Task Resume_Upload_List_Content_Default_Delete_Flow()
    {
        await registerDefaultUser();

        var text = "# Test Resume\nSummary: Backend engineer with 5 years experience.\n\n" +
                   "Skills: ASP.NET Core, PostgreSQL, C#, REST APIs\n\n" +
                   "Experience\n- Built and shipped a payment service used by 100k customers.\n" +
                   "- Reduced p95 latency by 40%.\n\nEducation\nBSc Computer Science";

        var (uploadStatus, uploadText) = await _client.PostFileAsync(
            "/api/resumes", "resume.txt", "text/plain", Encoding.UTF8.GetBytes(text));
        Assert.Equal(HttpStatusCode.OK, uploadStatus);
        var uploaded = Deserialize<ApiEnvelope<ResumeDto>>(uploadText);
        Assert.True(uploaded?.Success);
        Assert.NotNull(uploaded);
        var resumeId = uploaded!.Data.Id;

        var (listStatus, listEnvelope) = await _client.GetAsync<PagedResult<ResumeDto>>("/api/resumes");
        Assert.Equal(HttpStatusCode.OK, listStatus);
        Assert.Single(listEnvelope!.Data.Items);

        var (contentStatus, contentText) = await _client.GetRawAsync($"/api/resumes/{resumeId}/content");
        Assert.Equal(HttpStatusCode.OK, contentStatus);
        Assert.Contains("ASP.NET Core", contentText);

        var (setDefaultStatus, setDefaultEnvelope) =
            await _client.PostAsync<ResumeDto>($"/api/resumes/{resumeId}/set-default");
        Assert.Equal(HttpStatusCode.OK, setDefaultStatus);
        Assert.True(setDefaultEnvelope?.Data.IsDefault);

        var (analyzeStatus, analyzeEnvelope) =
            await _client.PostAsync<ResumeAnalysisDto>($"/api/resumes/{resumeId}/analyze");
        Assert.Equal(HttpStatusCode.OK, analyzeStatus);
        Assert.InRange(analyzeEnvelope!.Data.Score, 0, 100);
        Assert.False(analyzeEnvelope.Data.UsedAi);

        var (deleteStatus, _) = await _client.DeleteAsync<object>($"/api/resumes/{resumeId}");
        Assert.Equal(HttpStatusCode.OK, deleteStatus);

        var (emptyStatus, emptyEnvelope) = await _client.GetAsync<PagedResult<ResumeDto>>("/api/resumes");
        Assert.Equal(HttpStatusCode.OK, emptyStatus);
        Assert.Empty(emptyEnvelope!.Data.Items);
    }

    [Fact]
    public async Task Job_Create_Analyze_Match_SkillGaps_CoverLetter_Flow()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (createStatus, createEnvelope) = await _client.PostAsync<JobDto>("/api/jobs", new
        {
            title = "Senior Backend Engineer",
            companyName = "Example Corp",
            location = "Remote",
            employmentType = "Full-time",
            description = "We need a senior engineer with strong ASP.NET Core, C#, PostgreSQL and REST API skills " +
                           "to build scalable backend services. 5+ years experience preferred, cloud experience a plus.",
            sourceUrl = "https://example.com/jobs/1"
        });
        Assert.Equal(HttpStatusCode.OK, createStatus);
        var jobId = createEnvelope!.Data.Id;

        var (getStatus, getEnvelope) = await _client.GetAsync<JobDetailDto>($"/api/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.OK, getStatus);
        Assert.Equal("Example Corp", getEnvelope?.Data.CompanyName);

        var (analyzeStatus, analyzeEnvelope) = await _client.PostAsync<JobDetailDto>($"/api/jobs/{jobId}/analyze");
        Assert.Equal(HttpStatusCode.OK, analyzeStatus);
        Assert.True(analyzeEnvelope?.Data.IsAnalyzed);

        var (matchStatus, matchEnvelope) = await _client.PostAsync<JobMatchDto>($"/api/jobs/{jobId}/match");
        Assert.Equal(HttpStatusCode.OK, matchStatus);
        Assert.InRange(matchEnvelope!.Data.OverallScore, 0, 100);

        var (getMatchStatus, getMatchEnvelope) = await _client.GetAsync<JobMatchDto>($"/api/jobs/{jobId}/match");
        Assert.Equal(HttpStatusCode.OK, getMatchStatus);
        Assert.Equal(matchEnvelope.Data.OverallScore, getMatchEnvelope?.Data.OverallScore);

        var (gapsStatus, gapsEnvelope) = await _client.GetAsync<IReadOnlyList<SkillGapDto>>($"/api/jobs/{jobId}/skill-gaps");
        Assert.Equal(HttpStatusCode.OK, gapsStatus);

        var (tailorStatus, _) = await _client.PostAsync<TailoredResumeDetailDto>(
            $"/api/jobs/{jobId}/tailor-resume", new { resumeId = Guid.Empty, mode = "Balanced" });
        Assert.Equal(HttpStatusCode.NotFound, tailorStatus);

        var (letterStatus, letterEnvelope) = await _client.PostAsync<CoverLetterDto>(
            $"/api/jobs/{jobId}/cover-letter", new { length = "Standard", tone = "Professional" });
        Assert.Equal(HttpStatusCode.OK, letterStatus);
        Assert.Contains("Senior Backend Engineer", letterEnvelope!.Data.Content);

        var (updateStatus, updateEnvelope) = await _client.PutAsync<JobDto>($"/api/jobs/{jobId}", new
        {
            title = "Senior Backend Engineer (Updated)",
            companyName = "Example Corp",
            location = "Remote - EMEA",
            employmentType = "Full-time",
            description = "Same description with more details.",
            sourceUrl = "https://example.com/jobs/1"
        });
        Assert.Equal(HttpStatusCode.OK, updateStatus);
        Assert.Equal("Senior Backend Engineer (Updated)", updateEnvelope?.Data.Title);
    }

    [Fact]
    public async Task Applications_Crud_Flow()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (jobStatus, jobEnvelope) = await _client.PostAsync<JobDto>("/api/jobs", new
        {
            title = "Backend Engineer",
            companyName = "ACME",
            location = "Berlin",
            employmentType = "Full-time",
            description = "Build backend services with C#.",
            sourceUrl = ""
        });
        var jobId = jobEnvelope!.Data.Id;

        var (createStatus, createEnvelope) = await _client.PostAsync<ApplicationDto>("/api/applications", new
        {
            jobId,
            companyName = "ACME",
            jobTitle = "Backend Engineer",
            jobUrl = "https://acme.com/careers",
            location = "Berlin",
            status = "Applied",
            source = "manual"
        });
        Assert.Equal(HttpStatusCode.OK, createStatus);
        var applicationId = createEnvelope!.Data.Id;
        Assert.Equal("ACME", createEnvelope.Data.CompanyName);

        var (statusUpdate, statusEnvelope) = await _client.PutAsync<ApplicationDetailDto>(
            $"/api/applications/{applicationId}/status", new { newStatus = "Interview" });
        Assert.Equal(HttpStatusCode.OK, statusUpdate);
        Assert.Equal(ApplicationStatus.Interview, statusEnvelope?.Data.Status);

        var (detailsUpdate, detailsEnvelope) = await _client.PutAsync<ApplicationDetailDto>(
            $"/api/applications/{applicationId}", new { notes = "Recruiter call at 10am", followUpDate = DateTime.UtcNow.AddDays(3) });
        Assert.Equal(HttpStatusCode.OK, detailsUpdate);
        Assert.Contains("Recruiter call", detailsEnvelope?.Data.Notes);

        var (listStatus, listEnvelope) = await _client.GetAsync<PagedResult<ApplicationDto>>("/api/applications");
        Assert.Equal(HttpStatusCode.OK, listStatus);
        Assert.Single(listEnvelope!.Data.Items);

        var (invalidStatusUpdate, _) = await _client.PutAsync<object>(
            $"/api/applications/{applicationId}/status", new { newStatus = "Withdrawn" });
        Assert.Equal(HttpStatusCode.OK, invalidStatusUpdate);

        var (deleteStatus, _) = await _client.DeleteAsync<object>($"/api/applications/{applicationId}");
        Assert.Equal(HttpStatusCode.OK, deleteStatus);
    }

    [Fact]
    public async Task Interview_Session_Answer_And_Complete_Flow()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (jobStatus, jobEnvelope) = await _client.PostAsync<JobDto>("/api/jobs", new
        {
            title = "Full Stack Developer",
            companyName = "Tech Co",
            location = "Remote",
            employmentType = "Full-time",
            description = "React and .NET development role.",
            sourceUrl = ""
        });
        var jobId = jobEnvelope!.Data.Id;

        var (createStatus, createEnvelope) = await _client.PostAsync<InterviewSessionDetailDto>("/api/interviews",
            new { jobId, mode = "Mixed" });
        Assert.Equal(HttpStatusCode.OK, createStatus);
        var session = createEnvelope!.Data;
        Assert.True(session.Questions.Count > 0);

        var questionIds = session.Questions.Select(q => q.Id).ToList();
        var sessionId = session.Session.Id;

        var (submitStatus, submitEnvelope) = await _client.PostAsync<SubmitInterviewAnswerDto>(
            $"/api/interviews/questions/{questionIds[0]}/answer",
            new { answer = "I previously built a dashboard service with .NET reducing load times by 40 percent using caching." });
        Assert.Equal(HttpStatusCode.OK, submitStatus);
        Assert.InRange(submitEnvelope!.Data.Score, 0, 100);

        foreach (var qid in questionIds.Skip(1))
        {
            var (sStatus, _) = await _client.PostAsync<object>(
                $"/api/interviews/questions/{qid}/answer",
                new { answer = "Here is my structured approach: first I analyze requirements, then prototype, then ship with tests." });
            Assert.Equal(HttpStatusCode.OK, sStatus);
        }

        var (listStatus, listEnvelope) = await _client.GetAsync<IReadOnlyList<InterviewSessionDto>>($"/api/interviews?jobId={jobId}");
        Assert.Equal(HttpStatusCode.OK, listStatus);
        Assert.Single(listEnvelope!.Data);

        var (completeStatus, completeEnvelope) = await _client.PostAsync<InterviewSessionDetailDto>(
            $"/api/interviews/{sessionId}/complete");
        Assert.Equal(HttpStatusCode.OK, completeStatus);
        Assert.True(completeEnvelope?.Data.Session.IsCompleted);
    }

    [Fact]
    public async Task Roadmap_Generate_Get_UpdateTask_Flow()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (generateStatus, generateEnvelope) = await _client.PostAsync<RoadmapDto>("/api/roadmaps",
            new { targetRole = "Senior Software Engineer" });
        Assert.Equal(HttpStatusCode.OK, generateStatus);
        var roadmap = generateEnvelope!.Data;
        Assert.True(roadmap.Tasks.Count > 0);

        var (getStatus, getEnvelope) = await _client.GetAsync<RoadmapDto>("/api/roadmaps");
        Assert.Equal(HttpStatusCode.OK, getStatus);
        Assert.Equal(roadmap.Id, getEnvelope?.Data.Id);

        var taskId = roadmap.Tasks[0].Id;
        var (updateStatus, updateEnvelope) = await _client.PutAsync<RoadmapTaskDto>(
            $"/api/roadmaps/tasks/{taskId}", new { newStatus = "InProgress" });
        Assert.Equal(HttpStatusCode.OK, updateStatus);
        Assert.Equal("InProgress", updateEnvelope?.Data.Status);
    }

    [Fact]
    public async Task Copilot_Chat_And_Conversations_Flow()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (startStatus, startEnvelope) = await _client.PostAsync<CopilotReplyDto>("/api/copilot/chat",
            new { message = "How should I prepare for backend interviews?" });
        Assert.Equal(HttpStatusCode.OK, startStatus);
        var conversationId = startEnvelope!.Data.ConversationId;
        Assert.False(string.IsNullOrWhiteSpace(startEnvelope.Data.Message.Content));

        var (sendStatus, sendEnvelope) = await _client.PostAsync<CopilotReplyDto>("/api/copilot/chat",
            new { message = "Give me more detail.", conversationId });
        Assert.Equal(HttpStatusCode.OK, sendStatus);
        Assert.Equal(conversationId, sendEnvelope?.Data.ConversationId);

        var (convStatus, convEnvelope) = await _client.GetAsync<IReadOnlyList<CopilotConversationDto>>("/api/copilot/conversations");
        Assert.Equal(HttpStatusCode.OK, convStatus);
        Assert.Single(convEnvelope!.Data);

        var (messagesStatus, messagesEnvelope) = await _client.GetAsync<IReadOnlyList<CopilotMessageDto>>(
            $"/api/copilot/conversations/{conversationId}");
        Assert.Equal(HttpStatusCode.OK, messagesStatus);
        Assert.True(messagesEnvelope!.Data.Count >= 2);
    }

    [Fact]
    public async Task Dashboard_And_Readiness_Work()
    {
        await registerDefaultUser();
        await UpdateProfileAsync(BuildSampleProfile());

        var (dashboardStatus, dashboardEnvelope) = await _client.GetAsync<DashboardDto>("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardStatus);
        Assert.NotNull(dashboardEnvelope?.Data);

        var (readinessStatus, readinessEnvelope) = await _client.GetAsync<RecruiterReadinessDto>("/api/readiness?recalculate=true");
        Assert.Equal(HttpStatusCode.OK, readinessStatus);
        Assert.InRange(readinessEnvelope!.Data.OverallScore, 0, 100);

        var (cachedStatus, cachedEnvelope) = await _client.GetAsync<RecruiterReadinessDto>("/api/readiness");
        Assert.Equal(HttpStatusCode.OK, cachedStatus);
        Assert.Equal(readinessEnvelope.Data.OverallScore, cachedEnvelope?.Data.OverallScore);
    }

    [Fact]
    public async Task UsersCannotSeeEachOthersData()
    {
        var (_, resumeEnvelope) = await RegisterAsync("alice@test.dev", "Password123!", "Alice");
        var aliceClient = new ApiClient(_factory.CreateClient()) { Token = resumeEnvelope!.Data.Token };
        _client.Token = resumeEnvelope.Data.Token;

        var (uploadStatus, uploadText) = await aliceClient.PostFileAsync(
            "/api/resumes", "alice.txt", "text/plain", Encoding.UTF8.GetBytes("Alice resume text"));
        Assert.Equal(HttpStatusCode.OK, uploadStatus);
        var aliceResume = Deserialize<ApiEnvelope<ResumeDto>>(uploadText)!.Data;

        var (createJobStatus, createJobEnvelope) = await _client.PostAsync<JobDto>("/api/jobs", new
        {
            title = "Private Job",
            companyName = "AliceCorp",
            location = "",
            employmentType = "",
            description = "Confidential.",
            sourceUrl = ""
        });
        Assert.Equal(HttpStatusCode.OK, createJobStatus);

        var second = await RegisterAsync("bob@test.dev", "Password123!", "Bob");
        var bobClient = new ApiClient(_factory.CreateClient()) { Token = second.Envelope!.Data.Token };

        var (bobResumesStatus, bobResumes) = await bobClient.GetAsync<PagedResult<ResumeDto>>("/api/resumes");
        Assert.Equal(HttpStatusCode.OK, bobResumesStatus);
        Assert.Empty(bobResumes!.Data.Items);

        var (bobResumeStatus, _) = await bobClient.GetAsync<object>($"/api/resumes/{aliceResume.Id}");
        Assert.Equal(HttpStatusCode.NotFound, bobResumeStatus);

        var (bobJobsStatus, bobJobs) = await bobClient.GetAsync<PagedResult<JobDto>>("/api/jobs");
        Assert.Equal(HttpStatusCode.OK, bobJobsStatus);
        Assert.Empty(bobJobs!.Data.Items);

        var (bobJobStatus, _) = await bobClient.GetAsync<object>($"/api/jobs/{createJobEnvelope!.Data.Id}");
        Assert.Equal(HttpStatusCode.NotFound, bobJobStatus);
    }

    private async Task registerDefaultUser()
    {
        var (_, envelope) = await RegisterAsync("default@test.dev", "Password123!", "Test User");
        _client.Token = envelope!.Data.Token;
    }

    private async Task UpdateProfileAsync(UpdateProfileCommand profile)
    {
        var (status, _) = await _client.PutAsync<ProfileDto>("/api/profile", profile);
        Assert.Equal(HttpStatusCode.OK, status);
    }

    private async Task<(HttpStatusCode Status, ApiEnvelope<AuthResponse>? Envelope)> RegisterAsync(
        string email, string password, string fullName)
    {
        return await _client.PostAsync<AuthResponse>("/api/auth/register",
            new { email, password, fullName });
    }

    private static UpdateProfileCommand BuildSampleProfile()
        => new(
            FullName: "Test User",
            Headline: "Senior Backend Engineer",
            Phone: "+49 123 456789",
            Location: "Berlin",
            CareerLevel: CareerLevel.MidLevel,
            YearsOfExperience: 5,
            PreferredWorkType: WorkType.Hybrid,
            PreferredLocation: "Remote",
            TargetRole: "Senior Software Engineer",
            TargetIndustries: "Technology, Fintech",
            ProfessionalSummary: "Backend engineer who ships reliable services.",
            CareerGoals: "Become a staff engineer.",
            GitHubUrl: "https://github.com/testuser",
            LinkedInUrl: "https://linkedin.com/in/testuser",
            PortfolioUrl: string.Empty,
            Education: new[]
            {
                new EducationDto("TU Berlin", "BSc", "Computer Science", "2015", "2019", "Focus on distributed systems.")
            },
            Experiences: new[]
            {
                new ExperienceDto("ACME", "Backend Engineer", "Berlin", "2021", "", true,
                    "Built APIs serving 100k users.", "Led a team of 3.", "Shipped zero-downtime deploys.")
            },
            Projects: new[]
            {
                new ProjectDto("Order Services", "Order processing service", "C#, PostgreSQL, Azure",
                    "Lead Engineer", "", "2022", "2023", "Cut order latency by 40%.")
            },
            Skills: new[]
            {
                new SkillDto("ASP.NET Core", "Technical", "Advanced"),
                new SkillDto("C#", "Technical", "Advanced"),
                new SkillDto("PostgreSQL", "Technical", "Intermediate"),
                new SkillDto("Docker", "Technical", "Intermediate")
            },
            Certifications: new[]
            {
                new CertificationDto("AZ-204", "Microsoft", "2023", "")
            },
            Goals: new[]
            {
                new CareerGoalDto("Lead a platform team.", "2 years")
            },
            LinkedInProfile: new LinkedInDto("https://linkedin.com/in/testuser", "Backend Engineer",
                "I build backend systems.", "5 years at ACME.", "C#, SQL"));

    private static T? Deserialize<T>(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
}