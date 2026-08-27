import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../lib/api'
import type { InterviewMode, InterviewSessionDetailDto, InterviewSessionDto, SubmitInterviewAnswerDto } from '../lib/types'
import { Badge, Button, Card, EmptyState, ErrorAlert, Field, PageHeader, Select, Spinner, Textarea } from '../components/ui'

const modes: InterviewMode[] = ['Mixed', 'Technical', 'Behavioral', 'Hr', 'RoleSpecific']

export function InterviewSessionPage() {
  const { id } = useParams()
  const [sessions, setSessions] = useState<InterviewSessionDto[] | null>(null)
  const [detail, setDetail] = useState<InterviewSessionDetailDto | null>(null)
  const [mode, setMode] = useState<InterviewMode>('Mixed')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [feedback, setFeedback] = useState<Record<string, SubmitInterviewAnswerDto>>({})

  useEffect(() => {
    if (!id) return
    api
      .get<InterviewSessionDto[]>(`/api/interviews?jobId=${id}`)
      .then((list) => {
        setSessions(list)
        const active = list.find((s) => !s.isCompleted) ?? list[list.length - 1]
        if (active) return loadDetail(active.id)
      })
      .catch(() => setSessions([]))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function loadDetail(sessionId: string) {
    const d = await api.get<InterviewSessionDetailDto>(`/api/interviews/${sessionId}`)
    setDetail(d)
    return d
  }

  async function startSession() {
    if (!id) return
    setBusy(true)
    setError('')
    try {
      const d = await api.post<InterviewSessionDetailDto>('/api/interviews', { jobId: id, mode })
      setDetail(d)
      setSessions((s) => (s ? [{ ...d.session }, ...s] : [d.session]))
      setAnswers({})
      setFeedback({})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start session')
    } finally {
      setBusy(false)
    }
  }

  async function openSession(sessionId: string) {
    setBusy(true)
    try {
      await loadDetail(sessionId)
      setAnswers({})
      setFeedback({})
    } finally {
      setBusy(false)
    }
  }

  async function submitAnswer(questionId: string) {
    const answer = answers[questionId]
    if (!answer?.trim()) return
    setBusy(true)
    setError('')
    try {
      const res = await api.post<SubmitInterviewAnswerDto>(`/api/interviews/questions/${questionId}/answer`, { answer })
      setFeedback((f) => ({ ...f, [questionId]: res }))
      if (res.sessionCompleted && detail) {
        const fresh = await api.get<InterviewSessionDetailDto>(`/api/interviews/${detail.session.id}`)
        setDetail(fresh)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to submit answer')
    } finally {
      setBusy(false)
    }
  }

  async function complete() {
    if (!detail) return
    setBusy(true)
    try {
      const d = await api.post<InterviewSessionDetailDto>(`/api/interviews/${detail.session.id}/complete`)
      setDetail(d)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to complete session')
    } finally {
      setBusy(false)
    }
  }

  const progress =
    detail && detail.session.questionCount > 0
      ? Math.round((detail.session.answeredCount / detail.session.questionCount) * 100)
      : 0

  return (
    <div>
      <PageHeader title="Interview Prep" subtitle="Practice questions, get scored, improve fast" />
      <ErrorAlert message={error} />

      {sessions === null ? (
        <Spinner label="Loading sessions…" />
      ) : (
        <div className="space-y-6">
          <Card className="p-6">
            <div className="flex flex-wrap items-end gap-4">
              <Field label="Mode">
                <Select value={mode} onChange={(e) => setMode(e.target.value as InterviewMode)} className="w-56">
                  {modes.map((m) => (
                    <option key={m}>{m}</option>
                  ))}
                </Select>
              </Field>
              <Button disabled={busy} onClick={() => void startSession()}>
                {busy ? 'Starting…' : 'Start new session'}
              </Button>
            </div>
            {sessions.length > 0 && (
              <div className="mt-4 flex flex-wrap gap-2">
                {sessions.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => void openSession(s.id)}
                    className={`rounded-lg border px-3 py-1.5 text-xs font-medium ${
                      detail?.session.id === s.id ? 'border-indigo-600 bg-indigo-50 text-indigo-700' : 'border-slate-300 text-slate-600 hover:bg-slate-50'
                    }`}
                  >
                    {s.mode} · {s.isCompleted ? s.overallScore ?? 'done' : `${s.answeredCount}/${s.questionCount}`}
                  </button>
                ))}
              </div>
            )}
          </Card>

          {detail ? (
            <div className="space-y-4">
              <Card className="p-6">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h2 className="text-lg font-semibold text-slate-800">
                      {detail.session.jobTitle} <span className="text-sm font-normal text-slate-400">· {detail.session.companyName}</span>
                    </h2>
                    <p className="mt-1 text-sm text-slate-500">
                      {detail.session.isCompleted ? `Completed with score ${detail.session.overallScore ?? '—'}` : `In progress · ${detail.session.answeredCount}/${detail.session.questionCount} answered`}
                    </p>
                  </div>
                  {!detail.session.isCompleted && (
                    <Button variant="secondary" onClick={() => void complete()}>
                      Complete session
                    </Button>
                  )}
                </div>
                <div className="mt-3 h-2 overflow-hidden rounded-full bg-slate-100">
                  <div className="h-full rounded-full bg-indigo-500 transition-all" style={{ width: `${progress}%` }} />
                </div>
                {detail.session.summary && (
                  <p className="mt-3 rounded-lg bg-indigo-50 px-4 py-3 text-sm text-indigo-800">{detail.session.summary}</p>
                )}
              </Card>

              {detail.questions.map((q) => (
                <Card key={q.id} className="p-6">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">Q{q.order} · {q.questionType}</p>
                      <p className="mt-1 font-medium text-slate-800">{q.question}</p>
                    </div>
                    {q.answerScore !== null && <Badge color={q.answerScore >= 70 ? 'emerald' : q.answerScore >= 40 ? 'amber' : 'rose'}>{q.answerScore}</Badge>}
                  </div>

                  {q.isAnswered ? (
                    <div className="mt-4 rounded-lg bg-slate-50 p-4">
                      {feedback[q.id] && <Scores fb={feedback[q.id]} />}
                      {feedback[q.id]?.feedback && <p className="mt-2 text-sm text-slate-600">{feedback[q.id].feedback}</p>}
                      {feedback[q.id]?.improvementSuggestion && (
                        <p className="mt-1 text-sm text-slate-500">
                          <span className="font-medium">Improve:</span> {feedback[q.id].improvementSuggestion}
                        </p>
                      )}
                    </div>
                  ) : (
                    <div className="mt-4 space-y-2">
                      <Textarea
                        rows={3}
                        placeholder="Type your answer…"
                        value={answers[q.id] ?? ''}
                        onChange={(e) => setAnswers((a) => ({ ...a, [q.id]: e.target.value }))}
                      />
                      <Button variant="secondary" disabled={busy || !answers[q.id]?.trim()} onClick={() => void submitAnswer(q.id)}>
                        Submit answer
                      </Button>
                    </div>
                  )}
                </Card>
              ))}
            </div>
          ) : (
            <EmptyState title="No session for this job yet" hint="Start a session above to practice and get scored." />
          )}
        </div>
      )}
    </div>
  )
}

function Scores({ fb }: { fb: SubmitInterviewAnswerDto }) {
  const rows = [
    ['Relevance', fb.relevanceScore],
    ['Clarity', fb.clarityScore],
    ['Technical depth', fb.technicalScore],
    ['Structure', fb.structureScore],
    ['Specificity', fb.specificityScore],
    ['Conciseness', fb.concisenessScore],
  ] as const
  return (
    <div className="grid grid-cols-3 gap-2 sm:grid-cols-6">
      {rows.map(([label, value]) => (
        <div key={label} className="rounded bg-white px-2 py-1.5 text-center">
          <p className="text-xs text-slate-400">{label}</p>
          <p className="text-base font-bold text-slate-800">{value}</p>
        </div>
      ))}
    </div>
  )
}