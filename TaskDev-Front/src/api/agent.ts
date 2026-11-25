import { apiClient } from './client'
import type {
  AgentConfig,
  AgentConfigData,
  AgentChatMessage,
  AgentChatResponse,
  SuggestedTask,
  TestAgentConnectionData,
} from '@/types'

export const agentApi = {
  getConfig: () => apiClient.get<AgentConfig>('/agent/config'),
  saveConfig: (data: AgentConfigData) => apiClient.put<void>('/agent/config', data),
  deleteConfig: () => apiClient.delete<void>('/agent/config'),
  test: (data?: TestAgentConnectionData) =>
    apiClient.post<{ success: boolean; message: string }>('/agent/test', data ?? {}),
  listModels: (data?: TestAgentConnectionData) =>
    apiClient.post<{ models: string[] }>('/agent/models', data ?? {}),
  preview: (prompt: string) =>
    apiClient.post<{ tasks: SuggestedTask[] }>('/agent/preview', { prompt }),
  chat: (messages: AgentChatMessage[], spaceId?: string) =>
    apiClient.post<AgentChatResponse>('/agent/chat', { messages, spaceId }),
}
