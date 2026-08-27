import { useEffect, useState } from 'react'
import { api, ApiErrorResponse } from '../lib/api'
import type { RoadmapDto, RoadmapTaskDto, RoadmapTaskStatus } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, Field, Input, PageHeader, Spinner } from '../components/ui'

const statusStyles: Record<string, string> = {
  Pending: 'border-slate-300 bg-white',
  InProgress: 'border-amber-300 bg-amber-50',
  Completed: 'border-emerald-300 bg-emerald-50',
}

export function RoadmapPage() {
  const [roadmap, setRoadmap] = useState<RoadmapDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [targetRole, setTargetRole] = useState('')
  const [busy, setBusy] = useState(false)

  const load = () => {
    setLoading(true)
    setError('')
    api
      .get<RoadmapDto>('/api/roadmaps')
      .then(setRoadmap)
      .catch((e: unknown) => {
        if (e instanceof ApiErrorResponse && e.status === 404) setRoadmap(null)
        else setError(e instanceof Error ? e.message : 'Failed to load roadmap')
      })
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  async function generate() {
    setBusy(true)
    setError('')
    try {
      const r = await api.post<RoadmapDto>('/api/roadmaps', { targetRole })
      setRoadmap(r)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to generate roadmap')
    } finally {
      setBusy(false)
    }
  }

  async function updateStatus(task: RoadmapTaskDto, status: RoadmapTaskStatus) {
    if (!roadmap) return
    try {
      const updated = await api.put<RoadmapTaskDto>(`/api/roadmaps/tasks/${task.id}`, { newStatus: status })
      setRoadmap({ ...roadmap, tasks: roadmap.tasks.map((t) => (t.id === task.id ? updated : t)) })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update task')
    }
  }

  if (loading) return <Spinner label="Loading your roadmap…" />

  const byMonth = (roadmap?.tasks ?? []).reduce<Record<string, RoadmapTaskDto[]>>((acc, t) => {
    const key = t.month || 'Ungrouped'
    ;(acc[key] ??= []).push(t)
    return acc
  }, {})

  return (
    <div>
      <PageHeader title="Career Roadmap" subtitle="Step-by-step plan to reach your target role" />
      <ErrorAlert message={error} />

      {!roadmap ? (
        <Card className="mx-auto max-w-md p-6">
          <h2 className="mb-1 text-lg font-semibold text-slate-800">Generate your roadmap</h2>
          <p className="mb-4 text-sm text-slate-500">Based on your profile and target role, Copilot builds a personalized plan.</p>
          <Field label="Target role (optional)">
            <Input value={targetRole} onChange={(e) => setTargetRole(e.target.value)} placeholder="e.g. Staff Engineer" />
          </Field>
          <Button className="mt-4 w-full" disabled={busy} onClick={() => void generate()}>
            {busy ? 'Building roadmap…' : 'Generate roadmap'}
          </Button>
        </Card>
      ) : (
        <div>
          <Card className="mb-6 p-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold text-slate-800">{roadmap.targetRole || 'Your roadmap'}</h2>
                <p className="mt-1 text-sm text-slate-500">{roadmap.description}</p>
              </div>
              <Button
                variant="secondary"
                onClick={() => {
                  setRoadmap(null)
                  setTargetRole(roadmap.targetRole)
                }}
              >
                Regenerate
              </Button>
            </div>
          </Card>

          {Object.keys(byMonth).length === 0 ? (
            <EmptyState title="No tasks yet" />
          ) : (
            Object.entries(byMonth).map(([month, tasks]) => (
              <div key={month} className="mb-6">
                <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{month}</h3>
                <div className="grid gap-4 md:grid-cols-2">
                  {tasks.map((t) => (
                    <Card key={t.id} className={`border p-5 ${statusStyles[t.status]}`}>
                      <div className="flex items-start justify-between gap-3">
                        <h4 className="font-semibold text-slate-800">{t.title}</h4>
                        <Badge color={t.priority === 'High' ? 'rose' : t.priority === 'Medium' ? 'amber' : 'slate'}>{t.priority}</Badge>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{t.description}</p>
                      <p className="mt-2 text-xs text-slate-400">
                        {t.skill && <span className="mr-2">Skill: {t.skill}</span>}
                        {t.dueDate && <span>Due: {new Date(t.dueDate).toLocaleDateString()}</span>}
                      </p>
                      <div className="mt-4 flex flex-wrap gap-2">
                        {(['Pending', 'InProgress', 'Completed'] as const).map((s) => (
                          <button
                            key={s}
                            onClick={() => void updateStatus(t, s)}
                            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                              t.status === s ? 'bg-indigo-600 text-white' : 'bg-white text-slate-500 ring-1 ring-slate-300 hover:bg-slate-50'
                            }`}
                          >
                            {s}
                          </button>
                        ))}
                      </div>
                    </Card>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  )
}