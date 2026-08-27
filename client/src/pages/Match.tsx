import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiErrorResponse } from '../lib/api'
import type { JobMatchDto } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, PageHeader, Spinner } from '../components/ui'
import { ScoreRing } from '../components/ScoreRing'

export function MatchPage() {
  const { id } = useParams()
  const [match, setMatch] = useState<JobMatchDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(() => {
    setLoading(true)
    setError('')
    api
      .get<JobMatchDto>(`/api/jobs/${id}/match`)
      .then(setMatch)
      .catch((e: unknown) => {
        if (e instanceof ApiErrorResponse && e.status === 404) {
          setMatch(null)
        } else {
          setError(e instanceof Error ? e.message : 'Failed to load match')
        }
      })
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    load()
  }, [load])

  async function recalc() {
    setLoading(true)
    setError('')
    try {
      const m = await api.post<JobMatchDto>(`/api/jobs/${id}/match`)
      setMatch(m)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to calculate match')
    } finally {
      setLoading(false)
    }
  }

  if (loading) return <Spinner label="Scoring your match…" />

  return (
    <div>
      <PageHeader
        title="Job Match"
        subtitle="How your profile aligns with this role"
        actions={
          <Link to={`/jobs/${id}`}>
            <Button variant="secondary">Back to job</Button>
          </Link>
        }
      />
      <ErrorAlert message={error} />

      {!match ? (
        <EmptyState title="No match yet" hint="Calculate a fresh match score between your profile and this job." />
      ) : (
        <div className="space-y-6">
          <div className="flex flex-wrap items-center gap-6 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
            <ScoreRing value={match.overallScore} label="Overall" />
            <div className="grid flex-1 gap-2 sm:grid-cols-3">
              <MiniStat label="Skills" value={match.skillsScore} />
              <MiniStat label="Experience" value={match.experienceScore} />
              <MiniStat label="Education" value={match.educationScore} />
              <MiniStat label="Projects" value={match.projectScore} />
              <MiniStat label="Keywords" value={match.keywordScore} />
              <MiniStat label="Alignment" value={match.alignmentScore} />
            </div>
          </div>

          {match.explanation && (
            <Card className="p-6">
              <h2 className="mb-2 text-sm font-semibold text-slate-700">How you fit</h2>
              <p className="text-sm text-slate-600">{match.explanation}</p>
            </Card>
          )}

          <div className="grid gap-6 lg:grid-cols-3">
            <Card className="p-6">
              <h2 className="mb-3 text-sm font-semibold text-slate-700">
                Strong matches <Badge color="emerald">{match.strongMatches.length}</Badge>
              </h2>
              <ul className="list-disc space-y-1 pl-5 text-sm text-slate-600">
                {match.strongMatches.map((s) => (
                  <li key={s}>{s}</li>
                ))}
                {match.strongMatches.length === 0 && <li className="text-slate-400">None detected</li>}
              </ul>
            </Card>
            <Card className="p-6">
              <h2 className="mb-3 text-sm font-semibold text-slate-700">
                Partial matches <Badge color="amber">{match.partialMatches.length}</Badge>
              </h2>
              <ul className="list-disc space-y-1 pl-5 text-sm text-slate-600">
                {match.partialMatches.map((s) => (
                  <li key={s}>{s}</li>
                ))}
                {match.partialMatches.length === 0 && <li className="text-slate-400">None detected</li>}
              </ul>
            </Card>
            <Card className="p-6">
              <h2 className="mb-3 text-sm font-semibold text-slate-700">
                Missing <Badge color="rose">{match.missingRequirements.length}</Badge>
              </h2>
              <ul className="list-disc space-y-1 pl-5 text-sm text-slate-600">
                {match.missingRequirements.map((s) => (
                  <li key={s}>{s}</li>
                ))}
                {match.missingRequirements.length === 0 && <li className="text-slate-400">Nothing missing</li>}
              </ul>
            </Card>
          </div>

          {match.evidence.length > 0 && (
            <Card className="p-6">
              <h2 className="mb-3 text-sm font-semibold text-slate-700">Evidence</h2>
              <div className="divide-y divide-slate-100">
                {match.evidence.map((e) => (
                  <div key={e.name} className="flex items-start justify-between gap-4 py-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-slate-700">{e.name}</p>
                      <p className="text-xs text-slate-400">{e.detail}</p>
                    </div>
                    <Badge color={e.status === 'Strong' ? 'emerald' : e.status === 'Partial' ? 'amber' : 'rose'}>{e.status}</Badge>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {match.recommendations.length > 0 && (
            <Card className="p-6">
              <h2 className="mb-3 text-sm font-semibold text-slate-700">Recommendations</h2>
              <ul className="space-y-2">
                {match.recommendations.map((r) => (
                  <li key={r} className="flex items-start gap-2 text-sm text-slate-600">
                    <span className="mt-1 h-1.5 w-1.5 shrink-0 rounded-full bg-indigo-500" />
                    {r}
                  </li>
                ))}
              </ul>
            </Card>
          )}

          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => void recalc()}>
              Recalculate match
            </Button>
            <Link to={`/jobs/${id}/skill-gaps`}>
              <Button variant="secondary">View skill gaps</Button>
            </Link>
          </div>
        </div>
      )}

      {!match && (
        <Button className="mt-4" onClick={() => void recalc()}>
          Calculate match
        </Button>
      )}
    </div>
  )
}

function MiniStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg bg-slate-50 px-3 py-2">
      <p className="text-xs text-slate-400">{label}</p>
      <p className="text-xl font-bold text-slate-800">{value ?? '—'}</p>
    </div>
  )
}