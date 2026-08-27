import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import type { JobDto } from '../lib/types'
import { Badge, Button, Card, EmptyState, PageHeader, Spinner } from '../components/ui'

export function JobsPage() {
  const [items, setItems] = useState<JobDto[] | null>(null)

  const load = useCallback(() => {
    api
      .get<{ items: JobDto[] }>('/api/jobs')
      .then((r) => setItems(r.items))
      .catch(() => setItems([]))
  }, [])

  useEffect(load, [load])

  return (
    <div>
      <PageHeader
        title="Jobs"
        subtitle="Track roles and dive into matches"
        actions={
          <Link to="/jobs/add">
            <Button>Add job</Button>
          </Link>
        }
      />
      {items === null ? (
        <Spinner />
      ) : items.length === 0 ? (
        <EmptyState title="No jobs yet" hint="Paste a job description to get analysis, matching and tailoring." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((j) => (
            <Link key={j.id} to={`/jobs/${j.id}`}>
              <Card className="p-5 transition-shadow hover:shadow-md">
                <div className="flex items-start justify-between gap-2">
                  <h3 className="font-semibold text-slate-800">{j.title}</h3>
                  {j.latestMatchScore !== null && j.latestMatchScore !== undefined && (
                    <Badge color={j.latestMatchScore >= 70 ? 'emerald' : j.latestMatchScore >= 40 ? 'amber' : 'rose'}>
                      {j.latestMatchScore}%
                    </Badge>
                  )}
                </div>
                <p className="mt-1 text-sm text-slate-500">{j.companyName}</p>
                <p className="mt-0.5 text-xs text-slate-400">
                  {j.location || 'Remote'} · {j.employmentType || '—'}
                </p>
                <p className="mt-3 text-xs text-slate-400">
                  {j.isAnalyzed ? <Badge color="indigo">Analyzed</Badge> : <Badge color="slate">Not analyzed</Badge>}
                </p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}