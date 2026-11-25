import { apiClient } from './client'
import type { TaskItem, TaskStatus, CreateTaskData, UpdateTaskData } from '@/types'

export const tasksApi = {
  list: (spaceId?: string) =>
    apiClient.get<TaskItem[]>(`/tasks${spaceId ? `?spaceId=${spaceId}` : ''}`),
  get: (id: string) =>
    apiClient.get<TaskItem>(`/tasks/${id}`),
  create: (data: CreateTaskData) =>
    apiClient.post<TaskItem>('/tasks', data),
  update: (id: string, data: UpdateTaskData) =>
    apiClient.put<TaskItem>(`/tasks/${id}`, data),
  delete: (id: string) =>
    apiClient.delete<void>(`/tasks/${id}`),
  updateStatus: (id: string, status: TaskStatus) =>
    apiClient.patch<TaskItem>(`/tasks/${id}/status`, { status }),
  move: (id: string, data: { status: TaskStatus; position: number }) =>
    apiClient.patch<TaskItem>(`/tasks/${id}/move`, data),
  complete: (id: string) =>
    apiClient.patch<{ xpAwarded: boolean; points: number }>(`/tasks/${id}/complete`),
  uncomplete: (id: string) =>
    apiClient.patch<{ pointsDeducted: number }>(`/tasks/${id}/uncomplete`),
}
