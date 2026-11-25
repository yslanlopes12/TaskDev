import { apiClient } from './client'
import type { DashboardData, XpProgress } from '@/types'

export const dashboardApi = {
  get: (timezone?: string) =>
    apiClient.get<DashboardData>(`/dashboard${timezone ? `?timezone=${encodeURIComponent(timezone)}` : ''}`),
  progress: () =>
    apiClient.get<XpProgress>('/progress'),
}
