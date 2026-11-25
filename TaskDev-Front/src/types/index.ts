export type UserRole = 'ADMIN' | 'OPERATOR' | 'MEMBER'
export type TaskStatus = 'PENDING' | 'IN_PROGRESS' | 'DONE'
export type TaskPriority = 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT'
export type SpaceVisibility = 'PRIVATE' | 'SHARED'
export type AgentProviderType = 'OPENAI_COMPATIBLE' | 'ANTHROPIC' | 'GEMINI' | 'WEBHOOK'
export type ViewMode = 'list' | 'board'
export type ThemeMode = 'light' | 'dark' | 'system'

export interface User {
  id: string
  firstName: string
  lastName: string
  username: string
  role: UserRole
  isActive: boolean
  timezone?: string
  createdAt: string
}

export interface Space {
  id: string
  ownerUserId: string
  name: string
  icon?: string
  color?: string
  visibility: SpaceVisibility
  createdAt: string
  updatedAt: string
  memberCount?: number
  pendingTasksCount?: number
}

export interface SpaceMember {
  userId: string
  username: string
  firstName: string
  lastName: string
  joinedAt: string
}

export interface Subtask {
  id: string
  taskId: string
  title: string
  isCompleted: boolean
  position: number
  createdAt: string
}

export interface ToggleSubtaskResponse extends Subtask {
  pointsAwarded?: number
  pointsDeducted?: number
}

export interface TaskItem {
  id: string
  spaceId: string
  createdByUserId: string
  assigneeUserId?: string
  assigneeUsername?: string
  title: string
  description?: string
  status: TaskStatus
  priority: TaskPriority
  dueDate?: string
  position: number
  xpAwarded: boolean
  completedAt?: string
  createdAt: string
  updatedAt: string
  subtasks?: Subtask[]
  space?: { name: string; color?: string; icon?: string }
}

export interface XpProgress {
  totalXp: number
  level: number
  rank: string
  xpInCurrentLevel: number
  xpForNextLevel: number
  xpToNextLevel: number
}

export interface AgentConfig {
  name: string
  endpointUrl: string
  providerType: AgentProviderType
  model?: string
  isEnabled: boolean
  hasApiKey: boolean
}

export interface AgentConfigData {
  name: string
  endpointUrl: string
  providerType: AgentProviderType
  model?: string
  apiKey?: string
  isEnabled: boolean
}

export interface SuggestedTask {
  taskId?: string
  spaceId?: string
  title: string
  description?: string
  status?: TaskStatus
  priority?: TaskPriority
  dueDate?: string
  subtasks?: string[]
  changedFields?: Array<'title' | 'description' | 'status' | 'priority' | 'dueDate'>
}

export type AgentTaskAction = 'CREATE_TASK' | 'UPDATE_TASK' | 'DELETE_TASK'

export interface TestAgentConnectionData {
  endpointUrl?: string
  apiKey?: string
  providerType?: AgentProviderType
  model?: string
}

export interface AgentChatMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface AgentChatPoll {
  question: string
  multiSelect: boolean
  options: string[]
  allowCustomOption?: boolean
}

export interface AgentChatResponse {
  reply: string
  isAgentAction: boolean
  actionType?: AgentTaskAction
  needsSpace: boolean
  suggestedTask?: SuggestedTask
  poll?: AgentChatPoll
}

export interface DashboardData {
  todayCount: number
  pendingCount: number
  plannedCount: number
  doneCount: number
  todayTasks: TaskItem[]
  pendingTasks: TaskItem[]
  plannedTasks: TaskItem[]
  doneTasks: TaskItem[]
}

export interface CreateSpaceData {
  name: string
  icon?: string
  color?: string
  visibility: SpaceVisibility
}

export interface UpdateSpaceData {
  name?: string
  icon?: string
  color?: string
  visibility?: SpaceVisibility
}

export interface CreateTaskData {
  spaceId: string
  title: string
  description?: string
  status?: TaskStatus
  priority?: TaskPriority
  dueDate?: string
  assigneeUserId?: string
  subtasks?: string[]
}

export interface UpdateTaskData {
  spaceId?: string
  title?: string
  description?: string
  status?: TaskStatus
  priority?: TaskPriority
  dueDate?: string
  assigneeUserId?: string
  completedAt?: string
  position?: number
}

export interface ApiError {
  message: string
  errors?: Record<string, string[]>
}

export interface RegisterResponse {
  username: string
  password: string
  message: string
}

export interface ToastMessage {
  id: string
  message: string
  type: 'success' | 'error' | 'info'
  duration?: number
}
