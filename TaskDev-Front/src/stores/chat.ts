import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import { agentApi } from '@/api/agent'
import type { AgentConfig, AgentChatMessage, AgentChatPoll, AgentTaskAction, SuggestedTask } from '@/types'
import { useAuthStore } from './auth'

export interface ChatTimelineItem {
  id: string
  sender: 'user' | 'assistant'
  text: string
  actionType?: AgentTaskAction
  suggestedTask?: SuggestedTask
  needsSpace?: boolean
  selectedSpaceId?: string
  approved?: boolean
  dismissed?: boolean
  poll?: AgentChatPoll
  pollAnswered?: boolean
  pollSelectedOptions?: string[]
  excludeFromContext?: boolean
}

const WELCOME_MESSAGE: ChatTimelineItem = {
  id: 'welcome',
  sender: 'assistant',
  text: 'Olá! Podemos conversar sobre o seu projeto ou você pode me pedir para criar, editar ou excluir tarefas.',
}

const SAVED_MESSAGE_LIMIT = 250
const CONTEXT_MESSAGE_LIMIT = 60

function readHistory(key: string): ChatTimelineItem[] {
  try {
    const saved: unknown = JSON.parse(localStorage.getItem(key) ?? '[]')
    if (Array.isArray(saved)) {
      return saved.filter((item): item is ChatTimelineItem =>
        item != null && typeof item.id === 'string' && item.id !== WELCOME_MESSAGE.id &&
        (item.sender === 'user' || item.sender === 'assistant') && typeof item.text === 'string',
      ).slice(-SAVED_MESSAGE_LIMIT)
    }
  } catch {
    // O chat continua disponível se o armazenamento do navegador estiver indisponível.
  }
  return []
}

function toAgentMessage(item: ChatTimelineItem): AgentChatMessage {
  const { text, sender, suggestedTask, actionType, approved, dismissed } = item
  if (!suggestedTask || !actionType || (!approved && !dismissed)) return { role: sender, content: text }

  const action = { CREATE_TASK: 'criação', UPDATE_TASK: 'edição', DELETE_TASK: 'exclusão' }[actionType]
  const outcome = approved ? 'confirmada e concluída' : 'cancelada'
  return {
    role: sender,
    content: `${text}\nNo painel, a ${action} da tarefa “${suggestedTask.title}” foi ${outcome}.`,
  }
}

export const useChatStore = defineStore('chat', () => {
  const authStore = useAuthStore()
  const messages = ref<ChatTimelineItem[]>([{ ...WELCOME_MESSAGE }])
  const config = ref<AgentConfig | null>(null)
  const configLoaded = ref(false)
  const isSending = ref(false)
  let storageKey: string | undefined
  let accountVersion = 0

  watch(() => authStore.user?.id, (userId) => {
    accountVersion++
    storageKey = userId ? `taskdev:chat:v1:${userId}` : undefined
    messages.value = [{ ...WELCOME_MESSAGE }, ...(storageKey ? readHistory(storageKey) : [])]
    config.value = null
    configLoaded.value = false
    isSending.value = false
  }, { immediate: true, flush: 'sync' })

  watch(messages, (timeline) => {
    if (!storageKey) return

    try {
      const saved = timeline.slice(-SAVED_MESSAGE_LIMIT).filter((item) => item.id !== WELCOME_MESSAGE.id)
      if (saved.length > 0) {
        localStorage.setItem(storageKey, JSON.stringify(saved))
      } else {
        localStorage.removeItem(storageKey)
      }
    } catch {
      // A conversa permanece em memória se o navegador impedir a gravação.
    }
  }, { deep: true, flush: 'sync' })

  async function loadConfig() {
    if (!authStore.user) return
    const version = accountVersion
    try {
      const res = await agentApi.getConfig()
      if (version === accountVersion) config.value = res && res.name ? res : null
    } catch {
      if (version === accountVersion) config.value = null
    } finally {
      if (version === accountVersion) configLoaded.value = true
    }
  }

  function clearMessages() {
    messages.value = [{ ...WELCOME_MESSAGE }]
    isSending.value = false
  }

  async function sendMessage(text: string, currentSpaceId?: string): Promise<void> {
    const trimmed = text.trim()
    if (!trimmed || isSending.value || !authStore.user) return
    const timeline = messages.value

    timeline.push({
      id: Math.random().toString(36).substring(2, 10),
      sender: 'user',
      text: trimmed,
      excludeFromContext: true,
    })

    const outgoing = timeline[timeline.length - 1]!
    isSending.value = true
    try {
      const history = timeline.slice(1)
        .filter((item) => !item.excludeFromContext || item === outgoing)
        .slice(-CONTEXT_MESSAGE_LIMIT)
        .map(toAgentMessage)

      const res = await agentApi.chat(history, currentSpaceId)
      if (timeline !== messages.value) return
      outgoing.excludeFromContext = false

      timeline.push({
        id: Math.random().toString(36).substring(2, 10),
        sender: 'assistant',
        text: res.reply,
        actionType: res.actionType || (res.suggestedTask && !res.suggestedTask.taskId ? 'CREATE_TASK' : undefined),
        suggestedTask: res.suggestedTask,
        needsSpace: res.needsSpace,
        selectedSpaceId: res.suggestedTask?.spaceId || currentSpaceId || '',
        poll: res.poll,
        pollAnswered: false,
        pollSelectedOptions: [],
      })
    } catch (err: any) {
      if (timeline !== messages.value) return
      timeline.push({
        id: Math.random().toString(36).substring(2, 10),
        sender: 'assistant',
        text:
          err?.message ||
          'Não foi possível se comunicar com o assistente. Verifique sua chave de API nas Configurações.',
        excludeFromContext: true,
      })
    } finally {
      if (timeline === messages.value) isSending.value = false
    }
  }

  return {
    messages,
    config,
    configLoaded,
    isSending,
    loadConfig,
    clearMessages,
    sendMessage,
  }
})
