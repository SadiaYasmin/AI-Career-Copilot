import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import { Button, ErrorAlert, Field, Input } from '../components/ui'
import { AuthShell } from './Login'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (password.length < 8) {
      setError('Password must be at least 8 characters.')
      return
    }
    if (password !== confirm) {
      setError('Passwords do not match.')
      return
    }
    setBusy(true)
    try {
      await register(email, password, fullName)
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to create account')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthShell title="Create your account" subtitle="Start preparing for your next role">
      <form onSubmit={onSubmit} className="space-y-4">
        <Field label="Full name">
          <Input type="text" required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" />
        </Field>
        <Field label="Email">
          <Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
        </Field>
        <Field label="Password">
          <Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
        </Field>
        <Field label="Confirm password">
          <Input type="password" required value={confirm} onChange={(e) => setConfirm(e.target.value)} autoComplete="new-password" />
        </Field>
        <ErrorAlert message={error} />
        <Button type="submit" disabled={busy} className="w-full">
          {busy ? 'Creating account…' : 'Create account'}
        </Button>
      </form>
      <p className="mt-5 text-center text-sm text-slate-500">
        Already have an account?{' '}
        <Link to="/login" className="font-medium text-indigo-600 hover:text-indigo-700">
          Sign in
        </Link>
      </p>
    </AuthShell>
  )
}