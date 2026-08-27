import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { JobDetailDto } from '../lib/types'
import { Badge, Button, Card, PageHeader, Spinner } from '../components/ui'
import { ScoreRing } from '../components/ScoreRing'

export function JobDetailPage() {
  const { id } = useParams()
  const [job, setJob] = useState<JobDetailDto | null>(null)

  useEffect(() => {
    if (!id) return
    void api.get<JobDetailDto>(`/api/jobs/${id}`).then(setJob).catch(() => undefined)
  }, [id])

  if (!job) return <Spinner label="Loading job…" />

  const required = job.requirements.filter((r) => r.requirementType === 'Required')
  const preferred = job.requirements.filter((r) => r.requirementType === 'Preferred')
  const inferred = job.requirements.filter((r) => r.requirementType === 'Inferred')

  return (
    <div>
      <PageHeader
        title={job.title}
        subtitle={`${job.companyName} · ${job.location || 'Remote'} · ${job.employmentType || '—'}`}
        actions={
          <Link to="/jobs">
            <Button variant="secondary">Back to jobs</Button>
          </Link>
        }
      />

      {job.latestMatchScore !== null && job.latestMatchScore !== undefined && (
        <div className="mb-6 flex items-center gap-6 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <ScoreRing value={job.latestMatchScore} label="Match" />
          <div>
            <p className="text-sm font-medium text-slate-700">Latest match score</p>
            <Link to={`/jobs/${job.id}/match`} className="text-sm font-medium text-indigo-600 hover:text-indigo-700">
              View full match →
            </Link>
          </div>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-6">
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Overview</h2>
          <p className="whitespace-pre-wrap text-sm text-slate-600">{job.description || 'No description provided.'}</p>
        </Card>

        <div className="space-y-6">
          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Required skills</h2>
            {required.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {required.map((r) => (
                  <Badge key={r.id} color="rose">
                    {r.name}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="text-sm text-slate-400">{job.isAnalyzed ? 'Analyze again with AI for deeper extraction.' : 'Analyze this job to extract requirements.'}</p>
            )}
            {preferred.length > 0 && (
              <>
                <h3 className="mb-2 mt-4 text-sm font-semibold text-slate-500">Preferred</h3>
                <div className="flex flex-wrap gap-2">
                  {preferred.map((r) => (
                    <Badge key={r.id} color="slate">
                      {r.name}
                    </Badge>
                  ))}
                </div>
              </>
            )}
            {inferred.length > 0 && (
              <>
                <h3 className="mb-2 mt-4 text-sm font-semibold text-slate-500">Inferred</h3>
                <div className="flex flex-wrap gap-2">
                  {inferred.map((r) => (
                    <Badge key={r.id} color="indigo">
                      {r.name}
                    </Badge>
                  ))}
                </div>
              </>
            )}
          </Card>

          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Actions</h2>
            <div className="grid grid-cols-2 gap-2">
              <Link to={`/jobs/${job.id}/match`}>
                <Button variant="secondary" className="w-full">View match</Button>
              </Link>
              <Link to={`/jobs/${job.id}/skill-gaps`}>
                <Button variant="secondary" className="w-full">Skill gaps</Button>
              </Link>
              <Link to={`/jobs/${job.id}/tailor`}>
                <Button variant="secondary" className="w-full">Tailor resume</Button>
              </Link>
              <Link to={`/jobs/${job.id}/cover-letter`}>
                <Button variant="secondary" className="w-full">Cover letter</Button>
              </Link>
              <Link to={`/jobs/${job.id}/interview`} className="col-span-2">
                <Button className="w-full">Prepare interview</Button>
              </Link>
            </div>
          </Card>
        </div>
      </div>
    </div>
  )
}