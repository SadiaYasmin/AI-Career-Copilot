import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import { Button, Card, ErrorAlert, Field, Input } from '../components/ui'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await login(email, password)
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to sign in')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthShell title="Welcome back" subtitle="Sign in to Career Copilot">
      <form onSubmit={onSubmit} className="space-y-4">
        <Field label="Email">
          <Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
        </Field>
        <Field label="Password">
          <Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </Field>
        <ErrorAlert message={error} />
        <Button type="submit" disabled={busy} className="w-full">
          {busy ? 'Signing in…' : 'Sign in'}
        </Button>
      </form>
      <p className="mt-5 text-center text-sm text-slate-500">
        New here?{' '}
        <Link to="/register" className="font-medium text-indigo-600 hover:text-indigo-700">
          Create an account
        </Link>
      </p>
    </AuthShell>
  )
}

export function AuthShell({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center justify-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-white">
            <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M11.48 3.499a.562.562 0 011.04 0l2.125 5.111a.563.563 0 00.475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 00-.182.557l1.285 5.385a.562.562 0 01-.84.61l-4.725-2.885a.563.563 0 00-.586 0L6.982 20.54a.562.562 0 01-.84-.61l1.285-5.386a.562.562 0 00-.182-.557l-4.204-3.602a.562.562 0 01.321-.988l5.518-.442a.563.563 0 00.475-.345L11.48 3.5z" />
            </svg>
          </div>
        </div>
        <Card className="p-8">
          <h1 className="text-2xl font-bold text-slate-900">{title}</h1>
          <p className="mb-6 mt-1 text-sm text-slate-500">{subtitle}</p>
          {children}
        </Card>
      </div>
    </div>
  )
}