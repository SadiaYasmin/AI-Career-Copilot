import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import type { JobDetailDto, JobDto } from '../lib/types'
import { Button, Card, ErrorAlert, Field, Input, PageHeader, Textarea } from '../components/ui'

export function JobAddPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({
    title: '',
    companyName: '',
    location: '',
    employmentType: '',
    description: '',
    sourceUrl: '',
  })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError('')
    try {
      const job = await api.post<JobDto>('/api/jobs', form)
      await api.post<JobDetailDto>(`/api/jobs/${job.id}/analyze`).catch(() => undefined)
      navigate(`/jobs/${job.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save job')
    } finally {
      setBusy(false)
    }
  }

  function set<K extends keyof typeof form>(key: K, value: string) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader title="Add a job" subtitle="Paste the posting and Copilot will analyze it" />
      <form onSubmit={onSubmit} className="space-y-4">
        <Card className="space-y-4 p-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Job title">
              <Input required value={form.title} onChange={(e) => set('title', e.target.value)} placeholder="Senior Backend Engineer" />
            </Field>
            <Field label="Company">
              <Input value={form.companyName} onChange={(e) => set('companyName', e.target.value)} placeholder="Example Corp" />
            </Field>
            <Field label="Location">
              <Input value={form.location} onChange={(e) => set('location', e.target.value)} placeholder="Remote / Berlin" />
            </Field>
            <Field label="Employment type">
              <Input value={form.employmentType} onChange={(e) => set('employmentType', e.target.value)} placeholder="Full-time" />
            </Field>
          </div>
          <Field label="Source URL">
            <Input value={form.sourceUrl} onChange={(e) => set('sourceUrl', e.target.value)} placeholder="https://…" />
          </Field>
          <Field label="Job description">
            <Textarea rows={10} required value={form.description} onChange={(e) => set('description', e.target.value)} placeholder="Paste the full job posting here." />
          </Field>
        </Card>
        <ErrorAlert message={error} />
        <div className="flex gap-2">
          <Button type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save & analyze'}
          </Button>
          <Button type="button" variant="secondary" onClick={() => navigate('/jobs')}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  )
}