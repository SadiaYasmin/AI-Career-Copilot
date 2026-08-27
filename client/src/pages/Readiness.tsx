import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import type { RecruiterReadinessDto } from '../lib/types'
import { Button, Card, EmptyState, ErrorAlert, PageHeader, Spinner } from '../components/ui'
import { ScoreRing } from '../components/ScoreRing'

export function ReadinessPage() {
  const [data, setData] = useState<RecruiterReadinessDto | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  function load(recalculate = false) {
    setError('')
    api
      .get<RecruiterReadinessDto>(`/api/readiness${recalculate ? '?recalculate=true' : ''}`)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load readiness'))
  }

  useEffect(() => {
    load()
  }, [])

  if (!data && !error) return <Spinner label="Calculating readiness…" />

  if (!data) return <EmptyState title="Readiness unavailable" hint={error} />

  const subs = [
    ['Resume', data.resumeScore],
    ['Skills', data.skillsScore],
    ['Projects', data.projectsScore],
    ['Profile', data.profileScore],
    ['Interview', data.interviewScore],
  ] as const

  return (
    <div>
      <PageHeader
        title="Recruiter Readiness"
        subtitle="How ready you are to apply, right now"
        actions={
          <Button variant="secondary" disabled={busy} onClick={() => { setBusy(true); load(true); setTimeout(() => setBusy(false), 1200) }}>
            Recalculate
          </Button>
        }
      />
      <ErrorAlert message={error} />

      <div className="flex flex-wrap items-center gap-8 rounded-xl border border-slate-200 bg-white p-8 shadow-sm">
        <ScoreRing value={data.overallScore} size={140} label="Overall readiness" />
        <div className="grid flex-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
          {subs.map(([label, score]) => (
            <div key={label} className="rounded-xl bg-slate-50 p-4 text-center">
              <p className="text-2xl font-bold text-slate-800">{score ?? '—'}</p>
              <p className="mt-1 text-xs text-slate-400">{label}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        {data.improvementActions.length > 0 && (
          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Improvement actions</h2>
            <ul className="space-y-2">
              {data.improvementActions.map((a) => (
                <li key={a} className="flex items-start gap-2 text-sm text-slate-600">
                  <span className="mt-1 h-1.5 w-1.5 shrink-0 rounded-full bg-amber-500" />
                  {a}
                </li>
              ))}
            </ul>
          </Card>
        )}
        <Card className="p-6">
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Readiness tips</h2>
          <ul className="space-y-2 text-sm text-slate-600">
            <li>• Keep your default resume fresh and analyzed.</li>
            <li>• Fill your profile — completeness lifts this score directly.</li>
            <li>• Practice interviews to keep your interview score high.</li>
          </ul>
          <p className="mt-4 text-xs text-slate-400">Calculated {new Date(data.calculatedAt).toLocaleString()}</p>
        </Card>
      </div>
    </div>
  )
}