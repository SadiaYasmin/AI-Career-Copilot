import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { ResumeAnalysisDto, ResumeDto } from '../lib/types'
import { Badge, Button, Card, PageHeader, Spinner } from '../components/ui'

export function ResumeDetailPage() {
  const { id } = useParams()
  const [resume, setResume] = useState<ResumeDto | null>(null)
  const [content, setContent] = useState<string | null>(null)
  const [analysis, setAnalysis] = useState<ResumeAnalysisDto | null>(null)

  useEffect(() => {
    if (!id) return
    void api
      .get<ResumeDto>(`/api/resumes/${id}`)
      .then(setResume)
      .catch(() => undefined)
    void api
      .getText(`/api/resumes/${id}/content`)
      .then(setContent)
      .catch(() => undefined)
    void api
      .post<ResumeAnalysisDto>(`/api/resumes/${id}/analyze`)
      .then(setAnalysis)
      .catch(() => undefined)
  }, [id])

  if (!resume) return <Spinner label="Loading resume…" />

  const score = analysis?.score ?? resume.resumeScore

  return (
    <div>
      <PageHeader
        title={resume.originalFileName}
        subtitle={resume.isDefault ? 'Default resume' : 'Not the default resume'}
        actions={
          <>
            <Link to="/resumes">
              <Button variant="secondary">Back</Button>
            </Link>
          </>
        }
      />

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-6">
          <h2 className="mb-4 text-sm font-semibold text-slate-700">Analysis</h2>
          {analysis ? (
            <>
              <div className="mb-4 flex items-center gap-4">
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-indigo-100 text-2xl font-bold text-indigo-700">
                  {score}
                </div>
                <div>
                  <p className="text-sm text-slate-600">{analysis.summary}</p>
                  {analysis.usedAi ? (
                    <Badge color="sky">AI analysis</Badge>
                  ) : (
                    <Badge color="slate">Local analysis</Badge>
                  )}
                </div>
              </div>

              {analysis.strengths.length > 0 && (
                <Section title="Strengths" tone="emerald">
                  {analysis.strengths.map((s) => (
                    <li key={s}>{s}</li>
                  ))}
                </Section>
              )}
              {analysis.improvements.length > 0 && (
                <Section title="Improvements" tone="amber">
                  {analysis.improvements.map((s) => (
                    <li key={s}>{s}</li>
                  ))}
                </Section>
              )}
              {analysis.atRiskFindings.length > 0 && (
                <Section title="Watch out" tone="rose">
                  {analysis.atRiskFindings.map((s) => (
                    <li key={s}>{s}</li>
                  ))}
                </Section>
              )}
            </>
          ) : (
            <p className="text-sm text-slate-400">Analysis not available yet.</p>
          )}
        </Card>

        <Card className="p-6">
          <h2 className="mb-4 text-sm font-semibold text-slate-700">Extracted content</h2>
          <pre className="max-h-[560px] overflow-auto whitespace-pre-wrap rounded-lg bg-slate-50 p-4 font-mono text-xs text-slate-600">
            {content || 'No text extracted.'}
          </pre>
        </Card>
      </div>
    </div>
  )
}

function Section({ title, tone, children }: { title: string; tone: 'emerald' | 'amber' | 'rose'; children: ReactNode }) {
  const colors = { emerald: 'text-emerald-700', amber: 'text-amber-700', rose: 'text-rose-700' }
  return (
    <div className="mt-4">
      <h3 className={`text-xs font-semibold uppercase tracking-wide ${colors[tone]}`}>{title}</h3>
      <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-slate-600">{children}</ul>
    </div>
  )
}