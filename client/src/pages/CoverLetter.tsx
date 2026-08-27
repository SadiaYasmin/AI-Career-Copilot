import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { CoverLetterDto } from '../lib/types'
import { Button, Card, ErrorAlert, Field, Label, PageHeader, Select } from '../components/ui'

const tones = ['Professional', 'Friendly', 'Confident', 'Enthusiastic', 'Formal']
const lengths = ['Concise', 'Standard', 'Detailed']

export function CoverLetterPage() {
  const { id } = useParams()
  const [result, setResult] = useState<CoverLetterDto | null>(null)
  const [history, setHistory] = useState<CoverLetterDto[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [form, setForm] = useState({ length: 'Standard', tone: 'Professional' })

  useEffect(() => {
    void api.get<CoverLetterDto[]>('/api/tailored/cover-letters').then((r) => setHistory(r.filter((x) => x.jobId === id))).catch(() => undefined)
  }, [id])

  async function generate() {
    setBusy(true)
    setError('')
    try {
      const res = await api.post<CoverLetterDto>(`/api/jobs/${id}/cover-letter`, form)
      setResult(res)
      setHistory((h) => [res, ...h])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Cover letter generation failed')
    } finally {
      setBusy(false)
    }
  }

  async function open(item: CoverLetterDto) {
    try {
      const d = await api.post<CoverLetterDto>(`/api/jobs/${id}/cover-letter`, { length: item.length, tone: item.tone })
      setResult(d)
    } catch {
      setResult(item)
    }
  }

  return (
    <div>
      <PageHeader
        title="Cover Letter"
        subtitle="Generate a tailored cover letter for this job"
        actions={
          <Link to={`/jobs/${id}`}>
            <Button variant="secondary">Back to job</Button>
          </Link>
        }
      />

      <Card className="mx-auto mb-6 max-w-xl p-6">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Length">
            <Select value={form.length} onChange={(e) => setForm((f) => ({ ...f, length: e.target.value }))}>
              {lengths.map((l) => (
                <option key={l}>{l}</option>
              ))}
            </Select>
          </Field>
          <Field label="Tone">
            <Select value={form.tone} onChange={(e) => setForm((f) => ({ ...f, tone: e.target.value }))}>
              {tones.map((t) => (
                <option key={t}>{t}</option>
              ))}
            </Select>
          </Field>
        </div>
        <Button className="mt-4" disabled={busy} onClick={() => void generate()}>
          {busy ? 'Writing…' : 'Generate cover letter'}
        </Button>
        <ErrorAlert message={error} />
      </Card>

      {result && (
        <Card className="p-6">
          <div className="mb-3 flex items-center justify-between">
            <Label>{result.length} · {result.tone} · {new Date(result.createdAt).toLocaleString()}</Label>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                onClick={() => {
                  navigator.clipboard.writeText(result.content).catch(() => undefined)
                }}
              >
                Copy
              </Button>
              <Button
                variant="secondary"
                onClick={() => {
                  const blob = new Blob([result.content], { type: 'text/plain' })
                  const a = document.createElement('a')
                  a.href = URL.createObjectURL(blob)
                  a.download = 'cover-letter.txt'
                  a.click()
                }}
              >
                Download
              </Button>
            </div>
          </div>
          <pre className="whitespace-pre-wrap font-serif text-sm leading-6 text-slate-700">{result.content}</pre>
        </Card>
      )}

      {history.length > 0 && !result && (
        <div className="mt-6">
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Previous versions</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {history.map((h) => (
              <button key={h.id} onClick={() => void open(h)} className="rounded-xl border border-slate-200 bg-white p-4 text-left shadow-sm hover:shadow-md">
                <p className="text-sm font-medium text-slate-700">{h.companyName}</p>
                <p className="mt-1 text-xs text-slate-400">{h.length} · {h.tone}</p>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}