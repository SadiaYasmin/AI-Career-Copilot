using System.Text.Json;
using System.Text.RegularExpressions;
using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Scoring;
using Microsoft.Extensions.Options;

namespace CareerCopilot.AI;

/// <summary>
/// Career Copilot AI orchestration. Every public method degrades to a deterministic,
/// fully explainable result when the Gemini provider is unavailable, so the product
/// never blocks on or fabricates from an AI outage.
/// </summary>
public sealed class AiCareerService : ICareerAiService
{
    private const string SystemPrompt =
        "You are Career Copilot, a professional career assistant embedded in a job-search product. " +
        "Answer based ONLY on the user data provided. Never invent, guess or pad facts; " +
        "if data is missing, base the answer on the role/general best practice and say so. " +
        "Scores are career profile evaluations, not hiring decisions. " +
        "Always respond with valid JSON only, no markdown fences.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GeminiApiClient _client;
    private readonly GeminiOptions _options;
    private readonly ResumeAnalyzerService _resumeAnalyzer;

    public AiCareerService(GeminiApiClient client, IOptions<GeminiOptions> options, ResumeAnalyzerService resumeAnalyzer)
    {
        _client = client;
        _options = options.Value;
        _resumeAnalyzer = resumeAnalyzer;
    }

    public async Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Extract structured data from this job posting. Return JSON with this exact schema:\n" +
            "{\"title\":string,\"company\":string,\"location\":string,\"employmentType\":string,\"" +
            "experienceRequirement\":string,\"educationRequirement\":string,\"requirements\":[{\"name\":string,\"" +
            "requirementType\":\"required|preferred|inferred\",\"importance\":string,\"sourceText\":string}],\"" +
            "keywords\":[string],\"responsibilities\":[string]}\n\n" +
            $"Job posting:\n{JobDescriptions(context.Job.Description, context.Job.Title, context.Job.Company)}",
            cancellationToken);

        var dto = FromJson<JsonJobAnalysisDto>(json);
        if (dto is null)
        {
            return FallbackJobAnalysis(context);
        }

        return new JobAnalysisResult(
            dto.title ?? context.Job.Title,
            dto.company ?? context.Job.Company,
            dto.location ?? context.Job.Location,
            dto.employmentType ?? string.Empty,
            dto.experienceRequirement ?? string.Empty,
            dto.educationRequirement ?? string.Empty,
            (dto.requirements ?? new List<JsonRequirementDto>())
                .Where(r => !string.IsNullOrWhiteSpace(r.name))
                .Select(r => new AiJobRequirement(
                    r.name!.Trim(),
                    NormalizeRequirementType(r.requirementType),
                    r.importance ?? string.Empty,
                    r.sourceText ?? r.name.Trim()))
                .Take(40)
                .ToList(),
            dto.keywords ?? new List<string>(),
            dto.responsibilities ?? new List<string>());
    }

    public async Task<ResumeAnalysisResult> AnalyzeResumeAsync(ResumeAnalysisContext context, CancellationToken cancellationToken)
    {
        var scoringContext = new
        {
            resume = new { textLangs = context.Resume.Lines.Take(400).ToList() },
            person = new
            {
                targetRole = context.Person.TargetRole,
                headline = context.Person.Headline,
                summary = context.Person.ProfessionalSummary,
                skills = context.Person.Skills.Select(s => s.Name).ToList(),
                experience = context.Person.Experience,
                projects = context.Person.Projects
            }
        };

        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Score this resume for ATS readability, structure and impact (0-100). " +
            "Only reference facts present in the resume and profile below. " +
            "Return JSON with this exact schema:\n" +
            "{\"score\":0-100,\"strengths\":[string],\"improvements\":[string],\"atRiskFindings\":[string],\"summary\":string}\n\n" +
            "RESUME:\n" + Crop(context.Resume.ParsedText, 7000) +
            "\n\nPROFILE:\n" + JsonSerializer.Serialize(scoringContext.person),
            cancellationToken);

        var dto = FromJson<JsonResumeAnalysisDto>(json);
        if (dto is not null)
        {
            return new ResumeAnalysisResult(
                Math.Clamp(dto.score, 0, 100),
                ListOr(dto.strengths, "Include skills you verified in your industry."),
                ListOr(dto.improvements, "Add measurable outcomes to your bullet points."),
                dto.atRiskFindings ?? new List<string>(),
                dto.summary ?? SummarizeFallback(dto.score),
                true);
        }

        var fallback = _resumeAnalyzer.Analyze(context.Resume.ParsedText);
        return new ResumeAnalysisResult(
            fallback.Score, fallback.Strengths, fallback.Improvements,
            fallback.AtRiskFindings, fallback.Summary, false);
    }

    public async Task<MatchAiResult> ExplainMatchAsync(MatchAiContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Explain a resume/job match. Only reference the supplied evidence strings verbatim where possible. " +
            "Return JSON with this exact schema:\n" +
            "{\"matches\":[{\"name\":string,\"status\":\"strong|partial|missing\",\"evidence\":string}],\"" +
            "recommendations\":[string],\"explanation\":string}\n\n" +
            $"TARGET ROLE: {context.Job.Title}\n" +
            $"OVERALL SCORE: {context.OverallScore}/100 (skills {context.SkillsScore}, experience {context.ExperienceScore}, " +
            $"education {context.EducationScore}, projects {context.ProjectScore}, keywords {context.KeywordScore}, alignment {context.AlignmentScore})\n" +
            $"STRONG MATCHES: {Join(context.StrongMatches)}\nPARTIAL MATCHES: {Join(context.PartialMatches)}\n" +
            $"MISSING: {Join(context.Missing)}",
            cancellationToken);

        var dto = FromJson<JsonMatchDto>(json);
        if (dto is not null && (dto.matches?.Count > 0 || !string.IsNullOrWhiteSpace(dto.explanation)))
        {
            return new MatchAiResult(
                (dto.matches ?? new List<JsonMatchItemDto>())
                    .Where(m => !string.IsNullOrWhiteSpace(m.name))
                    .Select(m => new MatchItem(
                        m.name!.Trim(),
                        NormalizeMatchStatus(m.status),
                        m.evidence ?? string.Empty))
                    .Take(50)
                    .ToList(),
                dto.recommendations ?? new List<string>(),
                dto.explanation ?? FallbackExplanation(context));
        }

        return FallbackMatch(context);
    }

    public async Task<TailorResumeResult> TailorResumeAsync(TailorResumeContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Tailor the resume for the job. Rewrite section content to emphasize matching keywords, " +
            "keep every claim grounded in the original resume text (do not invent credentials). Mode: " + context.Mode + ". " +
            "Return JSON with this exact schema:\n{\"content\":string,\"changesSummary\":string}\n\n" +
            "RESUME:\n" + Crop(context.Resume.ParsedText, 6000) +
            $"\n\nJOB: {context.Job.Title} at {context.Job.Company}\n" +
            $"STRONG MATCHES: {Join(context.StrongMatches)}\nMISSING: {Join(context.Missing)}\n" +
            "JOB DESCRIPTION:\n" + Crop(context.Job.Description, 4000),
            cancellationToken);

        var dto = FromJson<JsonTailorDto>(json);
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.content))
        {
            return new TailorResumeResult(dto.content, dto.changesSummary ?? "Resume reworded to emphasize matching keywords.");
        }

        return new TailorResumeResult(
            context.Resume.ParsedText,
            "AI tailoring was unavailable; resume kept unchanged. Regenerate when the AI service is online.");
    }

    public async Task<CoverLetterResult> GenerateCoverLetterAsync(CoverLetterContext context, CancellationToken cancellationToken)
    {
        var person = new
        {
            headline = context.Person.Headline,
            summary = context.Person.ProfessionalSummary,
            skills = context.Person.Skills.Select(s => s.Name).ToList(),
            experience = context.Person.Experience.Select(e => e.Title)
        };

        var json = await _client.GenerateTextAsync(SystemPrompt,
            $"Write a cover letter for the {context.Job.Title} role at {context.Job.Company}. " +
            $"Length: {context.Length}. Tone: {context.Tone}. Ground every statement in the profile data - never invent facts. " +
            "Return JSON with this exact schema:\n{\"content\":string}\n\n" +
            "JOB DESCRIPTION:\n" + Crop(context.Job.Description, 4000) +
            "\n\nPROFILE:\n" + JsonSerializer.Serialize(person),
            cancellationToken);

        var dto = FromJson<JsonCoverLetterDto>(json);
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.content))
        {
            return new CoverLetterResult(dto.content);
        }

        return new CoverLetterResult(DeterministicCoverLetter(context.Person, context.Job));
    }

    public async Task<InterviewQuestionsResult> GenerateInterviewQuestionsAsync(InterviewContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Create 5 realistic mock-interview questions for the candidate based on the job and profile. " +
            "Mix question types: Behavioral, Technical, Scenario, ResumeBased, CompanyRole. " +
            "Return JSON with this exact schema:\n{\"questions\":[{\"question\":string,\"questionType\":\"" +
            "behavioral|technical|scenario|resume|company\"}]}\n\n" +
            $"JOB: {context.Job.Title} at {context.Job.Company}\n" +
            $"RESPONSIBILITIES: {Join(context.JobResponsibilities)}\nREQUIRED SKILLS: {Join(context.RequiredSkills)}\n" +
            $"PROFILE SKILLS: {string.Join(", ", context.Person.Skills.Select(s => s.Name))}\n" +
            "JOB DESCRIPTION:\n" + Crop(context.Job.Description, 4000),
            cancellationToken);

        var dto = FromJson<JsonInterviewQuestionsDto>(json);
        var questions = dto?.questions?.Where(q => !string.IsNullOrWhiteSpace(q.question))
            .Select(q => new AiInterviewQuestion(q.question!.Trim(), NormalizeQuestionType(q.questionType)))
            .Take(5)
            .ToList();

        if (questions is { Count: > 0 })
        {
            return new InterviewQuestionsResult(questions);
        }

        return new InterviewQuestionsResult(FallbackQuestions(context));
    }

    public async Task<AnswerEvaluationResult> EvaluateAnswerAsync(AnswerEvaluationContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Score the interview answer 0-100 across relevance, clarity, technical depth, structure, specificity and conciseness. " +
            "Be honest and specific; never invent claims about the candidate. " +
            "Return JSON with this exact schema:\n" +
            "{\"score\":0-100,\"relevanceScore\":0-100,\"clarityScore\":0-100,\"technicalScore\":0-100,\"" +
            "structureScore\":0-100,\"specificityScore\":0-100,\"concisenessScore\":0-100,\"feedback\":string,\"" +
            "improvementSuggestion\":string,\"followUpQuestion\":string|null}\n\n" +
            $"QUESTION ({context.QuestionType}): {context.Question}\nJOB: {context.Job.Title}\n" +
            $"REQUIRED SKILLS: {string.Join(", ", context.Job.Requirements.Select(r => r.Name))}\n" +
            $"ANSWER:\n{context.Answer}",
            cancellationToken);

        var dto = FromJson<JsonEvaluationDto>(json);
        if (dto is not null && dto.score > 0)
        {
            return new AnswerEvaluationResult(
                Math.Clamp(dto.score, 0, 100),
                Math.Clamp(dto.relevanceScore, 0, 100),
                Math.Clamp(dto.clarityScore, 0, 100),
                Math.Clamp(dto.technicalScore, 0, 100),
                Math.Clamp(dto.structureScore, 0, 100),
                Math.Clamp(dto.specificityScore, 0, 100),
                Math.Clamp(dto.concisenessScore, 0, 100),
                dto.feedback ?? "Good structured answer.",
                dto.improvementSuggestion ?? "Add a concrete example with measurable impact.",
                dto.followUpQuestion);
        }

        return FallbackEvaluation(context);
    }

    public async Task<CareerRoadmapResult> GenerateCareerRoadmapAsync(CareerRoadmapContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Create a 6-12 month career roadmap toward the target role, addressing the candidate's skill gaps. " +
            "Ground tasks in the listed gaps; do not invent new qualifications. " +
            "Return JSON with this exact schema:\n" +
            "{\"targetRole\":string,\"description\":string,\"tasks\":[{\"title\":string,\"description\":string,\"" +
            "month\":1-12,\"skill\":string,\"priority\":\"low|medium|high|critical\"}]}\n\n" +
            $"TARGET ROLE: {context.TargetRole}\nSKILL GAPS: {Join(context.SkillGaps)}\n" +
            "PROFILE:\n" + JsonSerializer.Serialize(new { context.Person.TargetRole, skills = context.Person.Skills.Select(s => s.Name) }),
            cancellationToken);

        var dto = FromJson<JsonRoadmapDto>(json);
        if (dto is not null && (dto.tasks?.Count > 0 || !string.IsNullOrWhiteSpace(dto.description)))
        {
            return new CareerRoadmapResult(
                dto.targetRole ?? context.TargetRole,
                dto.description ?? string.Empty,
                (dto.tasks ?? new List<JsonRoadmapTaskDto>())
                    .Where(t => !string.IsNullOrWhiteSpace(t.title))
                    .Select(t => new AiRoadmapTask(
                        t.title!.Trim(), t.description ?? string.Empty,
                        t.month ?? string.Empty, t.skill ?? string.Empty,
                        NormalizePriority(t.priority)))
                    .Take(12)
                    .ToList());
        }

        return FallbackRoadmap(context);
    }

    public async Task<string> GenerateCopilotReplyAsync(CopilotContext context, CancellationToken cancellationToken)
    {
        var dataBlock = string.Join("\n", context.SupportingData.Keys
            .Select(k => $"- {k}: {Crop(context.SupportingData[k], 200)}"));

        var json = await _client.GenerateTextAsync(SystemPrompt,
            "You are a career copilot inside a job-search product. Answer the user's career question helpfully and concisely. " +
            "Ground your answer in the provided supporting data (profile, resume, match, gaps). If the data is empty say so. " +
            "Return JSON with this exact schema:\n{\"reply\":string}\n\n" +
            $"CONVERSATION TITLE: {context.ConversationTitle}\nRECENT MESSAGES: {Join(context.RecentMessages)}\n" +
            $"TARGET ROLE: {context.Person.TargetRole}\nSUPPORTING DATA:\n{dataBlock}\n\nUSER MESSAGE:\n{context.Message}",
            cancellationToken);

        var dto = FromJson<JsonCopilotDto>(json);
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.reply))
        {
            return dto.reply;
        }

        return FallbackCopilotReply(context);
    }

    public async Task<LinkedInAnalysisResult> AnalyzeLinkedInAsync(LinkedInContext context, CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Analyze this LinkedIn profile for recruiter readiness. Return JSON with this exact schema:\n" +
            "{\"suggestions\":[{\"section\":string,\"original\":string,\"improved\":string,\"reasoning\":string}],\"" +
            "strengths\":[string],\"score\":0-100}\n\nTARGET ROLE: " + context.TargetRole + "\nHEADLINE: " +
            context.Headline + "\nABOUT:\n" + Crop(context.About, 3000) + "\nEXPERIENCE:\n" +
            Crop(context.ExperienceText, 4000) + "\nSKILLS:\n" + Crop(context.SkillsText, 2000),
            cancellationToken);

        var dto = FromJson<JsonLinkedInDto>(json);
        if (dto is not null && dto.score.HasValue)
        {
            return new LinkedInAnalysisResult(
                (dto.suggestions ?? new List<JsonLinkedInSuggestionDto>())
                    .Where(s => !string.IsNullOrWhiteSpace(s.section))
                    .Select(s => new LinkedInSuggestion(
                        s.section!, s.original ?? string.Empty,
                        s.improved ?? string.Empty, s.reasoning ?? string.Empty))
                    .Take(20)
                    .ToList(),
                dto.strengths ?? new List<string>(),
                Math.Clamp(dto.score.Value, 0, 100));
        }

        return new LinkedInAnalysisResult(new List<LinkedInSuggestion>(),
            new List<string> { "Profile provided for analysis." }, 50);
    }

    public async Task<string> GenerateInterviewCompletionSummaryAsync(
        string questionSummary,
        string role,
        CancellationToken cancellationToken)
    {
        var json = await _client.GenerateTextAsync(SystemPrompt,
            "Write a 2-4 sentence closing summary of the completed mock interview. Use only the feedback lines provided. " +
            "Return JSON with this exact schema:\n{\"reply\":string}\n\nROLE: " + role + "\nFEEDBACK:\n" + Crop(questionSummary, 3000),
            cancellationToken);

        var dto = FromJson<JsonCopilotDto>(json);
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.reply))
        {
            return dto.reply;
        }

        return $"Your mock interview for {role} is complete. Focus next on the feedback above: " +
               "add concrete, quantified examples and tighten your answer structure.";
    }

    private static MatchAiResult FallbackMatch(MatchAiContext context)
    {
        var matches = new List<MatchItem>();
        foreach (var item in context.StrongMatches)
        {
            matches.Add(new MatchItem(item, "strong", "Listed as a strong match."));
        }

        foreach (var item in context.PartialMatches)
        {
            matches.Add(new MatchItem(item, "partial", "Listed as a partial match."));
        }

        foreach (var item in context.Missing)
        {
            matches.Add(new MatchItem(item, "missing", "Listed as missing."));
        }

        var recommendations = context.Missing.Take(5)
            .Select(m => $"Develop or evidence: {m}.")
            .ToList();

        return new MatchAiResult(
            matches,
            recommendations,
            FallbackExplanation(context));
    }

    private static string FallbackExplanation(MatchAiContext context)
        => $"Your profile scores {context.OverallScore}/100 against {context.Job.Title}: " +
           $"skills {context.SkillsScore}, experience {context.ExperienceScore}, education {context.EducationScore}, " +
           $"projects {context.ProjectScore}, keywords {context.KeywordScore} and role alignment {context.AlignmentScore}. " +
           "Focus on the listed missing items to raise your match.";

    private static IReadOnlyList<AiInterviewQuestion> FallbackQuestions(InterviewContext context)
    {
        var questions = new List<AiInterviewQuestion>
        {
            new($"Tell me about your background and why you are a strong fit for the {context.Job.Title} role.", "behavioral")
        };

        var skill = context.RequiredSkills.FirstOrDefault() ?? context.Person.Skills.FirstOrDefault()?.Name;
        questions.Add(new AiInterviewQuestion(
            skill is null
                ? "Describe a time you solved a difficult technical problem. What was your approach?"
                : $"Walk me through a time you applied {skill} in a real project.", "technical"));

        questions.Add(new AiInterviewQuestion(
            "How would you handle receiving conflicting requirements from two stakeholders?", "scenario"));

        var project = context.Person.Projects.FirstOrDefault();
        questions.Add(new AiInterviewQuestion(
            project is null
                ? "Walk me through a project from your background and the impact it delivered."
                : $"Walk me through {project.Name}. What was your role and what measurable impact did it have?", "resume"));

        questions.Add(new AiInterviewQuestion(
            string.IsNullOrWhiteSpace(context.Job.Company)
                ? "What excites you about the " + context.Job.Title + " role and which of your strengths are most relevant?"
                : "What do you know about " + context.Job.Company + " and why do you want to join?", "company"));

        return questions;
    }

    private static AnswerEvaluationResult FallbackEvaluation(AnswerEvaluationContext context)
    {
        var words = context.Answer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var lower = context.Answer.ToLowerInvariant();

        var keywordHits = context.Job.Requirements
            .Select(r => r.Name?.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrEmpty(n) && lower.Contains(n!))
            .Count();

        var mentionsNumbers = System.Text.RegularExpressions.Regex.IsMatch(
            context.Answer, @"\d+(\s?(%|x|users|customers|clients|projects|people)\b|$|,)");
        var hasStructure = new[]
        {
            "first", "second", "then", "finally", "for example", "in conclusion", "additionally", "however"
        }.Any(w => lower.Contains(w));
        var hasDeliverable = new[]
        {
            "built", "created", "developed", "led", "launched", "delivered", "improved", "reduced", "increased"
        }.Any(w => lower.Contains(w));

        var specificity = (int)Math.Round(Math.Min(100.0, (mentionsNumbers ? 35 : 5) + (hasDeliverable ? 35 : 10)));
        var relevance = Math.Min(100, 40 + keywordHits * 15);
        var clarity = (int)Math.Min(100, 35 + words / 8.0);
        var structure = hasStructure ? 75 : 45;
        var technical = Math.Min(100, 30 + keywordHits * 10);
        var conciseness = words is >= 40 and <= 140 ? 80 : words < 20 ? 45 : 60;
        var overall = Math.Clamp((int)Math.Round(
            (relevance + clarity + technical + structure + specificity + conciseness) / 6.0), 0, 100);

        var feedback = overall >= 70
            ? "Solid answer with relevant content and concrete language."
            : "The answer includes useful material but needs more structure and specifics.";
        var suggestion = "Add a concrete example (preferably with a measurable outcome) and structure it as Situation-Task-Action-Result.";

        return new AnswerEvaluationResult(
            overall, relevance, clarity, technical, structure, specificity, conciseness,
            feedback, suggestion, null);
    }

    private static CareerRoadmapResult FallbackRoadmap(CareerRoadmapContext context)
    {
        var tasks = new List<AiRoadmapTask>();
        var month = 1;
        foreach (var gap in context.SkillGaps.Take(6))
        {
            var priority = gap.Contains("Critical", StringComparison.OrdinalIgnoreCase) ? "critical"
                : gap.Contains("High", StringComparison.OrdinalIgnoreCase) ? "high"
                : "medium";
            tasks.Add(new AiRoadmapTask(
                $"Close the gap: {gap}",
                $"Build structured {gap} capability through a course plus a hands-on project that you can show on your resume.",
                month.ToString(), gap, priority));
            month++;
        }

        if (tasks.Count == 0)
        {
            tasks.Add(new AiRoadmapTask(
                "Polish your targeted profile",
                "Complete your career profile and add measurable achievements to your resume and projects.",
                "1", "Profile", "high"));
        }

        return new CareerRoadmapResult(
            context.TargetRole,
            $"A step-by-step roadmap to become a {context.TargetRole}, built from your current skill gaps.",
            tasks);
    }

    private static string FallbackCopilotReply(CopilotContext context)
    {
        var facts = new List<string>();
        var targetRole = context.Person.TargetRole;
        if (!string.IsNullOrWhiteSpace(targetRole))
        {
            facts.Add($"your target role is {targetRole}");
        }

        if (context.Person.Skills.Count > 0)
        {
            facts.Add($"you have listed {context.Person.Skills.Count} skills");
        }

        if (context.Job is not null)
        {
            facts.Add($"your active context is the {context.Job.Title} role at {context.Job.Company}");
        }

        var prefix = facts.Count > 0
            ? "From your Career Copilot data: " + string.Join(", ", facts) + "."
            : "I don't have a complete career profile saved yet - fill it in so I can give more tailored advice.";

        return $"{prefix} I'm on hand for interview practice, resume tailoring, match explanations and career " +
               "planning. Ask away (live AI is currently unavailable, so this is a deterministic summary).";
    }

    private static string DeterministicCoverLetter(AiPersonSnapshot person, AiJobSnapshot job)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Dear Hiring Manager,");
        sb.AppendLine();
        sb.AppendLine($"I'm writing to express my interest in the {job.Title} position at {job.Company}.");

        if (!string.IsNullOrWhiteSpace(person.Headline))
        {
            sb.AppendLine($"I describe myself as: {person.Headline}.");
        }

        if (!string.IsNullOrWhiteSpace(person.ProfessionalSummary))
        {
            sb.AppendLine(person.ProfessionalSummary.TrimEnd('.') + ".");
        }

        sb.AppendLine();
        sb.AppendLine("I place these experiences and strengths at the core of my application:");
        var strengths = person.Skills.Select(s => s.Name)
            .Concat(person.Experience.Select(e => e.Title))
            .Concat(person.Projects.Select(p => p.Name))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(6)
            .ToList();
        if (strengths.Count > 0)
        {
            foreach (var item in strengths)
            {
                sb.AppendLine("- " + item);
            }
        }
        else
        {
            sb.AppendLine("- My ability to learn fast and deliver measurable outcomes.");
        }

        sb.AppendLine();
        sb.AppendLine("This letter uses only the information from your profile - tailor it with specifics before sending.");
        sb.AppendLine();
        sb.AppendLine("Sincerely,");

        if (!string.IsNullOrWhiteSpace(person.TargetRole))
        {
            sb.AppendLine("[Your Name] - " + person.TargetRole);
        }
        else
        {
            sb.AppendLine("[Your Name]");
        }

        return sb.ToString();
    }

    private static T? FromJson<T>(string? json)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Crop(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= max ? value : value[..max];
    }

    private static string Join(IReadOnlyList<string> items)
        => items.Count == 0 ? "(none)" : string.Join(", ", items.Take(50));

    private static string NormalizeRequirementType(string? value)
        => value?.ToLowerInvariant() switch
        {
            "required" => "Required",
            "preferred" => "Preferred",
            _ => "Inferred"
        };

    private static readonly string[] TechKeywords =
    {
        "C#", "C++", ".NET", "ASP.NET", "Java", "Python", "JavaScript", "TypeScript", "Go", "Rust",
        "Ruby", "PHP", "Kotlin", "Swift", "SQL", "PostgreSQL", "MySQL", "SQL Server", "MongoDB",
        "Redis", "Kafka", "RabbitMQ", "Azure", "AWS", "GCP", "Kubernetes", "Docker", "Terraform",
        "CI/CD", "React", "Angular", "Vue", "Node.js", "GraphQL", "REST", "gRPC", "microservices",
        "HTML", "CSS", "Git", "Linux", "Agile", "Scrum", "Jira", "Snowflake", "Databricks",
        "Spark", "TensorFlow", "PyTorch", "Machine Learning", "Data Analysis", "Selenium", "NUnit"
    };

    private static readonly Regex ExperienceYearsRegex =
        new(@"\b\d{1,2}\+?\s*(?:year|yr)s?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static JobAnalysisResult FallbackJobAnalysis(JobAnalysisContext context)
    {
        var description = context.Job.Description ?? string.Empty;
        var lower = description.ToLowerInvariant();

        var sentences = description
            .Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 3)
            .ToList();

        var requirements = new List<AiJobRequirement>();
        var keywords = new List<string>();

        foreach (var keyword in TechKeywords)
        {
            var needle = keyword.ToLowerInvariant();
            if (!lower.Contains(needle, StringComparison.Ordinal))
            {
                continue;
            }

            keywords.Add(keyword);
            var strong = lower.Contains("must", StringComparison.Ordinal)
                         || lower.Contains("required", StringComparison.Ordinal)
                         || lower.Contains("essential", StringComparison.Ordinal)
                         || lower.Contains("mandatory", StringComparison.Ordinal)
                         || lower.Contains("strong", StringComparison.Ordinal);
            var sentence = sentences
                .FirstOrDefault(s => s.ToLowerInvariant().Contains(needle, StringComparison.Ordinal))
                ?? string.Empty;
            requirements.Add(new AiJobRequirement(
                keyword,
                strong ? "Required" : "Preferred",
                strong ? "High" : "Medium",
                sentence));
        }

        var responsibilities = sentences
            .Where(s =>
            {
                if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("apply", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("if you", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("we offer", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("equal opportunity", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var t = s.ToLowerInvariant();
                return t.Contains(" build ", StringComparison.Ordinal)
                    || t.Contains(" develop ", StringComparison.Ordinal)
                    || t.Contains(" design ", StringComparison.Ordinal)
                    || t.Contains(" architect ", StringComparison.Ordinal)
                    || t.Contains(" maintain ", StringComparison.Ordinal)
                    || t.Contains(" implement ", StringComparison.Ordinal)
                    || t.Contains(" lead ", StringComparison.Ordinal);
            })
            .Select(s => s.Trim())
            .Take(20)
            .ToList();

        var experienceMatch = ExperienceYearsRegex.Match(description);
        var experienceRequirement = experienceMatch.Success
            ? experienceMatch.Value.Trim() + " experience"
            : string.Empty;

        return new JobAnalysisResult(
            context.Job.Title,
            context.Job.Company,
            context.Job.Location,
            DetectEmploymentType(description),
            experienceRequirement,
            string.Empty,
            requirements.Take(40).ToList(),
            keywords.Distinct().ToList(),
            responsibilities);
    }

    private static string DetectEmploymentType(string description)
    {
        var lower = description.ToLowerInvariant();
        if (lower.Contains("remote", StringComparison.Ordinal))
        {
            return "Remote";
        }

        if (lower.Contains("hybrid", StringComparison.Ordinal))
        {
            return "Hybrid";
        }

        if (lower.Contains("full-time", StringComparison.Ordinal) || lower.Contains("full time", StringComparison.Ordinal))
        {
            return "Full-time";
        }

        if (lower.Contains("part-time", StringComparison.Ordinal) || lower.Contains("part time", StringComparison.Ordinal))
        {
            return "Part-time";
        }

        if (lower.Contains("contract", StringComparison.Ordinal))
        {
            return "Contract";
        }

        if (lower.Contains("internship", StringComparison.Ordinal))
        {
            return "Internship";
        }

        return "Unknown";
    }

    private static string NormalizeMatchStatus(string? value)
        => value?.ToLowerInvariant() switch
        {
            "strong" => "strong",
            "partial" => "partial",
            "missing" => "missing",
            _ => "partial"
        };

    private static string NormalizeQuestionType(string? value)
        => value?.ToLowerInvariant() switch
        {
            "technical" => "technical",
            "scenario" or "situational" or "case" => "scenario",
            "resume" or "resumebased" => "resume",
            "company" or "companyrole" or "role" => "company",
            _ => "behavioral"
        };

    private static string NormalizePriority(string? value)
        => value?.ToLowerInvariant() switch
        {
            "critical" => "critical",
            "high" => "high",
            "low" => "low",
            _ => "medium"
        };

    private static string SummarizeFallback(int score)
        => score >= 80
            ? "Strong resume overall - keep tailoring it per job."
            : score >= 60
                ? "Solid resume with room to improve keywords and measurable impact."
                : "The resume needs structural improvements and more quantified results.";

    private static List<string> ListOr(IReadOnlyList<string>? list, string fallback)
        => list is { Count: > 0 }
            ? list.ToList()
            : new List<string> { fallback };

    private static string JobDescriptions(string description, string title, string company)
    {
        var header = $"Title: {title}\nCompany: {company}\n";
        return header + Crop(description, 8000);
    }
}