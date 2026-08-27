import { useEffect, useRef, useState } from 'react'
import { api } from '../lib/api'
import type { CopilotConversationDto, CopilotMessageDto } from '../lib/types'
import { Button, Card, PageHeader, Spinner, Textarea } from '../components/ui'

export function CopilotPage() {
  const [conversations, setConversations] = useState<CopilotConversationDto[] | null>(null)
  const [active, setActive] = useState<CopilotConversationDto | null>(null)
  const [messages, setMessages] = useState<CopilotMessageDto[]>([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const endRef = useRef<HTMLDivElement>(null)

  const loadConvs = () => {
    api
      .get<CopilotConversationDto[]>('/api/copilot/conversations')
      .then(setConversations)
      .catch(() => setConversations([]))
  }

  useEffect(loadConvs, [])

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function openConversation(c: CopilotConversationDto) {
    setActive(c)
    setBusy(true)
    try {
      const ms = await api.get<CopilotMessageDto[]>(`/api/copilot/conversations/${c.id}`)
      setMessages(ms)
    } catch {
      setMessages([])
    } finally {
      setBusy(false)
    }
  }

  async function startNew() {
    setActive(null)
    setMessages([])
    setInput('')
  }

  async function send() {
    const text = input.trim()
    if (!text || busy) return
    const convoId = active?.id
    setMessages((m) => [...m, { id: `tmp-${m.length}`, role: 'User', content: text, createdAt: new Date().toISOString() }])
    setInput('')
    setBusy(true)
    try {
      const reply = await api.post<{ conversationId: string; message: CopilotMessageDto }>('/api/copilot/chat', {
        message: text,
        conversationId: convoId ?? null,
        jobId: null,
      })
      setMessages((m) => [...m, reply.message])
      if (!convoId) {
        const fresh = await api.get<CopilotConversationDto[]>('/api/copilot/conversations')
        setConversations(fresh)
        const c = fresh.find((x) => x.id === reply.conversationId)
        if (c) setActive(c)
      }
    } catch (e) {
      setMessages((m) => [
        ...m,
        { id: `err-${m.length}`, role: 'Copilot', content: e instanceof Error ? e.message : 'Something went wrong', createdAt: new Date().toISOString() },
      ])
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <PageHeader title="Career Copilot" subtitle="Ask anything about your job search" />
      <div className="grid gap-6 lg:grid-cols-4">
        <Card className="p-4 lg:col-span-1">
          <Button variant="secondary" className="mb-3 w-full" onClick={startNew}>
            + New conversation
          </Button>
          {conversations === null ? (
            <Spinner />
          ) : (
            <ul className="space-y-1">
              {conversations.map((c) => (
                <li key={c.id}>
                  <button
                    onClick={() => void openConversation(c)}
                    className={`w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                      active?.id === c.id ? 'bg-indigo-50 text-indigo-700' : 'text-slate-600 hover:bg-slate-50'
                    }`}
                  >
                    <span className="block truncate font-medium">{c.title}</span>
                    <span className="block text-xs text-slate-400">
                      {c.messageCount} messages · {new Date(c.lastActivityAt).toLocaleDateString()}
                    </span>
                  </button>
                </li>
              ))}
              {conversations.length === 0 && <li className="px-3 py-2 text-sm text-slate-400">No conversations yet</li>}
            </ul>
          )}
        </Card>

        <Card className="flex h-[560px] flex-col lg:col-span-3">
          <div className="flex-1 space-y-4 overflow-y-auto p-4">
            {messages.length === 0 && (
              <p className="pt-20 text-center text-sm text-slate-400">
                Ask about salary negotiation, resume wording, interview questions, or how to bridge a skill gap.
              </p>
            )}
            {messages.map((m) => (
              <div key={m.id} className={`flex ${m.role === 'User' ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[80%] rounded-xl px-4 py-2.5 text-sm ${
                    m.role === 'User' ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-700'
                  }`}
                >
                  <p className="whitespace-pre-wrap">{m.content}</p>
                  <p className={`mt-1 text-[10px] ${m.role === 'User' ? 'text-indigo-200' : 'text-slate-400'}`}>
                    {new Date(m.createdAt).toLocaleTimeString()}
                  </p>
                </div>
              </div>
            ))}
            {busy && (
              <div className="flex justify-start">
                <div className="rounded-xl bg-slate-100 px-4 py-2.5 text-sm text-slate-400">Copilot is thinking…</div>
              </div>
            )}
            <div ref={endRef} />
          </div>
          <div className="border-t border-slate-100 p-4">
            <div className="flex gap-2">
              <Textarea
                rows={2}
                placeholder="Ask Copilot…"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault()
                    void send()
                  }
                }}
              />
              <Button onClick={() => void send()} disabled={busy || !input.trim()}>
                Send
              </Button>
            </div>
          </div>
        </Card>
      </div>
    </div>
  )
}