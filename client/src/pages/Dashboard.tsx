import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ResponsiveContainer, Bar, BarChart, Cell, XAxis, YAxis, Tooltip } from 'recharts'
import { api } from '../lib/api'
import type { DashboardDto, JobDto } from '../lib/types'
import { Button, Card, EmptyState, PageHeader, Spinner, Badge } from '../components/ui'
import { ScoreRing } from '../components/ScoreRing'

const statusColors: Record<string, string> = {
  Saved: '#64748b',
  Applied: '#0ea5e9',
  Screening: '#8b5cf6',
  Interview: '#f59e0b',
  TechnicalRound: '#f97316',
  FinalRound: '#ef4444',
  Offer: '#059669',
  Rejected: '#e2e8f0',
  Withdrawn: '#cbd5e1',
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardDto | null>(null)
  const [jobs, setJobs] = useState<JobDto[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    void api
      .get<DashboardDto>('/api/dashboard')
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load dashboard'))
    void api
      .get<{ items: JobDto[] }>('/api/jobs?page=1&pageSize=8')
      .then((r) => setJobs(r.items))
      .catch(() => undefined)
  }, [])

  if (error) return <EmptyState title="Could not load the dashboard" hint={error} />
  if (!data) return <Spinner label="Building your dashboard…" />

  const cards = [
    { label: 'Recruiter Readiness', value: data.recruiterReadinessScore, to: '/readiness' },
    { label: 'Best Match', value: data.latestMatchScore, to: data.lastJobMatchId ? `/jobs/${data.lastJobMatchId}/match` : '/jobs' },
    { label: 'Active Applications', value: data.activeApplicationCount, to: '/applications' },
    { label: 'Interviews', value: data.interviewCount, to: '/interviews' },
  ]

  const chartData = data.applicationStatuses.map((s) => ({ name: s.status, count: s.count }))

  return (
    <div>
      <PageHeader
        title="Dashboard"
        subtitle="Your job search at a glance"
        actions={
          <Link to="/jobs/add">
            <Button>Analyze a Job</Button>
          </Link>
        }
      />

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {cards.map((c) => (
          <Link key={c.label} to={c.to} className="block">
            <Card className="p-5 transition-shadow hover:shadow-md">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{c.label}</p>
              <p className="mt-2 text-3xl font-bold text-slate-900">{c.value ?? '—'}</p>
            </Card>
          </Link>
        ))}
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <Card className="p-5">
          <h2 className="mb-4 text-sm font-semibold text-slate-700">Applications by stage</h2>
          {chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={chartData} margin={{ top: 0, right: 0, left: -28, bottom: 0 }}>
                <XAxis dataKey="name" tick={{ fontSize: 10 }} interval={0} angle={-28} textAnchor="end" height={48} />
                <YAxis tick={{ fontSize: 10 }} allowDecimals={false} />
                <Tooltip cursor={{ fill: 'rgba(99,102,241,0.08)' }} />
                <Bar dataKey="count" radius={[4, 4, 0, 0]}>
                  {chartData.map((entry) => (
                    <Cell key={entry.name} fill={statusColors[entry.name] ?? '#64748b'} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <p className="py-10 text-center text-sm text-slate-400">No applications yet</p>
          )}
        </Card>

        <Card className="p-5">
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Top skill gaps</h2>
          {data.topSkillGaps.length > 0 ? (
            <ul className="space-y-2">
              {data.topSkillGaps.map((g) => (
                <li key={g} className="flex items-center justify-between rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
                  <span>{g}</span>
                  <Badge color="amber">Gap</Badge>
                </li>
              ))}
            </ul>
          ) : (
            <p className="py-6 text-center text-sm text-slate-400">No gaps detected — nice work</p>
          )}

          <h2 className="mb-3 mt-6 text-sm font-semibold text-slate-700">Upcoming roadmap tasks</h2>
          {data.upcomingTasks.length > 0 ? (
            <ul className="space-y-2">
              {data.upcomingTasks.slice(0, 4).map((t) => (
                <li key={t.title} className="flex items-center gap-2 text-sm text-slate-600">
                  <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-indigo-500" />
                  <span className="truncate">{t.title}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="py-3 text-center text-sm text-slate-400">Generate a roadmap to see tasks</p>
          )}
        </Card>

        <Card className="p-5">
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Recent applications</h2>
          {data.recentApplications.length > 0 ? (
            <ul className="space-y-2">
              {data.recentApplications.map((a) => (
                <li key={a.id}>
                  <Link to={`/applications/${a.id}`} className="flex items-center justify-between rounded-lg px-3 py-2 hover:bg-slate-50">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-slate-700">{a.jobTitle}</p>
                      <p className="truncate text-xs text-slate-400">{a.companyName}</p>
                    </div>
                    <Badge color={a.status === 'Offer' ? 'emerald' : a.status === 'Rejected' ? 'rose' : 'indigo'}>
                      {a.status}
                    </Badge>
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <p className="py-6 text-center text-sm text-slate-400">No tracked applications</p>
          )}

          <h2 className="mb-3 mt-6 flex items-center justify-between text-sm font-semibold text-slate-700">
            <span>Average match</span>
            {data.latestMatchScore !== null && data.latestMatchScore !== undefined && (
              <ScoreRing value={data.latestMatchScore} size={56} />
            )}
          </h2>
          <div className="space-y-2">
            {jobs.slice(0, 4).map((j) => (
              <Link key={j.id} to={`/jobs/${j.id}`} className="flex items-center justify-between rounded-lg px-3 py-2 hover:bg-slate-50">
                <span className="truncate text-sm text-slate-600">{j.title}</span>
                {j.latestMatchScore !== null && j.latestMatchScore !== undefined && (
                  <span className="text-sm font-semibold text-slate-800">{j.latestMatchScore}</span>
                )}
              </Link>
            ))}
          </div>
        </Card>
      </div>
    </div>
  )
}