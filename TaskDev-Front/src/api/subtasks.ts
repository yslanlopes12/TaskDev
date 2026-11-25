import { apiClient } from './client'
import type { Subtask, ToggleSubtaskResponse } from '@/types'

export const subtasksApi = {
  list: (taskId: string) =>
    apiClient.get<Subtask[]>(`/tasks/${taskId}/subtasks`),
  create: (taskId: string, title: string) =>
    apiClient.post<Subtask>(`/tasks/${taskId}/subtasks`, { title }),
  update: (id: string, data: Partial<Subtask>) =>
    apiClient.patch<Subtask>(`/subtasks/${id}`, data),
  toggle: (id: string) =>
    apiClient.patch<ToggleSubtaskResponse>(`/subtasks/${id}/toggle`),
  delete: (id: string) =>
    apiClient.delete<void>(`/subtasks/${id}`),
}
