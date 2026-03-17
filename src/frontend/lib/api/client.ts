/**
 * Base API fetch helper.
 * - Server components use BACKEND_API_BASE_URL (server-only env var).
 * - Client components fall back to NEXT_PUBLIC_BACKEND_API_BASE_URL.
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

function getBaseUrl(): string {
  // Server-side: prefer the non-public env var for security
  if (typeof window === 'undefined') {
    return (
      process.env.BACKEND_API_BASE_URL ??
      process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL ??
      'http://localhost:5000'
    )
  }
  // Client-side: must use NEXT_PUBLIC_ prefixed var
  return process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL ?? 'http://localhost:5000'
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string,
): Promise<T> {
  const baseUrl = getBaseUrl()
  const headers = new Headers(options.headers)
  const isFormDataBody = options.body instanceof FormData

  if (!isFormDataBody && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const res = await fetch(`${baseUrl}/api/v1${path}`, {
    ...options,
    headers,
  })

  if (res.status === 401) {
    // Client-side: redirect to sign-in preserving current URL as return destination
    if (typeof window !== 'undefined') {
      const callbackUrl = encodeURIComponent(window.location.pathname + window.location.search)
      window.location.href = `/api/auth/signin?callbackUrl=${callbackUrl}`
      return new Promise<T>(() => {}) // suspend to prevent downstream error handling
    }
    throw new ApiError('UNAUTHORIZED', 401)
  }

  if (res.status === 204) {
    return undefined as T
  }

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const body = await res.json()
      message = body.message ?? body.error ?? body.title ?? message
    } catch {
      // ignore parse errors
    }
    throw new ApiError(message, res.status)
  }

  return res.json() as Promise<T>
}
