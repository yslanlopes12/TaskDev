<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { useUiStore } from '@/stores/ui'
import { useTasksStore } from '@/stores/tasks'
import { useSpacesStore } from '@/stores/spaces'
import { useChatStore, type ChatTimelineItem } from '@/stores/chat'
import { useToast } from '@/composables/useToast'
import { tasksApi } from '@/api/tasks'
import type { AgentTaskAction } from '@/types'
import {
  X,
  Send,
  Bot,
  Sparkles,
  CheckCircle,
  AlertCircle,
  Settings,
  Trash2,
  Calendar,
  Folder,
} from 'lucide-vue-next'
import AppButton from '@/components/ui/AppButton.vue'
import PriorityDot from '@/components/ui/PriorityDot.vue'

const route = useRoute()
const router = useRouter()
const uiStore = useUiStore()
const tasksStore = useTasksStore()
const spacesStore = useSpacesStore()
const chatStore = useChatStore()
const { success, error: toastError } = useToast()

const inputMessage = ref('')
const applyingTaskId = ref<string | null>(null)
const messagesContainer = ref<HTMLElement | null>(null)
const customPollOpenId = ref<string | null>(null)
const customPollValue = ref('')

const taskActions = {
  CREATE_TASK: { button: 'Criar tarefa', success: 'Tarefa criada.' },
  UPDATE_TASK: { button: 'Salvar alterações', success: 'Tarefa atualizada.' },
  DELETE_TASK: { button: 'Excluir tarefa', success: 'Tarefa excluída.' },
} satisfies Record<AgentTaskAction, { button: string; success: string }>

const currentSpaceId = computed(() => {
  if (route.path.startsWith('/spaces/') && route.params.id) {
    return route.params.id as string
  }
  return undefined
})

const isAgentReady = computed(() => Boolean(chatStore.config && chatStore.config.isEnabled))

onMounted(async () => {
  await chatStore.loadConfig()
})

watch(
  () => uiStore.chatOpen,
  async (open) => {
    if (open) {
      await chatStore.loadConfig()
      scrollToBottom()
    }
  }
)

watch(
  () => chatStore.messages.length,
  () => {
    scrollToBottom()
  }
)

async function scrollToBottom() {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

async function handleSendMessage() {
  const text = inputMessage.value.trim()
  if (!text || chatStore.isSending || applyingTaskId.value) return
  inputMessage.value = ''

  const latest = chatStore.messages.at(-1)
  const answer = text.toLowerCase().replace(/[.!]+$/, '')
  if ((latest?.actionType === 'CREATE_TASK' || latest?.actionType === 'DELETE_TASK') &&
      latest.suggestedTask && !latest.approved && !latest.dismissed) {
    if (answer === 'sim') {
      await confirmTaskAction(latest)
      return
    }
    if (latest.actionType === 'CREATE_TASK') {
      const title = text.match(/^(?:(?:novo )?t[ií]tulo)\s*:\s*(.+)$/i)?.[1]?.trim()
      if (title) {
        if (title.length > 200) {
          toastError('O título deve ter até 200 caracteres.')
        } else {
          latest.suggestedTask.title = title
        }
        return
      }
    }
    if (answer === 'não' || answer === 'nao') {
      latest.dismissed = true
      return
    }
  }

  await chatStore.sendMessage(text, currentSpaceId.value)
}

async function confirmTaskAction(msg: ChatTimelineItem) {
  const { actionType, suggestedTask } = msg
  if (!actionType || !suggestedTask || msg.approved || msg.dismissed || applyingTaskId.value || chatStore.isSending) return

  applyingTaskId.value = msg.id
  try {
    if (actionType === 'CREATE_TASK') {
      const targetSpaceId =
        msg.selectedSpaceId ||
        suggestedTask.spaceId ||
        (spacesStore.spaces.length === 1 ? spacesStore.spaces[0]?.id : undefined)

      if (!targetSpaceId) {
        toastError('Selecione ou crie um espaço antes de adicionar a tarefa.')
        return
      }

      const title = suggestedTask.title.trim()
      if (title.length === 0 || title.length > 200) {
        toastError('Informe um título de 1 a 200 caracteres.')
        return
      }

      await tasksStore.createTask({
        spaceId: targetSpaceId,
        title,
        description: suggestedTask.description,
        priority: suggestedTask.priority || 'MEDIUM',
        dueDate: suggestedTask.dueDate,
        status: 'PENDING',
        subtasks: suggestedTask.subtasks,
      })
      suggestedTask.title = title
      msg.approved = true
    } else {
      const taskId = suggestedTask.taskId
      if (!taskId) {
        toastError('Não foi possível identificar a tarefa.')
        return
      }

      if (actionType === 'DELETE_TASK') {
        await tasksStore.deleteTask(taskId)
        if (uiStore.selectedTaskId === taskId) uiStore.closeTaskDrawer()
      } else {
        const current = await tasksApi.get(taskId)
        const fields = suggestedTask.changedFields ?? ['title', 'description', 'status', 'priority', 'dueDate']
        await tasksStore.updateTask(taskId, {
          title: fields.includes('title') ? suggestedTask.title : current.title,
          description: (fields.includes('description') ? suggestedTask.description : current.description) ?? undefined,
          status: (fields.includes('status') ? suggestedTask.status : current.status) ?? current.status,
          priority: (fields.includes('priority') ? suggestedTask.priority : current.priority) ?? current.priority,
          dueDate: (fields.includes('dueDate') ? suggestedTask.dueDate : current.dueDate) ?? undefined,
          assigneeUserId: current.assigneeUserId,
        })
      }
      msg.approved = true
    }

    success(taskActions[actionType].success)
  } catch (err: any) {
    toastError(err?.message || 'Não foi possível aplicar a ação à tarefa.')
  } finally {
    applyingTaskId.value = null
  }
}

function dismissSuggestion(msg: ChatTimelineItem) {
  msg.dismissed = true
}

function renderMarkdown(content: string): string {
  const html = marked.parse(content, { breaks: true, gfm: true }) as string
  return DOMPurify.sanitize(html)
}

function isOtherOption(option: string): boolean {
  const normalized = option.trim().toLowerCase()
  return normalized === 'outro' || normalized === 'outros' || normalized.startsWith('outro')
}

function isDatePoll(question: string): boolean {
  const q = question.toLowerCase()
  return q.includes('prazo') || q.includes('data')
}

async function handlePollOptionClick(msg: ChatTimelineItem, option: string) {
  if (!msg.poll || msg.pollAnswered || chatStore.isSending || applyingTaskId.value) return

  if (msg.poll.allowCustomOption !== false && isOtherOption(option)) {
    msg.pollSelectedOptions = [option]
    customPollOpenId.value = msg.id
    customPollValue.value = ''
    return
  }

  if (!msg.poll.multiSelect) {
    msg.pollSelectedOptions = [option]
    msg.pollAnswered = true
    customPollOpenId.value = null
    await chatStore.sendMessage(option, currentSpaceId.value)
    return
  }

  const current = msg.pollSelectedOptions ?? []
  if (current.includes(option)) {
    msg.pollSelectedOptions = current.filter((item) => item !== option)
  } else {
    msg.pollSelectedOptions = [...current, option]
  }
}

async function submitCustomPoll(msg: ChatTimelineItem) {
  if (!msg.poll || msg.pollAnswered || chatStore.isSending || applyingTaskId.value) return
  const val = customPollValue.value.trim()
  if (!val) return

  msg.pollSelectedOptions = [val]
  msg.pollAnswered = true
  customPollOpenId.value = null
  customPollValue.value = ''
  await chatStore.sendMessage(val, currentSpaceId.value)
}

async function submitMultiPoll(msg: ChatTimelineItem) {
  if (!msg.poll || msg.pollAnswered || chatStore.isSending || applyingTaskId.value) return
  const selected = msg.pollSelectedOptions ?? []
  if (selected.length === 0) return

  msg.pollAnswered = true
  await chatStore.sendMessage(selected.join(', '), currentSpaceId.value)
}

function goToSettings() {
  uiStore.setChatOpen(false)
  router.push('/settings')
}
</script>

<template>
  <aside
    class="h-full bg-sidebar border-l border-line flex flex-col justify-between transition-all duration-300 z-40 select-none overflow-hidden"
    :class="uiStore.chatOpen ? 'w-[360px]' : 'w-0 border-l-0'"
  >
    <!-- Header -->
    <div class="h-[57px] flex items-center justify-between px-4 border-b border-line shrink-0 bg-panel">
      <div class="flex items-center gap-2 min-w-0">
        <div class="w-7 h-7 rounded-lg bg-accent-bg border border-accent/30 flex items-center justify-center text-accent shrink-0">
          <Bot class="w-4 h-4" />
        </div>
        <div class="min-w-0">
          <h3 class="text-sm font-semibold text-fg truncate">Assistente IA</h3>
          <p class="text-xs text-muted leading-tight truncate">
            {{ isAgentReady ? (chatStore.config?.model || chatStore.config?.name) : 'Não configurado' }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-1 shrink-0">
        <button
          type="button"
          class="text-muted hover:text-fg p-1.5 rounded-lg hover:bg-hover transition cursor-pointer"
          title="Limpar conversa"
          @click="chatStore.clearMessages"
        >
          <Trash2 class="w-3.5 h-3.5" />
        </button>

        <button
          type="button"
          class="text-muted hover:text-fg p-1.5 rounded-lg hover:bg-hover transition cursor-pointer"
          title="Configurar IA"
          @click="goToSettings"
        >
          <Settings class="w-3.5 h-3.5" />
        </button>

        <button
          type="button"
          class="text-muted hover:text-fg p-1.5 rounded-lg hover:bg-hover transition cursor-pointer"
          title="Fechar chat"
          @click="uiStore.toggleChat"
        >
          <X class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- Chat Messages Body -->
    <div ref="messagesContainer" class="flex-1 overflow-y-auto p-4 space-y-4 text-base select-text">
      <!-- Aviso caso agente não esteja configurado ou esteja desativado -->
      <div
        v-if="chatStore.configLoaded && !isAgentReady"
        class="p-3 rounded-xl bg-[#edc887]/10 border border-[#edc887]/30 text-dim space-y-2"
      >
        <div class="flex items-center gap-2 text-[#edc887] font-semibold">
          <AlertCircle class="w-4 h-4 shrink-0" />
          <span>{{ chatStore.config ? 'Agente desabilitado' : 'Configure sua IA' }}</span>
        </div>
        <p class="text-sm leading-relaxed">
          Escolha seu provedor (NVIDIA, DeepSeek, ChatGPT, Claude, Gemini, OpenRouter ou Ollama) e cole sua API Key nas Configurações.
        </p>
        <AppButton variant="secondary" size="sm" class="text-sm!" @click="goToSettings">
          <Settings class="w-3 h-3 mr-1" />
          Configurar Integração de IA
        </AppButton>
      </div>

      <!-- Histórico de Mensagens -->
      <div
        v-for="msg in chatStore.messages"
        :key="msg.id"
        class="flex flex-col gap-2"
        :class="msg.sender === 'user' ? 'items-end' : 'items-start'"
      >
        <div
          v-if="msg.sender === 'user'"
          class="max-w-[88%] rounded-xl px-3.5 py-2.5 leading-relaxed wrap-anywhere whitespace-pre-wrap bg-accent text-[#101114] font-medium rounded-tr-xs shadow-sm"
        >
          {{ msg.text }}
        </div>

        <div
          v-else
          class="max-w-[92%] rounded-xl px-3.5 py-2.5 leading-relaxed wrap-anywhere bg-panel-2 border border-line text-fg rounded-tl-xs chat-markdown"
          v-html="renderMarkdown(msg.text)"
        />

        <!-- Enquete / Opções Interativas -->
        <div
          v-if="msg.poll && !msg.suggestedTask && msg.poll.options.length > 0"
          class="w-full p-3 rounded-xl bg-panel border border-line text-left space-y-2"
        >
          <p v-if="!msg.text.endsWith(msg.poll.question)" class="text-sm font-semibold text-dim">
            {{ msg.poll.question }}
          </p>

          <div class="flex flex-wrap gap-1.5">
            <button
              v-for="opt in msg.poll.options"
              :key="opt"
              type="button"
              :disabled="msg.pollAnswered || chatStore.isSending || !!applyingTaskId"
              class="max-w-full px-2.5 py-1.5 rounded-lg text-sm wrap-anywhere font-medium border transition cursor-pointer disabled:cursor-default text-left"
              :class="
                msg.pollSelectedOptions?.includes(opt)
                  ? 'bg-accent/20 border-accent text-accent font-semibold'
                  : 'bg-panel-2 border-line text-fg hover:border-accent/60 hover:bg-hover disabled:opacity-60'
              "
              @click="handlePollOptionClick(msg, opt)"
            >
              {{ opt }}
            </button>
          </div>

          <div v-if="customPollOpenId === msg.id && !msg.pollAnswered" class="flex items-center gap-1.5 pt-1">
            <input
              v-model="customPollValue"
              :type="isDatePoll(msg.poll.question) ? 'date' : 'text'"
              placeholder="Digite sua escolha..."
              class="min-w-0 flex-1 px-2.5 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-sm focus:outline-none focus:border-accent"
              @keydown.enter.prevent="submitCustomPoll(msg)"
            />
            <AppButton
              variant="primary"
              size="sm"
              class="text-sm!"
              :disabled="!customPollValue.trim() || chatStore.isSending || !!applyingTaskId"
              @click="submitCustomPoll(msg)"
            >
              Enviar
            </AppButton>
          </div>

          <div v-else-if="msg.poll.multiSelect && !msg.pollAnswered" class="pt-1">
            <AppButton
              variant="primary"
              size="sm"
              class="w-full text-sm!"
              :disabled="!msg.pollSelectedOptions?.length || chatStore.isSending || !!applyingTaskId"
              @click="submitMultiPoll(msg)"
            >
              Confirmar escolha ({{ msg.pollSelectedOptions?.length || 0 }})
            </AppButton>
          </div>
        </div>

        <!-- Confirmação da ação sobre a tarefa -->
        <div
          v-if="msg.actionType && msg.suggestedTask && !msg.dismissed"
          class="w-full p-3 rounded-xl bg-panel border border-accent/30 text-left text-sm wrap-anywhere space-y-2.5 shadow-sm"
        >
          <div class="flex items-start justify-between gap-2">
            <label v-if="msg.actionType === 'CREATE_TASK' && !msg.approved" class="min-w-0 flex-1 space-y-1">
              <span class="block font-semibold text-dim">Título da tarefa</span>
              <input
                v-model="msg.suggestedTask.title"
                type="text"
                maxlength="200"
                :disabled="applyingTaskId === msg.id"
                class="w-full px-2.5 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-base focus:outline-none focus:border-accent"
              />
            </label>
            <span v-else class="font-semibold text-fg text-base leading-snug">
              {{ msg.suggestedTask.title }}
            </span>
            <PriorityDot v-if="msg.actionType !== 'DELETE_TASK'" :priority="msg.suggestedTask.priority || 'MEDIUM'" />
          </div>

          <p v-if="msg.actionType !== 'CREATE_TASK' && msg.suggestedTask.spaceId" class="text-muted">
            {{ spacesStore.spaces.find((space) => space.id === msg.suggestedTask?.spaceId)?.name }}
          </p>

          <p v-if="msg.actionType !== 'DELETE_TASK' && (msg.suggestedTask.description || msg.suggestedTask.changedFields?.includes('description'))" class="text-dim leading-relaxed">
            {{ msg.suggestedTask.description || 'Sem descrição' }}
          </p>

          <!-- Prazo -->
          <div v-if="msg.actionType !== 'DELETE_TASK' && (msg.suggestedTask.dueDate || msg.suggestedTask.changedFields?.includes('dueDate'))" class="flex items-center gap-1.5 text-muted">
            <Calendar class="w-3.5 h-3.5 text-accent" />
            <span>{{ msg.suggestedTask.dueDate ? `Prazo: ${msg.suggestedTask.dueDate}` : 'Sem prazo' }}</span>
          </div>

          <p v-if="msg.actionType === 'UPDATE_TASK' && msg.suggestedTask.status" class="text-muted">
            {{ { PENDING: 'Pendente', IN_PROGRESS: 'Em andamento', DONE: 'Concluída' }[msg.suggestedTask.status] }}
          </p>

          <!-- Subtarefas -->
          <details
            v-if="msg.suggestedTask.subtasks && msg.suggestedTask.subtasks.length > 0"
            :open="msg.suggestedTask.subtasks.length <= 8"
            class="space-y-1 pt-1 border-t border-line"
          >
            <summary class="cursor-pointer font-semibold text-dim">
              <span>Subtarefas ({{ msg.suggestedTask.subtasks.length }})</span>
            </summary>
            <ul class="space-y-1 pl-1">
              <li
                v-for="(sub, idx) in msg.suggestedTask.subtasks"
                :key="idx"
                class="text-muted flex items-start gap-1.5"
              >
                <span class="text-accent mt-0.5">•</span>
                <span>{{ sub }}</span>
              </li>
            </ul>
          </details>

          <!-- Seleção de Espaço -->
          <div v-if="!msg.approved && msg.actionType === 'CREATE_TASK' && spacesStore.spaces.length > 0" class="pt-1 border-t border-line">
            <label class="flex items-center gap-1.5 font-semibold text-dim mb-1">
              <Folder class="w-3.5 h-3.5 text-accent" />
              <span>Espaço de destino</span>
            </label>
            <select
              v-model="msg.selectedSpaceId"
              class="w-full px-2.5 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-sm focus:outline-none focus:border-accent"
            >
              <option value="" disabled>Selecione um espaço...</option>
              <option v-for="sp in spacesStore.spaces" :key="sp.id" :value="sp.id">
                {{ sp.name }}
              </option>
            </select>
          </div>

          <!-- Ações do Card -->
          <div v-if="!msg.approved" class="flex items-center gap-2 pt-1">
            <AppButton
              :variant="msg.actionType === 'DELETE_TASK' ? 'danger' : 'primary'"
              size="sm"
              class="flex-1 text-sm!"
              :loading="applyingTaskId === msg.id"
              :disabled="!!applyingTaskId || chatStore.isSending"
              @click="confirmTaskAction(msg)"
            >
              {{ taskActions[msg.actionType].button }}
            </AppButton>
            <AppButton
              variant="ghost"
              size="sm"
              class="text-sm!"
              :disabled="applyingTaskId === msg.id"
              @click="dismissSuggestion(msg)"
            >
              Cancelar
            </AppButton>
          </div>

          <div
            v-else
            class="text-center py-1 font-semibold text-[#9cd9b7] flex items-center justify-center gap-1.5"
          >
            <CheckCircle class="w-3.5 h-3.5" />
            <span>{{ taskActions[msg.actionType].success }}</span>
          </div>
        </div>
        <p v-if="msg.dismissed && msg.actionType" class="text-sm text-muted">Ação cancelada.</p>
      </div>

      <!-- Loading animado -->
      <div v-if="chatStore.isSending" class="flex items-center gap-2 text-muted italic">
        <Sparkles class="w-3.5 h-3.5 animate-spin text-accent" />
        <span>Pensando...</span>
      </div>
    </div>

    <!-- Input Footer -->
    <div class="p-3 border-t border-line bg-panel shrink-0">
      <form @submit.prevent="handleSendMessage" class="flex items-center gap-2">
        <textarea
          v-model="inputMessage"
          rows="2"
          placeholder="Converse ou peça para alterar suas tarefas..."
          class="min-w-0 flex-1 px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-base resize-none"
          @keydown.enter.exact.prevent="handleSendMessage"
        />
        <button
          type="submit"
          :disabled="chatStore.isSending || !!applyingTaskId || !inputMessage.trim()"
          class="p-2.5 rounded-lg bg-accent text-[#101114] hover:bg-[#b8acf2] disabled:opacity-40 transition cursor-pointer shrink-0"
        >
          <Send class="w-4 h-4" />
        </button>
      </form>
    </div>
  </aside>
</template>

<style scoped>
.chat-markdown :deep(p) {
  margin: 0.25rem 0;
}
.chat-markdown :deep(p:first-child) {
  margin-top: 0;
}
.chat-markdown :deep(p:last-child) {
  margin-bottom: 0;
}
.chat-markdown :deep(ul),
.chat-markdown :deep(ol) {
  margin: 0.35rem 0;
  padding-left: 1.15rem;
}
.chat-markdown :deep(ul) {
  list-style-type: disc;
}
.chat-markdown :deep(ol) {
  list-style-type: decimal;
}
.chat-markdown :deep(li) {
  margin: 0.15rem 0;
}
.chat-markdown :deep(h1),
.chat-markdown :deep(h2),
.chat-markdown :deep(h3) {
  font-weight: 700;
  color: var(--color-fg, #f3f4f6);
  margin: 0.5rem 0 0.25rem;
}
.chat-markdown :deep(h1) {
  font-size: 1.125em;
}
.chat-markdown :deep(code) {
  background: rgba(166, 154, 235, 0.12);
  color: #c8befa;
  padding: 0.1rem 0.3rem;
  border-radius: 0.25rem;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.875em;
}
.chat-markdown :deep(pre) {
  background: #101114;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.5rem;
  padding: 0.6rem;
  overflow-x: auto;
  margin: 0.4rem 0;
}
.chat-markdown :deep(pre code) {
  background: transparent;
  padding: 0;
  color: #e5e7eb;
}
.chat-markdown :deep(strong) {
  font-weight: 700;
  color: #ffffff;
}
.chat-markdown :deep(blockquote) {
  border-left: 2px solid #a69aeb;
  padding-left: 0.6rem;
  margin: 0.35rem 0;
  color: #9ca3af;
}
</style>
