import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { ResumeDto, TailoredResumeDetailDto, TailoredResumeDto, TailoringMode } from '../lib/types'
import { Button, Card, ErrorAlert, Field, Label, PageHeader, Select, Spinner } from '../components/ui'

export function TailorPage() {
  const { id } = useParams()
  const [resumes, setResumes] = useState<ResumeDto[] | null>(null)
  const [history, setHistory] = useState<TailoredResumeDto[]>([])
  const [result, setResult] = useState<TailoredResumeDetailDto | null>(null)
  const [resumeId, setResumeId] = useState('')
  const [mode, setMode] = useState<TailoringMode>('Balanced')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    void api
      .get<{ items: ResumeDto[] }>('/api/resumes')
      .then((r) => {
        setResumes(r.items)
        const def = r.items.find((x) => x.isDefault) ?? r.items[0]
        if (def) setResumeId(def.id)
      })
      .catch(() => setResumes([]))
    void api
      .get<TailoredResumeDto[]>('/api/tailored/resumes')
      .then((r) => setHistory(r.filter((x) => x.jobId === id)))
      .catch(() => setHistory([]))
  }, [id])

  async function generate() {
    setBusy(true)
    setError('')
    try {
      const res = await api.post<TailoredResumeDetailDto>(`/api/jobs/${id}/tailor-resume`, { resumeId, mode })
      setResult(res)
      setHistory((h) => [res, ...h])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Tailoring failed')
    } finally {
      setBusy(false)
    }
  }

  async function open(item: TailoredResumeDto) {
    try {
      const d = await api.get<TailoredResumeDetailDto>(`/api/tailored/resumes/${item.id}`)
      setResult(d)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load tailored resume')
    }
  }

  return (
    <div>
      <PageHeader
        title="Tailor Resume"
        subtitle="Rewrite your resume for this specific job"
        actions={
          <Link to={`/jobs/${id}`}>
            <Button variant="secondary">Back to job</Button>
          </Link>
        }
      />

      <Card className="mx-auto mb-6 max-w-xl p-6">
        <div className="space-y-4">
          <Field label="Resume">
            {resumes === null ? (
              <Spinner />
            ) : resumes.length === 0 ? (
              <p className="text-sm text-slate-400">Upload a resume first.</p>
            ) : (
              <Select value={resumeId} onChange={(e) => setResumeId(e.target.value)}>
                {resumes.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.originalFileName}
                  </option>
                ))}
              </Select>
            )}
          </Field>
          <Field label="Tailoring mode">
            <Select value={mode} onChange={(e) => setMode(e.target.value as TailoringMode)}>
              <option value="Conservative">Conservative — keep as-is, small tweaks</option>
              <option value="Balanced">Balanced — keep original meaning, add job keywords</option>
              <option value="Aggressive">Aggressive — heavy rewrite for keywords</option>
            </Select>
          </Field>
          <Button disabled={busy || resumes === null || resumes.length === 0} onClick={() => void generate()}>
            {busy ? 'Tailoring…' : 'Generate tailored resume'}
          </Button>
        </div>
        <ErrorAlert message={error} />
      </Card>

      {result && <DiffView result={result} />}

      {history.length > 0 && !result && (
        <div>
          <h2 className="mb-3 text-sm font-semibold text-slate-700">Previous versions</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {history.map((h) => (
              <button key={h.id} onClick={() => void open(h)} className="rounded-xl border border-slate-200 bg-white p-4 text-left shadow-sm hover:shadow-md">
                <p className="text-sm font-medium text-slate-700">{h.jobTitle} · {h.companyName}</p>
                <p className="mt-1 text-xs text-slate-400">
                  {h.mode} · {new Date(h.createdAt).toLocaleDateString()}
                </p>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function DiffView({ result }: { result: TailoredResumeDetailDto }) {
  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <Label>{result.mode} tailoring · {new Date(result.createdAt).toLocaleString()}</Label>
        <Button
          variant="secondary"
          onClick={() => {
            const blob = new Blob([result.content], { type: 'text/plain' })
            const a = document.createElement('a')
            a.href = URL.createObjectURL(blob)
            a.download = 'tailored-resume.txt'
            a.click()
          }}
        >
          Download
        </Button>
      </div>
      {result.changesSummary && (
        <p className="mb-4 rounded-lg bg-indigo-50 px-4 py-3 text-sm text-indigo-800">Changes: {result.changesSummary}</p>
      )}
      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-6">
          <h2 className="mb-2 text-sm font-semibold text-slate-500">Original resume</h2>
          <pre className="max-h-[520px] overflow-auto whitespace-pre-wrap font-mono text-xs text-slate-600">{result.originalContent}</pre>
        </Card>
        <Card className="p-6">
          <h2 className="mb-2 text-sm font-semibold text-emerald-700">Tailored for this job</h2>
          <pre className="max-h-[520px] overflow-auto whitespace-pre-wrap font-mono text-xs text-slate-700">{result.content}</pre>
        </Card>
      </div>
      <p className="mt-2 text-xs text-slate-400">Separator: {result.separator}</p>
    </div>
  )
}