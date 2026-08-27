import { Navigate, Route, Routes } from 'react-router-dom'
import type { ReactNode } from 'react'
import { AppLayout } from './components/Layout'
import { LoginPage } from './pages/Login'
import { RegisterPage } from './pages/Register'
import { DashboardPage } from './pages/Dashboard'
import { ProfilePage } from './pages/Profile'
import { ResumesPage } from './pages/Resumes'
import { ResumeDetailPage } from './pages/ResumeDetail'
import { JobsPage } from './pages/Jobs'
import { JobAddPage } from './pages/JobAdd'
import { JobDetailPage } from './pages/JobDetail'
import { MatchPage } from './pages/Match'
import { SkillGapsPage } from './pages/SkillGaps'
import { TailorPage } from './pages/Tailor'
import { CoverLetterPage } from './pages/CoverLetter'
import { ApplicationsPage } from './pages/Applications'
import { ApplicationDetailPage } from './pages/ApplicationDetail'
import { InterviewsPage } from './pages/Interviews'
import { InterviewSessionPage } from './pages/InterviewSession'
import { RoadmapPage } from './pages/Roadmap'
import { CopilotPage } from './pages/Copilot'
import { ReadinessPage } from './pages/Readiness'
import { SettingsPage } from './pages/Settings'
import { useAuth } from './lib/auth'

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  if (loading) return null
  if (!user) return <Navigate to="/login" replace />
  return <>{children}</>
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route
        element={
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/resumes" element={<ResumesPage />} />
        <Route path="/resumes/:id" element={<ResumeDetailPage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/add" element={<JobAddPage />} />
        <Route path="/jobs/:id" element={<JobDetailPage />} />
        <Route path="/jobs/:id/match" element={<MatchPage />} />
        <Route path="/jobs/:id/skill-gaps" element={<SkillGapsPage />} />
        <Route path="/jobs/:id/tailor" element={<TailorPage />} />
        <Route path="/jobs/:id/cover-letter" element={<CoverLetterPage />} />
        <Route path="/jobs/:id/interview" element={<InterviewSessionPage />} />
        <Route path="/applications" element={<ApplicationsPage />} />
        <Route path="/applications/:id" element={<ApplicationDetailPage />} />
        <Route path="/interviews" element={<InterviewsPage />} />
        <Route path="/roadmap" element={<RoadmapPage />} />
        <Route path="/copilot" element={<CopilotPage />} />
        <Route path="/readiness" element={<ReadinessPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}