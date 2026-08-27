const BASE = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/+$/, '')

export interface ApiEnvelope<T> {
  success: boolean
  data: T
}

export interface ApiError {
  success: false
  message: string
  errorCode: string
  errors?: Record<string, string[]>
}

export class ApiErrorResponse extends Error {
  readonly status: number
  readonly error: ApiError

  constructor(status: number, error: ApiError) {
    super(error.message || `Request failed (${status})`)
    this.status = status
    this.error = error
  }
}

const TOKEN_KEY = 'career_copilot_token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t: string) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const token = tokenStore.get()
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${BASE}${path}`, { ...init, headers })

  if (response.status === 401 && token) {
    tokenStore.clear()
    if (!path.startsWith('/api/auth/login')) {
      window.location.href = '/login'
    }
  }

  const text = await response.text()
  const json = text ? safeParse(text) : null

  if (!response.ok) {
    const err: ApiError = json
      ? (json as ApiError)
      : { success: false, message: text || response.statusText, errorCode: 'UNKNOWN' }
    throw new ApiErrorResponse(response.status, err)
  }

  const envelope = json as ApiEnvelope<T> | undefined
  if (envelope && typeof envelope.success === 'boolean') {
    return envelope.data
  }
  return json as T
}

function safeParse(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return null
  }
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  getText: async (path: string) => {
    const token = tokenStore.get()
    const headers = new Headers()
    if (token) headers.set('Authorization', `Bearer ${token}`)
    const response = await fetch(`${BASE}${path}`, { headers })
    if (!response.ok) {
      throw new ApiErrorResponse(response.status, { success: false, message: response.statusText, errorCode: 'HTTP' })
    }
    return response.text()
  },
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: 'POST',
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: 'PUT',
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
  upload: <T>(path: string, file: File, setDefault: boolean) => {
    const form = new FormData()
    form.append('file', file)
    form.append('setDefault', setDefault ? 'true' : 'false')
    return request<T>(path, { method: 'POST', body: form })
  },
}