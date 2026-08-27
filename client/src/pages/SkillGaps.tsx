import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiErrorResponse } from '../lib/api'
import type { SkillGapDto } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, PageHeader, Spinner } from '../components/ui'

const gapColors: Record<string, string> = {
  High: 'rose',
  Medium: 'amber',
  Low: 'sky',
}

export function SkillGapsPage() {
  const { id } = useParams()
  const [gaps, setGaps] = useState<SkillGapDto[] | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api
      .get<SkillGapDto[]>(`/api/jobs/${id}/skill-gaps`)
      .then(setGaps)
      .catch((e: unknown) => {
        setError(e instanceof ApiErrorResponse && e.status === 400 ? (e as Error).message : e instanceof Error ? e.message : 'Failed to load gaps')
      })
  }, [id])

  return (
    <div>
      <PageHeader
        title="Skill Gaps"
        subtitle="Bridges between your profile and the job requirements"
        actions={
          <Link to={`/jobs/${id}`}>
            <Button variant="secondary">Back to job</Button>
          </Link>
        }
      />
      <ErrorAlert message={error} />
      {gaps === null ? (
        <Spinner />
      ) : gaps.length === 0 ? (
        <EmptyState title="No skill gaps" hint="Looks like a strong fit — or analyze the job first to detect requirements." />
      ) : (
        <div className="space-y-3">
          {gaps.map((g) => (
            <Card key={g.id} className="p-5">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-slate-800">{g.skillName}</h3>
                    <Badge color={gapColors[g.priority] ?? 'slate'}>{g.priority} priority</Badge>
                    <Badge>{g.gapType}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-slate-500">
                    Current: <span className="font-medium">{g.currentLevel || 'Not detected'}</span> → Required:{' '}
                    <span className="font-medium">{g.requiredLevel}</span>
                  </p>
                </div>
              </div>
              {g.recommendation && <p className="mt-2 text-sm text-slate-600">{g.recommendation}</p>}
              {g.learningPath && (
                <p className="mt-1 text-sm text-slate-400">
                  <span className="font-medium text-slate-500">Learning path:</span> {g.learningPath}
                </p>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}