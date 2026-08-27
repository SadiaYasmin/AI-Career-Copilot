import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { api, tokenStore } from './api'
import type { AuthResponse, UserDto } from './types'

interface AuthState {
  user: UserDto | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, fullName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!tokenStore.get()) {
      setLoading(false)
      return
    }
    api
      .get<AuthResponse>('/api/auth/me')
      .then((res) => setUser(res.user))
      .catch(() => {
        tokenStore.clear()
        setUser(null)
      })
      .finally(() => setLoading(false))
  }, [])

  async function persist(response: AuthResponse) {
    tokenStore.set(response.token)
    setUser(response.user)
  }

  async function login(email: string, password: string) {
    const res = await api.post<AuthResponse>('/api/auth/login', { email, password })
    await persist(res)
  }

  async function register(email: string, password: string, fullName: string) {
    const res = await api.post<AuthResponse>('/api/auth/register', { email, password, fullName })
    await persist(res)
  }

  function logout() {
    tokenStore.clear()
    setUser(null)
    void api.post('/api/auth/logout').catch(() => undefined)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}