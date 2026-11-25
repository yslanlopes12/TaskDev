// Wrapper para fetch com CSRF automático, error handling e base URL configurável via .env
const BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '')

class ApiClient {
  private csrfToken: string | null = null

  public setCsrfToken(token: string | null) {
    this.csrfToken = token
  }

  private getCookie(name: string): string | null {
    if (typeof document === 'undefined') return null
    const value = `; ${document.cookie}`
    const parts = value.split(`; ${name}=`)
    if (parts.length === 2) {
      return decodeURIComponent(parts.pop()?.split(';').shift() || '') || null
    }
    return null
  }

  public async getCsrfToken(): Promise<string | null> {
    if (this.csrfToken) return this.csrfToken

    const cookieToken = this.getCookie('XSRF-TOKEN') || this.getCookie('CSRF-TOKEN')
    if (cookieToken) {
      this.csrfToken = cookieToken
      return cookieToken
    }

    try {
      const res = await fetch(`${BASE_URL}/auth/csrf`, {
        credentials: 'include',
      })
      if (res.ok) {
        const data = await res.json()
        if (data.csrfToken) {
          this.csrfToken = data.csrfToken
          return data.csrfToken
        }
      }
    } catch {
      // Falha silenciosa no fetch do CSRF; tenta prosseguir
    }

    return null
  }

  private async getHeaders(isWrite = false): Promise<HeadersInit> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    }

    if (isWrite) {
      const token = await this.getCsrfToken()
      if (token) {
        headers['X-CSRF-TOKEN'] = token
        headers['X-XSRF-TOKEN'] = token
      }
    }

    return headers
  }

  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      credentials: 'include',
      headers: await this.getHeaders(),
    })
    if (!res.ok) await this.throwError(res)
    return res.json()
  }

  async post<T>(path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: await this.getHeaders(true),
      body: body ? JSON.stringify(body) : undefined,
    })
    if (!res.ok) await this.throwError(res)
    return res.json()
  }

  async put<T>(path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'PUT',
      credentials: 'include',
      headers: await this.getHeaders(true),
      body: body ? JSON.stringify(body) : undefined,
    })
    if (!res.ok) await this.throwError(res)
    return res.json()
  }

  async patch<T>(path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'PATCH',
      credentials: 'include',
      headers: await this.getHeaders(true),
      body: body ? JSON.stringify(body) : undefined,
    })
    if (!res.ok) await this.throwError(res)
    return res.json()
  }

  async delete<T>(path: string): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'DELETE',
      credentials: 'include',
      headers: await this.getHeaders(true),
    })
    if (!res.ok) await this.throwError(res)
    if (res.status === 204) return undefined as T
    return res.json()
  }

  private async throwError(res: Response): Promise<never> {
    let message = `Erro ${res.status}`
    try {
      const data = await res.json()
      message = data.message || message
    } catch {}
    throw { status: res.status, message }
  }
}

export const apiClient = new ApiClient()
