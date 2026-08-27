import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { ApplicationDetailDto, ApplicationStatus } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, Field, Input, PageHeader, Spinner } from '../components/ui'

const statuses: ApplicationStatus[] = ['Saved', 'Applied', 'Screening', 'Interview', 'TechnicalRound', 'FinalRound', 'Offer', 'Rejected', 'Withdrawn']

export function ApplicationDetailPage() {
  const { id } = useParams()
  const [app, setApp] = useState<ApplicationDetailDto | null>(null)
  const [notes, setNotes] = useState('')
  const [followUp, setFollowUp] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function load() {
    if (!id) return
    try {
      const d = await api.get<ApplicationDetailDto>(`/api/applications/${id}`)
      setApp(d)
      setNotes(d.notes ?? '')
      setFollowUp(d.followUpDate ? d.followUpDate.slice(0, 10) : '')
    } catch {
      setApp(null)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function save() {
    if (!id) return
    setBusy(true)
    setError('')
    try {
      const d = await api.put<ApplicationDetailDto>(`/api/applications/${id}`, {
        notes,
        followUpDate: followUp ? new Date(followUp).toISOString() : null,
      })
      setApp(d)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save')
    } finally {
      setBusy(false)
    }
  }

  async function changeStatus(status: ApplicationStatus) {
    if (!id) return
    try {
      const d = await api.put<ApplicationDetailDto>(`/api/applications/${id}/status`, { newStatus: status })
      setApp(d)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update status')
    }
  }

  async function remove() {
    if (!id || !confirm('Delete this application?')) return
    await api.del(`/api/applications/${id}`).catch(() => undefined)
    window.location.href = '/applications'
  }

  if (app === null) return <EmptyState title="Application not found" />
  if (!app) return <Spinner label="Loading application…" />

  return (
    <div>
      <PageHeader
        title={app.jobTitle}
        subtitle={`${app.companyName}${app.location ? ` · ${app.location}` : ''}`}
        actions={
          <Link to="/applications">
            <Button variant="secondary">Back to applications</Button>
          </Link>
        }
      />
      <ErrorAlert message={error} />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Status</h2>
            <div className="flex flex-wrap gap-2">
              {statuses.map((s) => (
                <button
                  key={s}
                  onClick={() => void changeStatus(s)}
                  className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                    app.status === s ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {s}
                </button>
              ))}
            </div>
          </Card>

          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Details & follow-up</h2>
            <div className="space-y-4">
              <Field label="Notes">
                <Input type="text" value={notes} onChange={(e) => setNotes(e.target.value)} />
              </Field>
              <Field label="Follow-up date">
                <Input type="date" value={followUp} onChange={(e) => setFollowUp(e.target.value)} />
              </Field>
              <Button disabled={busy} onClick={() => void save()}>
                {busy ? 'Saving…' : 'Save details'}
              </Button>
            </div>
          </Card>

          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Prepare</h2>
            <div className="flex flex-wrap gap-2">
              {app.jobId && (
                <>
                  <Link to={`/jobs/${app.jobId}/interview`}>
                    <Button variant="secondary">Prepare interview</Button>
                  </Link>
                  <Link to={`/jobs/${app.jobId}/match`}>
                    <Button variant="secondary">View match</Button>
                  </Link>
                </>
              )}
              {app.jobUrl && (
                <a href={app.jobUrl} target="_blank" rel="noreferrer">
                  <Button variant="secondary">Open job posting</Button>
                </a>
              )}
            </div>
          </Card>
        </div>

        <div className="space-y-6">
          <Card className="p-6">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Snapshot</h2>
            <dl className="space-y-2 text-sm">
              <Row label="Status" value={<Badge>{app.status}</Badge>} />
              <Row label="Match" value={app.matchScore !== null ? `${app.matchScore}%` : '—'} />
              <Row label="Applied" value={app.appliedAt ? new Date(app.appliedAt).toLocaleDateString() : '—'} />
              <Row label="Updated" value={app.updatedAt ? new Date(app.updatedAt).toLocaleDateString() : '—'} />
              <Row label="Resume" value={app.resumeName || '—'} />
              <Row label="Interviews" value={`${app.interviewCount}`} />
              <Row label="Source" value={app.source || '—'} />
            </dl>
          </Card>
          <Button variant="danger" className="w-full" onClick={() => void remove()}>
            Delete application
          </Button>
        </div>
      </div>
    </div>
  )
}

function Row({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="text-slate-400">{label}</dt>
      <dd className="font-medium text-slate-700">{value}</dd>
    </div>
  )
}