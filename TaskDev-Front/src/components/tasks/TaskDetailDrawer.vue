<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useUiStore } from '@/stores/ui'
import { useTasksStore } from '@/stores/tasks'
import { useSpacesStore } from '@/stores/spaces'
import { useToast } from '@/composables/useToast'
import AppDrawer from '@/components/ui/AppDrawer.vue'
import AppButton from '@/components/ui/AppButton.vue'
import { tasksApi } from '@/api/tasks'
import { subtasksApi } from '@/api/subtasks'
import {
  Trash2,
  Check,
  Calendar,
  Flag,
  Folder,
  Plus,
  CheckSquare,
  Square,
  AlertCircle,
} from 'lucide-vue-next'
import type { TaskStatus, TaskPriority, TaskItem } from '@/types'

const route = useRoute()
const uiStore = useUiStore()
const tasksStore = useTasksStore()
const spacesStore = useSpacesStore()
const { success, error } = useToast()

// Tipagem de subtarefas em cache local
interface LocalSubtask {
  id: string
  taskId?: string
  title: string
  isCompleted: boolean
  isNew?: boolean
  isDeleted?: boolean
}

// Estado em cache/rascunho local no frontend
const title = ref('')
const titleError = ref(false)
const description = ref('')
const status = ref<TaskStatus>('PENDING')
const priority = ref<TaskPriority>('MEDIUM')
const dueDate = ref('')
const spaceId = ref('')
const assigneeUserId = ref('')
const localSubtasks = ref<LocalSubtask[]>([])
const newSubtaskTitle = ref('')
const expandedSubtasks = ref<Record<string, boolean>>({})

// Controle de alterações não salvas (snapshot inicial)
interface InitialSnapshot {
  title: string
  description: string
  status: TaskStatus
  priority: TaskPriority
  dueDate: string
  spaceId: string
  assigneeUserId: string
  subtasks: { id: string; title: string; isCompleted: boolean }[]
}
const initialSnapshot = ref<InitialSnapshot | null>(null)

function takeSnapshot() {
  initialSnapshot.value = {
    title: title.value,
    description: description.value,
    status: status.value,
    priority: priority.value,
    dueDate: dueDate.value,
    spaceId: spaceId.value,
    assigneeUserId: assigneeUserId.value,
    subtasks: localSubtasks.value.map((s) => ({
      id: s.id,
      title: s.title,
      isCompleted: s.isCompleted,
    })),
  }
}

const hasUnsavedChanges = computed(() => {
  if (!initialSnapshot.value) return false
  if (title.value !== initialSnapshot.value.title) return true
  if (description.value !== initialSnapshot.value.description) return true
  if (status.value !== initialSnapshot.value.status) return true
  if (priority.value !== initialSnapshot.value.priority) return true
  if (dueDate.value !== initialSnapshot.value.dueDate) return true
  if (spaceId.value !== initialSnapshot.value.spaceId) return true
  if (assigneeUserId.value !== initialSnapshot.value.assigneeUserId) return true

  // Subtarefas novas ou deletadas
  if (localSubtasks.value.some((s) => s.isNew || s.isDeleted)) return true

  // Subtarefas com status ou texto alterado
  const origMap = new Map(initialSnapshot.value.subtasks.map((s) => [s.id, s]))
  for (const s of localSubtasks.value) {
    const orig = origMap.get(s.id)
    if (!orig || orig.isCompleted !== s.isCompleted || orig.title !== s.title) {
      return true
    }
  }

  return false
})

const loading = ref(false)
const isSaving = ref(false)

const isCreating = computed(() => uiStore.selectedTaskId === null)

const activeSubtasks = computed(() => {
  return localSubtasks.value.filter((s) => !s.isDeleted)
})

function toggleExpandSubtask(id: string) {
  expandedSubtasks.value[id] = !expandedSubtasks.value[id]
}

function handleAddLocalSubtask() {
  const trimmed = newSubtaskTitle.value.trim()
  if (!trimmed) return
  localSubtasks.value.push({
    id: `temp-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
    title: trimmed,
    isCompleted: false,
    isNew: true,
  })
  newSubtaskTitle.value = ''
}

function handleToggleLocalSubtask(sub: LocalSubtask) {
  sub.isCompleted = !sub.isCompleted
}

function handleDeleteLocalSubtask(id: string) {
  const index = localSubtasks.value.findIndex((s) => s.id === id)
  if (index !== -1) {
    const target = localSubtasks.value[index]
    if (target) {
      if (target.isNew) {
        localSubtasks.value.splice(index, 1)
      } else {
        target.isDeleted = true
      }
    }
  }
}

function initNewTask() {
  title.value = ''
  description.value = ''
  status.value = 'PENDING'
  priority.value = 'MEDIUM'
  dueDate.value = ''
  spaceId.value = uiStore.targetSpaceIdForNewTask || (route.params.id as string) || spacesStore.spaces[0]?.id || ''
  assigneeUserId.value = ''
  localSubtasks.value = []
  newSubtaskTitle.value = ''
  expandedSubtasks.value = {}
  takeSnapshot()
}

function populateFromTask(task: TaskItem) {
  title.value = task.title
  description.value = task.description || ''
  status.value = task.status
  priority.value = task.priority
  dueDate.value = task.dueDate ? task.dueDate.slice(0, 10) : ''
  spaceId.value = task.spaceId
  assigneeUserId.value = task.assigneeUserId || ''
  localSubtasks.value = (task.subtasks || []).map((s) => ({
    id: s.id,
    taskId: s.taskId,
    title: s.title,
    isCompleted: s.isCompleted,
    isNew: false,
    isDeleted: false,
  }))
  newSubtaskTitle.value = ''
  expandedSubtasks.value = {}
  takeSnapshot()
}

// Watch que sincroniza sempre que abre ou troca a tarefa selecionada
watch(
  () => [uiStore.taskDrawerOpen, uiStore.selectedTaskId] as const,
  async ([open, taskId]) => {
    if (!open) return
    titleError.value = false

    if (taskId) {
      const task = tasksStore.tasks.find((t) => t.id === taskId)
      if (task) {
        populateFromTask(task)
      } else {
        try {
          loading.value = true
          const remote = await tasksApi.get(taskId)
          populateFromTask(remote)
        } catch {
          error('Erro ao carregar detalhes da tarefa')
          uiStore.closeTaskDrawer()
        } finally {
          loading.value = false
        }
      }
    } else {
      initNewTask()
    }
  },
  { immediate: true }
)

function handleCloseDrawer() {
  if (hasUnsavedChanges.value) {
    if (!confirm('Você possui alterações não salvas. Deseja realmente fechar e descartá-las?')) {
      return
    }
  }
  uiStore.closeTaskDrawer()
}

async function handleSave() {
  if (!title.value.trim()) {
    titleError.value = true
    error('O título da tarefa é obrigatório')
    return
  }
  titleError.value = false

  if (!spaceId.value) {
    error('Selecione um espaço para a tarefa')
    return
  }

  isSaving.value = true
  try {
    if (isCreating.value) {
      const created = await tasksStore.createTask({
        spaceId: spaceId.value,
        title: title.value.trim(),
        description: description.value.trim() || undefined,
        status: status.value,
        priority: priority.value,
        dueDate: dueDate.value ? dueDate.value.slice(0, 10) : undefined,
        assigneeUserId: assigneeUserId.value || undefined,
      })

      // Salva subtarefas criadas
      for (const sub of activeSubtasks.value) {
        try {
          const createdSub = await subtasksApi.create(created.id, sub.title)
          if (sub.isCompleted) {
            await subtasksApi.toggle(createdSub.id)
          }
        } catch (e) {
          console.error('Erro ao salvar subtarefa:', e)
        }
      }

      await tasksStore.loadTasks(spaceId.value)
      success('Tarefa criada com sucesso!')
      uiStore.closeTaskDrawer()
    } else if (uiStore.selectedTaskId) {
      const taskId = uiStore.selectedTaskId

      // 1. Atualiza campos principais da tarefa
      await tasksStore.updateTask(taskId, {
        spaceId: spaceId.value,
        title: title.value.trim(),
        description: description.value.trim() || undefined,
        status: status.value,
        priority: priority.value,
        dueDate: dueDate.value ? dueDate.value.slice(0, 10) : undefined,
        assigneeUserId: assigneeUserId.value || undefined,
      })

      // 2. Sincroniza subtarefas deletadas
      const deleted = localSubtasks.value.filter((s) => s.isDeleted && !s.isNew)
      for (const sub of deleted) {
        try {
          await subtasksApi.delete(sub.id)
        } catch (e) {
          console.error('Erro ao deletar subtarefa:', e)
        }
      }

      // 3. Sincroniza novas subtarefas
      const newSubs = localSubtasks.value.filter((s) => s.isNew && !s.isDeleted)
      for (const sub of newSubs) {
        try {
          const createdSub = await subtasksApi.create(taskId, sub.title)
          if (sub.isCompleted) {
            await subtasksApi.toggle(createdSub.id)
          }
        } catch (e) {
          console.error('Erro ao criar subtarefa:', e)
        }
      }

      // 4. Sincroniza subtarefas existentes cujo status foi alternado
      const origMap = new Map(initialSnapshot.value?.subtasks.map((s) => [s.id, s]))
      const existingActive = localSubtasks.value.filter((s) => !s.isNew && !s.isDeleted)
      for (const sub of existingActive) {
        const orig = origMap.get(sub.id)
        if (orig && orig.isCompleted !== sub.isCompleted) {
          try {
            await subtasksApi.toggle(sub.id)
          } catch (e) {
            console.error('Erro ao alternar status da subtarefa:', e)
          }
        }
      }

      await tasksStore.loadTasks(spaceId.value)
      success('Alterações salvas com sucesso!')
      uiStore.closeTaskDrawer()
    }
  } catch (err: any) {
    error(err.message || 'Erro ao salvar tarefa')
  } finally {
    isSaving.value = false
  }
}

async function handleDelete() {
  if (!uiStore.selectedTaskId) return
  if (!confirm('Deseja realmente excluir esta tarefa?')) return

  loading.value = true
  try {
    await tasksStore.deleteTask(uiStore.selectedTaskId)
    success('Tarefa excluída')
    uiStore.closeTaskDrawer()
  } catch (err: any) {
    error(err.message || 'Erro ao excluir tarefa')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AppDrawer
    :show="uiStore.taskDrawerOpen"
    :title="isCreating ? 'Nova Tarefa' : 'Detalhes da Tarefa'"
    width="max-w-[500px]"
    @close="handleCloseDrawer"
  >
    <div class="space-y-5">
      <!-- Indicador de alterações não salvas -->
      <div
        v-if="hasUnsavedChanges"
        class="flex items-center gap-2 px-3 py-2 rounded-lg bg-[#edc887]/10 border border-[#edc887]/30 text-[#edc887] text-xs font-medium animate-fade-in"
      >
        <AlertCircle class="w-4 h-4 shrink-0" />
        <span>Você tem alterações em rascunho. Clique em <strong>Salvar Alterações</strong> para aplicar.</span>
      </div>

      <!-- Campo Título -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider flex items-center justify-between">
          <span class="flex items-center gap-1">
            Título da Tarefa
            <span class="text-[#e9a0a7] font-bold">*</span>
          </span>
          <span v-if="titleError" class="text-xs text-[#e9a0a7] font-normal lowercase">obrigatório</span>
        </label>
        <input
          v-model="title"
          type="text"
          placeholder="Ex: Criar migrations do banco de dados..."
          required
          autofocus
          class="w-full px-3.5 py-2.5 rounded-xl bg-panel-2 border text-fg placeholder:text-muted focus:outline-none text-sm font-medium transition shadow-inner"
          :class="titleError ? 'border-[#e9a0a7] focus:border-[#e9a0a7]' : 'border-line focus:border-accent'"
          @input="titleError = false"
        />
      </div>

      <!-- Grid de Propriedades Rápidas -->
      <div class="space-y-3 p-3.5 rounded-xl bg-panel-2 border border-line text-xs">
        <!-- Espaço -->
        <div class="flex items-center justify-between">
          <span class="flex items-center gap-2 text-dim text-xs font-medium">
            <Folder class="w-4 h-4 text-muted" />
            Espaço
          </span>
          <select
            v-model="spaceId"
            class="px-2.5 py-1.5 rounded-lg bg-panel border border-line text-fg text-xs focus:outline-none focus:border-accent"
          >
            <option v-for="sp in spacesStore.spaces" :key="sp.id" :value="sp.id">
              {{ sp.name }}
            </option>
          </select>
        </div>

        <!-- Status -->
        <div class="flex items-center justify-between">
          <span class="flex items-center gap-2 text-dim text-xs font-medium">
            <Check class="w-4 h-4 text-muted" />
            Status
          </span>
          <select
            v-model="status"
            class="px-2.5 py-1.5 rounded-lg bg-panel border border-line text-fg text-xs focus:outline-none focus:border-accent"
          >
            <option value="PENDING">Pendente</option>
            <option value="IN_PROGRESS">Em andamento</option>
            <option value="DONE">Concluído</option>
          </select>
        </div>

        <!-- Prioridade -->
        <div class="flex items-center justify-between">
          <span class="flex items-center gap-2 text-dim text-xs font-medium">
            <Flag class="w-4 h-4 text-muted" />
            Prioridade
          </span>
          <select
            v-model="priority"
            class="px-2.5 py-1.5 rounded-lg bg-panel border border-line text-fg text-xs focus:outline-none focus:border-accent"
          >
            <option value="LOW">Baixa</option>
            <option value="MEDIUM">Média</option>
            <option value="HIGH">Alta</option>
            <option value="URGENT">Urgente</option>
          </select>
        </div>

        <!-- Prazo -->
        <div class="flex items-center justify-between">
          <span class="flex items-center gap-2 text-dim text-xs font-medium">
            <Calendar class="w-4 h-4 text-muted" />
            Prazo
          </span>
          <input
            v-model="dueDate"
            type="date"
            class="px-2.5 py-1.5 rounded-lg bg-panel border border-line text-fg text-xs focus:outline-none focus:border-accent"
          />
        </div>
      </div>

      <!-- Descrição -->
      <div class="space-y-1.5">
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider">
          Descrição
        </label>
        <textarea
          v-model="description"
          rows="4"
          placeholder="Adicione mais contexto ou notas para esta tarefa..."
          class="w-full px-3 py-2.5 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-sm leading-relaxed"
        />
      </div>

      <!-- Subtarefas (Edição Livre em Cache de Memória) -->
      <div class="space-y-2 pt-2 border-t border-line">
        <div class="flex items-center justify-between">
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider">
            Subtarefas ({{ activeSubtasks.length }})
          </label>
          <span class="text-xs text-muted">
            Rascunho em memória
          </span>
        </div>

        <!-- Lista de subtarefas ativas -->
        <div v-if="activeSubtasks.length > 0" class="space-y-1">
          <div
            v-for="sub in activeSubtasks"
            :key="sub.id"
            class="flex items-start justify-between gap-2 p-2 rounded-lg bg-panel-2 hover:bg-hover group transition text-sm border border-line/40"
          >
            <button
              type="button"
              class="flex items-start gap-2 text-left flex-1 min-w-0 cursor-pointer"
              @click="handleToggleLocalSubtask(sub)"
            >
              <CheckSquare v-if="sub.isCompleted" class="w-4 h-4 text-accent shrink-0 mt-0.5" />
              <Square v-else class="w-4 h-4 text-muted shrink-0 mt-0.5 group-hover:text-dim" />

              <div class="flex-1 min-w-0 leading-snug">
                <span
                  class="break-words"
                  :class="sub.isCompleted ? 'line-through text-muted' : 'text-fg'"
                >
                  {{ expandedSubtasks[sub.id] || sub.title.length <= 60 ? sub.title : sub.title.slice(0, 60) + '...' }}
                </span>
                <button
                  v-if="sub.title.length > 60"
                  type="button"
                  class="inline-block text-xs font-medium text-accent hover:underline ml-1 cursor-pointer select-none"
                  @click.stop="toggleExpandSubtask(sub.id)"
                >
                  {{ expandedSubtasks[sub.id] ? 'ler menos' : 'ler mais...' }}
                </button>
              </div>
            </button>

            <button
              type="button"
              class="opacity-0 group-hover:opacity-100 text-muted hover:text-[#e9a0a7] p-1 transition cursor-pointer shrink-0 mt-0.5"
              title="Remover subtarefa"
              @click="handleDeleteLocalSubtask(sub.id)"
            >
              <Trash2 class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>

        <!-- Adicionar nova subtarefa -->
        <form @submit.prevent="handleAddLocalSubtask" class="flex items-center gap-2 pt-1">
          <input
            v-model="newSubtaskTitle"
            type="text"
            placeholder="Adicionar subtarefa em rascunho..."
            class="flex-1 px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
          />
          <button
            type="submit"
            :disabled="!newSubtaskTitle.trim()"
            class="p-2 rounded-lg bg-panel-2 border border-line text-dim hover:text-fg hover:bg-hover disabled:opacity-50 cursor-pointer"
          >
            <Plus class="w-4 h-4" />
          </button>
        </form>
      </div>
    </div>

    <!-- Footer do Drawer -->
    <template #footer>
      <div>
        <AppButton
          v-if="!isCreating"
          variant="danger"
          size="sm"
          :loading="loading"
          @click="handleDelete"
        >
          <Trash2 class="w-3.5 h-3.5 mr-1" />
          Excluir
        </AppButton>
      </div>

      <div class="flex items-center gap-2">
        <AppButton variant="ghost" size="sm" @click="handleCloseDrawer">
          Cancelar
        </AppButton>
        <AppButton variant="primary" size="sm" :loading="isSaving" @click="handleSave">
          {{ isCreating ? 'Criar Tarefa' : 'Salvar Alterações' }}
        </AppButton>
      </div>
    </template>
  </AppDrawer>
</template>
