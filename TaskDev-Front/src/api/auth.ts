import { apiClient } from './client'
import type { User, RegisterResponse } from '@/types'

export const authApi = {
  register: (data: { firstName: string; lastName: string; timezone?: string; password?: string }) =>
    apiClient.post<RegisterResponse>('/auth/register', data),
  login: (username: string, password: string) =>
    apiClient.post<{ message: string; csrfToken?: string }>('/auth/login', { username, password }),
  csrf: () => apiClient.get<{ csrfToken: string }>('/auth/csrf'),
  logout: () => apiClient.post<void>('/auth/logout'),
  me: () => apiClient.get<User>('/auth/me'),
}
