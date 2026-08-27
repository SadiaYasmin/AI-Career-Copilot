import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import type { InterviewSessionDto, JobDto } from '../lib/types'
import { Badge, Button, Card, EmptyState, Field, PageHeader, Select, Spinner } from '../components/ui'

export function InterviewsPage() {
  const [jobs, setJobs] = useState<JobDto[] | null>(null)
  const [selected, setSelected] = useState('')
  const [sessions, setSessions] = useState<InterviewSessionDto[] | null>(null)

  useEffect(() => {
    api
      .get<{ items: JobDto[] }>('/api/jobs')
      .then((r) => {
        setJobs(r.items)
        if (r.items[0]) setSelected(r.items[0].id)
      })
      .catch(() => setJobs([]))
  }, [])

  useEffect(() => {
    if (!selected) return
    api
      .get<InterviewSessionDto[]>(`/api/interviews?jobId=${selected}`)
      .then(setSessions)
      .catch(() => setSessions([]))
  }, [selected])

  const currentJob = jobs?.find((j) => j.id === selected)

  return (
    <div>
      <PageHeader title="Interview Sessions" subtitle="All your practice sessions by job" />
      <Card className="mb-6 p-6">
        <div className="flex flex-wrap items-end gap-4">
          <Field label="Job">
            <Select value={selected} onChange={(e) => setSelected(e.target.value)} className="w-72">
              {(jobs ?? []).map((j) => (
                <option key={j.id} value={j.id}>
                  {j.title} — {j.companyName}
                </option>
              ))}
            </Select>
          </Field>
          {currentJob && (
            <Link to={`/jobs/${currentJob.id}/interview`}>
              <Button>Prepare for this job</Button>
            </Link>
          )}
        </div>
      </Card>

      {jobs === null ? (
        <Spinner />
      ) : (jobs ?? []).length === 0 ? (
        <EmptyState title="Add a job first" hint="You need at least one job to practice interviews." />
      ) : sessions === null ? (
        <Spinner />
      ) : sessions.length === 0 ? (
        <EmptyState title="No sessions yet" hint="Start an interview session from the job or the button above." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sessions.map((s) => (
            <Card key={s.id} className="p-5">
              <div className="flex items-start justify-between">
                <h3 className="font-semibold text-slate-800">{s.mode}</h3>
                {s.isCompleted ? (
                  <Badge color="emerald">{s.overallScore ?? '—'}/100</Badge>
                ) : (
                  <Badge color="amber">In progress</Badge>
                )}
              </div>
              <p className="mt-1 text-sm text-slate-500">
                {s.jobTitle} · {s.companyName}
              </p>
              <p className="mt-3 text-xs text-slate-400">
                {s.answeredCount}/{s.questionCount} answered
                {s.completedAt ? ` · completed ${new Date(s.completedAt).toLocaleDateString()}` : ` · started ${new Date(s.startedAt).toLocaleDateString()}`}
              </p>
              <Link to={`/jobs/${s.jobId}/interview`}>
                <Button variant="secondary" className="mt-4 w-full">
                  {s.isCompleted ? 'Review session' : 'Continue'}
                </Button>
              </Link>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}