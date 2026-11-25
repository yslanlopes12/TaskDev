import { apiClient } from './client'
import type { Space, SpaceMember, CreateSpaceData, UpdateSpaceData } from '@/types'

export const spacesApi = {
  list: () => apiClient.get<Space[]>('/spaces'),
  get: (id: string) => apiClient.get<Space>(`/spaces/${id}`),
  create: (data: CreateSpaceData) => apiClient.post<Space>('/spaces', data),
  update: (id: string, data: UpdateSpaceData) => apiClient.put<Space>(`/spaces/${id}`, data),
  delete: (id: string) => apiClient.delete<void>(`/spaces/${id}`),
  getMembers: (id: string) => apiClient.get<SpaceMember[]>(`/spaces/${id}/members`),
  addMember: (id: string, username: string) => apiClient.post<void>(`/spaces/${id}/members`, { username }),
  removeMember: (id: string, userId: string) => apiClient.delete<void>(`/spaces/${id}/members/${userId}`),
}
