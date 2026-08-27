import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import type { ResumeAnalysisDto, ResumeDto } from '../lib/types'
import { Button, Card, EmptyState, ErrorAlert, PageHeader, Spinner } from '../components/ui'

export function ResumesPage() {
  const [items, setItems] = useState<ResumeDto[] | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(() => {
    api
      .get<{ items: ResumeDto[] }>('/api/resumes')
      .then((r) => setItems(r.items))
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load resumes'))
  }, [])

  useEffect(load, [load])

  async function onUpload(file: File, setDefault: boolean) {
    setBusy(true)
    setError('')
    try {
      await api.upload<ResumeDto>('/api/resumes', file, setDefault)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed')
    } finally {
      setBusy(false)
    }
  }

  async function onAnalyze(id: string) {
    setError('')
    try {
      const res = await api.post<ResumeAnalysisDto>(`/api/resumes/${id}/analyze`)
      alert(`Resume score: ${res.score}\n${res.summary}`)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Analysis failed')
    }
  }

  async function onDelete(id: string) {
    if (!confirm('Delete this resume?')) return
    try {
      await api.del(`/api/resumes/${id}`)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Delete failed')
    }
  }

  return (
    <div>
      <PageHeader title="Resumes" subtitle="Upload, analyze and keep your best version ready" actions={<UploadButton onUpload={onUpload} busy={busy} />} />
      <ErrorAlert message={error} />
      {items === null ? (
        <Spinner />
      ) : items.length === 0 ? (
        <EmptyState title="No resumes yet" hint="Upload a PDF, DOCX or TXT to get an instant ATS analysis." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((r) => (
            <Card key={r.id} className="p-5">
              <div className="flex items-start justify-between">
                <div className="min-w-0">
                  <Link to={`/resumes/${r.id}`} className="font-semibold text-slate-800 hover:text-indigo-600">
                    {r.originalFileName}
                  </Link>
                  <p className="mt-0.5 text-xs text-slate-400">
                    {new Date(r.uploadedAt).toLocaleDateString()} · {r.fileType}
                  </p>
                </div>
                {r.isDefault && (
                  <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">Default</span>
                )}
              </div>
              <div className="mt-3 flex items-center justify-between">
                <span className="text-sm text-slate-500">
                  {r.resumeScore !== null ? `Score ${r.resumeScore}/100` : 'Not analyzed'}
                </span>
                {r.parseFailed && <span className="text-xs text-rose-500">Parsing failed</span>}
              </div>
              <div className="mt-4 flex items-center gap-2">
                <Button variant="secondary" onClick={() => onAnalyze(r.id)}>Analyze</Button>
                {!r.isDefault && (
                  <Button
                    variant="ghost"
                    onClick={() => {
                      void api.post<ResumeDto>(`/api/resumes/${r.id}/set-default`).then(load).catch((e) => setError(e instanceof Error ? e.message : 'Failed'))
                    }}
                  >
                    Set default
                  </Button>
                )}
                <Button variant="ghost" className="ml-auto text-rose-600 hover:bg-rose-50" onClick={() => onDelete(r.id)}>
                  Delete
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

function UploadButton({ onUpload, busy }: { onUpload: (file: File, setDefault: boolean) => void; busy: boolean }) {
  const inputRef = useRef<HTMLInputElement>(null)
  return (
    <>
      <input
        ref={inputRef}
        type="file"
        accept=".pdf,.docx,.txt"
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0]
          if (file) {
            const useDefault = confirm('Set as default resume?')
            void onUpload(file, useDefault)
          }
          e.target.value = ''
        }}
      />
      <Button disabled={busy} onClick={() => inputRef.current?.click()}>
        {busy ? 'Uploading…' : 'Upload resume'}
      </Button>
    </>
  )
}