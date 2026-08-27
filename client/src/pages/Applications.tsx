import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import type { ApplicationDto, ApplicationStatus, CreateApplicationCommand } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, Field, Input, PageHeader, Select, Spinner } from '../components/ui'

const statuses: ApplicationStatus[] = ['Saved', 'Applied', 'Screening', 'Interview', 'TechnicalRound', 'FinalRound', 'Offer', 'Rejected', 'Withdrawn']

const statusColors: Record<string, string> = {
  Saved: 'slate',
  Applied: 'sky',
  Screening: 'violet',
  Interview: 'amber',
  TechnicalRound: 'orange',
  FinalRound: 'rose',
  Offer: 'emerald',
  Rejected: 'rose',
  Withdrawn: 'slate',
}

export function ApplicationsPage() {
  const [items, setItems] = useState<ApplicationDto[] | null>(null)
  const [filter, setFilter] = useState<ApplicationStatus | ''>('')
  const [showForm, setShowForm] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [form, setForm] = useState<CreateApplicationCommand>({
    companyName: '',
    jobTitle: '',
    jobUrl: '',
    location: '',
    status: 'Saved',
    source: 'Manual',
    jobDescription: null,
  })

  async function load() {
    const q = filter ? `?status=${filter}` : ''
    api
      .get<{ items: ApplicationDto[] }>(`/api/applications${q}`)
      .then((r) => setItems(r.items))
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load applications'))
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter])

  async function create(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError('')
    try {
      await api.post<ApplicationDto>('/api/applications', form)
      setShowForm(false)
      setForm({ companyName: '', jobTitle: '', jobUrl: '', location: '', status: 'Saved', source: 'Manual', jobDescription: null })
      void load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add application')
    } finally {
      setBusy(false)
    }
  }

  function setField<K extends keyof CreateApplicationCommand>(key: K, value: CreateApplicationCommand[K]) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  return (
    <div>
      <PageHeader
        title="Applications"
        subtitle="Track every application in your pipeline"
        actions={
          <Button onClick={() => setShowForm((s) => !s)}>{showForm ? 'Close' : 'Add application'}</Button>
        }
      />
      <ErrorAlert message={error} />

      {showForm && (
        <Card className="mb-6 p-6">
          <form onSubmit={create} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Field label="Company">
              <Input required value={form.companyName} onChange={(e) => setField('companyName', e.target.value)} />
            </Field>
            <Field label="Job title">
              <Input required value={form.jobTitle} onChange={(e) => setField('jobTitle', e.target.value)} />
            </Field>
            <Field label="Location">
              <Input value={form.location} onChange={(e) => setField('location', e.target.value)} />
            </Field>
            <Field label="Job URL">
              <Input value={form.jobUrl} onChange={(e) => setField('jobUrl', e.target.value)} placeholder="https://…" />
            </Field>
            <Field label="Source">
              <Input value={form.source} onChange={(e) => setField('source', e.target.value)} placeholder="LinkedIn / Company site" />
            </Field>
            <Field label="Status">
              <Select value={form.status} onChange={(e) => setField('status', e.target.value as ApplicationStatus)}>
                {statuses.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </Select>
            </Field>
            <div className="flex items-end lg:col-span-2">
              <Button type="submit" disabled={busy} className="w-full sm:w-auto">
                {busy ? 'Saving…' : 'Save application'}
              </Button>
            </div>
          </form>
        </Card>
      )}

      <div className="mb-4 flex flex-wrap items-center gap-2">
        {(['', ...statuses] as const).map((s) => (
          <button
            key={s || 'all'}
            onClick={() => setFilter(s)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
              filter === s ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            {s || 'All'}
          </button>
        ))}
      </div>

      {items === null ? (
        <Spinner />
      ) : items.length === 0 ? (
        <EmptyState title="No applications" hint="Add your first application to start tracking." />
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-400">
              <tr>
                <th className="px-4 py-3">Role</th>
                <th className="px-4 py-3">Company</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Match</th>
                <th className="px-4 py-3">Applied</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {items.map((a) => (
                <tr key={a.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <Link to={`/applications/${a.id}`} className="font-medium text-slate-700 hover:text-indigo-600">
                      {a.jobTitle}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-slate-500">{a.companyName}</td>
                  <td className="px-4 py-3">
                    <Badge color={statusColors[a.status] ?? 'slate'}>{a.status}</Badge>
                  </td>
                  <td className="px-4 py-3 text-slate-600">{a.matchScore ?? '—'}</td>
                  <td className="px-4 py-3 text-slate-400">{a.appliedAt ? new Date(a.appliedAt).toLocaleDateString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}