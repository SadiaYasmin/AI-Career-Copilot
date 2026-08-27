export interface UserDto {
  id: string
  email: string
  role: string
  isActive: boolean
}

export interface AuthResponse {
  token: string
  user: UserDto
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export type CareerLevel =
  | 'Student'
  | 'FreshGraduate'
  | 'Junior'
  | 'MidLevel'
  | 'Senior'
  | 'Lead'
  | 'Manager'
export type WorkType = 'OnSite' | 'Remote' | 'Hybrid'
export type ApplicationStatus =
  | 'Saved'
  | 'Applied'
  | 'Screening'
  | 'Interview'
  | 'TechnicalRound'
  | 'FinalRound'
  | 'Offer'
  | 'Rejected'
  | 'Withdrawn'
export type InterviewMode = 'Technical' | 'Behavioral' | 'Hr' | 'RoleSpecific' | 'Mixed'
export type RoadmapTaskStatus = 'Pending' | 'InProgress' | 'Completed'
export type TailoringMode = 'Conservative' | 'Balanced' | 'Aggressive'

export interface EducationDto {
  institution: string
  degree: string
  fieldOfStudy: string
  startDate: string
  endDate: string
  description: string
}
export interface ExperienceDto {
  company: string
  title: string
  location: string
  startDate: string
  endDate: string
  isCurrent: boolean
  description: string
  responsibilities: string
  achievements: string
}
export interface ProjectDto {
  name: string
  description: string
  technologies: string
  role: string
  url: string
  startDate: string
  endDate: string
  highlights: string
}
export interface SkillDto {
  name: string
  category: string
  proficiency: string
}
export interface CertificationDto {
  name: string
  issuer: string
  dateObtained: string
  url: string
}
export interface CareerGoalDto {
  description: string
  timeframe: string
}
export interface LinkedInDto {
  url: string
  headline: string
  about: string
  experienceText: string
  skillsText: string
}

export interface ProfileDto {
  userId: string
  fullName: string
  email: string
  headline: string
  phone: string
  location: string
  careerLevel: CareerLevel
  yearsOfExperience: number
  preferredWorkType: WorkType
  preferredLocation: string
  targetRole: string
  targetIndustries: string
  professionalSummary: string
  careerGoals: string
  githubUrl: string
  linkedInUrl: string
  portfolioUrl: string
  education: EducationDto[]
  experiences: ExperienceDto[]
  projects: ProjectDto[]
  skills: SkillDto[]
  certifications: CertificationDto[]
  goals: CareerGoalDto[]
  linkedInProfile: LinkedInDto | null
}

export type UpdateProfileCommand = Omit<ProfileDto, 'userId' | 'email'>

export interface ResumeDto {
  id: string
  name: string
  originalFileName: string
  fileType: string
  isDefault: boolean
  uploadedAt: string
  parseFailed: boolean
  resumeScore: number | null
  analyzedAt: string | null
}

export interface ResumeAnalysisDto {
  resume: ResumeDto
  score: number
  strengths: string[]
  improvements: string[]
  atRiskFindings: string[]
  summary: string
  usedAi: boolean
}

export interface JobDto {
  id: string
  title: string
  companyName: string
  location: string
  employmentType: string
  sourceUrl: string
  isAnalyzed: boolean
  analyzedAt: string | null
  createdAt: string
  latestMatchScore: number | null
}
export interface JobDetailDto extends JobDto {
  description: string
  applicationsCount: number
  requirements: JobRequirementDto[]
}
export interface JobRequirementDto {
  id: string
  requirementType: 'Required' | 'Preferred' | 'Inferred'
  name: string
  description: string
  importance: string
  sourceText: string
}
export interface CreateJobCommand {
  title: string
  companyName: string
  location: string
  employmentType: string
  description: string
  sourceUrl: string
}
export type UpdateJobCommand = CreateJobCommand

export interface JobMatchDto {
  id: string
  jobId: string
  resumeId: string | null
  overallScore: number
  skillsScore: number
  experienceScore: number
  educationScore: number
  projectScore: number
  keywordScore: number
  alignmentScore: number
  strongMatches: string[]
  partialMatches: string[]
  missingRequirements: string[]
  evidence: MatchEvidenceDto[]
  recommendations: string[]
  explanation: string
  createdAt: string
}
export interface MatchEvidenceDto {
  name: string
  status: string
  source: string
  detail: string
}

export interface SkillGapDto {
  id: string
  jobId: string
  jobTitle: string
  skillName: string
  gapType: string
  priority: string
  currentLevel: string
  requiredLevel: string
  recommendation: string
  learningPath: string
}

export interface TailoredResumeDto {
  id: string
  resumeId: string
  jobId: string
  jobTitle: string
  companyName: string
  mode: string
  changesSummary: string
  createdAt: string
}
export interface TailoredResumeDetailDto extends TailoredResumeDto {
  content: string
  originalContent: string
  separator: string
}
export interface CoverLetterDto {
  id: string
  jobId: string
  jobTitle: string
  companyName: string
  content: string
  length: string
  tone: string
  createdAt: string
}

export interface ApplicationDto {
  id: string
  jobId: string | null
  jobTitle: string
  companyName: string
  status: ApplicationStatus
  source: string
  appliedAt: string | null
  resumeName: string
  matchScore: number | null
  updatedAt: string | null
}
export interface ApplicationDetailDto extends ApplicationDto {
  jobUrl: string
  location: string
  followUpDate: string | null
  notes: string
  resumeId: string | null
  coverLetterId: string | null
  interviewCount: number
  lastInterviewAt: string | null
}
export interface CreateApplicationCommand {
  jobId?: string | null
  companyName: string
  jobTitle: string
  jobUrl: string
  location: string
  jobDescription?: string | null
  status: ApplicationStatus
  source: string
  appliedAt?: string | null
  resumeId?: string | null
  coverLetterId?: string | null
}

export interface InterviewQuestionDto {
  id: string
  question: string
  questionType: string
  order: number
  answerScore: number | null
  isAnswered: boolean
}
export interface InterviewSessionDto {
  id: string
  jobId: string
  jobTitle: string
  companyName: string
  mode: string
  overallScore: number | null
  summary: string | null
  startedAt: string
  completedAt: string | null
  questionCount: number
  answeredCount: number
  isCompleted: boolean
}
export interface InterviewSessionDetailDto {
  session: InterviewSessionDto
  questions: InterviewQuestionDto[]
}
export interface SubmitInterviewAnswerDto {
  questionId: string
  score: number
  relevanceScore: number
  clarityScore: number
  technicalScore: number
  structureScore: number
  specificityScore: number
  concisenessScore: number
  feedback: string
  improvementSuggestion: string
  followUpQuestion: string | null
  sessionCompleted: boolean
  sessionOverallScore: number | null
  sessionSummary: string | null
}

export interface RoadmapTaskDto {
  id: string
  title: string
  description: string
  month: string
  skill: string
  priority: string
  status: RoadmapTaskStatus
  dueDate: string | null
}
export interface RoadmapDto {
  id: string
  targetRole: string
  description: string
  createdAt: string
  tasks: RoadmapTaskDto[]
}

export interface CopilotMessageDto {
  id: string
  role: string
  content: string
  createdAt: string
}
export interface CopilotConversationDto {
  id: string
  title: string
  messageCount: number
  lastActivityAt: string
}
export interface CopilotReplyDto {
  conversationId: string
  message: CopilotMessageDto
}

export interface DashboardStatusCountDto {
  status: ApplicationStatus
  count: number
}
export interface DashboardTaskDto {
  title: string
  skill: string
  priority: string
  status: RoadmapTaskStatus
  dueDate: string | null
}
export interface DashboardApplicationDto {
  id: string
  jobTitle: string
  companyName: string
  status: ApplicationStatus
  matchScore: number | null
}
export interface DashboardDto {
  jobCount: number
  activeApplicationCount: number
  interviewCount: number
  resumeCount: number
  skillGapCount: number
  latestMatchScore: number | null
  lastJobMatchId: string | null
  recruiterReadinessScore: number | null
  applicationStatuses: DashboardStatusCountDto[]
  topSkillGaps: string[]
  upcomingTasks: DashboardTaskDto[]
  recentApplications: DashboardApplicationDto[]
}

export interface RecruiterReadinessDto {
  overallScore: number
  resumeScore: number
  skillsScore: number
  projectsScore: number
  profileScore: number
  interviewScore: number
  improvementActions: string[]
  calculatedAt: string
}