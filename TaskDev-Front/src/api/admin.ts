import { apiClient } from './client'
import type { User, RegisterResponse } from '@/types'

export const adminApi = {
  getUsers: () =>
    apiClient.get<User[]>('/admin/users'),
  updateUserRole: (userId: string, role: string) =>
    apiClient.patch<User>(`/admin/users/${userId}/role`, { role }),
  toggleUserStatus: (userId: string) =>
    apiClient.patch<User>(`/admin/users/${userId}/status`),
  resetPassword: (userId: string) =>
    apiClient.post<{ newPassword: string; message: string }>(`/admin/users/${userId}/reset-password`),
  bootstrap: (data: { firstName: string; lastName: string }) =>
    apiClient.post<RegisterResponse>('/admin/bootstrap', data),
}
