<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useSpacesStore } from '@/stores/spaces'
import { useUiStore } from '@/stores/ui'
import TaskChecklist from '@/components/tasks/TaskChecklist.vue'
import KanbanBoard from '@/components/tasks/KanbanBoard.vue'
import SpaceMembersPanel from '@/components/spaces/SpaceMembersPanel.vue'
import AppButton from '@/components/ui/AppButton.vue'
import type { TaskPriority, TaskStatus } from '@/types'
import {
  Settings,
  Users,
  Plus,
  List,
  Kanban,
  Search,
  Folder,
  Code,
  Briefcase,
  GraduationCap,
  BookOpen,
  Layers,
  Target,
  Flag,
} from 'lucide-vue-next'

const route = useRoute()
const authStore = useAuthStore()
const tasksStore = useTasksStore()
const spacesStore = useSpacesStore()
const uiStore = useUiStore()

const spaceId = computed(() => route.params.id as string)
const currentSpace = computed(() => spacesStore.getSpaceById(spaceId.value) || spacesStore.currentSpace)

const isOwner = computed(() => {
  if (!currentSpace.value) return false
  return currentSpace.value.ownerUserId === authStore.user?.id || authStore.isAdmin
})

const selectedStatus = ref<TaskStatus | 'ALL'>('ALL')
const searchQuery = ref('')
const filterPriority = ref<TaskPriority | ''>('')
const sortBy = ref<'position' | 'dueDate' | 'priority' | 'title'>('position')

const showMembersModal = ref(false)

const iconMap: Record<string, any> = {
  Folder,
  Code,
  Briefcase,
  GraduationCap,
  BookOpen,
  Layers,
  Target,
  Flag,
}

function getSpaceIcon(name?: string) {
  if (name && iconMap[name]) return iconMap[name]
  return Folder
}

async function loadSpaceData() {
  if (!spaceId.value) return
  await Promise.all([
    spacesStore.selectSpace(spaceId.value),
    tasksStore.loadTasks(spaceId.value),
    spacesStore.loadMembers(spaceId.value),
  ])
}

onMounted(() => {
  loadSpaceData()
})

watch(
  () => route.params.id,
  () => {
    loadSpaceData()
  }
)

const spaceTasks = computed(() => {
  return tasksStore.tasks.filter((t) => t.spaceId === spaceId.value)
})

const filteredTasks = computed(() => {
  let list = [...spaceTasks.value]

  // Status
  if (selectedStatus.value !== 'ALL') {
    list = list.filter((t) => t.status === selectedStatus.value)
  }

  // Busca
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter((t) => t.title.toLowerCase().includes(q) || t.description?.toLowerCase().includes(q))
  }

  // Prioridade
  if (filterPriority.value) {
    list = list.filter((t) => t.priority === filterPriority.value)
  }

  // Ordenação
  if (sortBy.value === 'position') {
    list.sort((a, b) => a.position - b.position)
  } else if (sortBy.value === 'dueDate') {
    list.sort((a, b) => (a.dueDate || '9999').localeCompare(b.dueDate || '9999'))
  } else if (sortBy.value === 'priority') {
    const weights: Record<TaskPriority, number> = { URGENT: 4, HIGH: 3, MEDIUM: 2, LOW: 1 }
    list.sort((a, b) => weights[b.priority] - weights[a.priority])
  } else if (sortBy.value === 'title') {
    list.sort((a, b) => a.title.localeCompare(b.title))
  }

  return list
})

const statusCounts = computed(() => {
  const all = spaceTasks.value
  return {
    total: all.length,
    pending: all.filter((t) => t.status === 'PENDING').length,
    inProgress: all.filter((t) => t.status === 'IN_PROGRESS').length,
    done: all.filter((t) => t.status === 'DONE').length,
  }
})

const canDragManual = computed(() => {
  return (
    selectedStatus.value === 'ALL' &&
    !searchQuery.value &&
    !filterPriority.value &&
    sortBy.value === 'position'
  )
})

function handleNewTask() {
  uiStore.openTaskDrawer(null, spaceId.value)
}

function handleEditSpace() {
  uiStore.openSpaceModal(spaceId.value)
}
</script>

<template>
  <div class="space-y-6 max-w-6xl mx-auto">
    <!-- Header do Espaço -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 rounded-2xl bg-panel-2 border border-line">
      <div class="flex items-center gap-3.5 min-w-0">
        <div
          class="w-12 h-12 rounded-xl flex items-center justify-center shrink-0 border"
          :style="{
            backgroundColor: `${currentSpace?.color || '#a69aeb'}15`,
            borderColor: `${currentSpace?.color || '#a69aeb'}35`,
            color: currentSpace?.color || '#a69aeb',
          }"
        >
          <component :is="getSpaceIcon(currentSpace?.icon)" class="w-6 h-6" />
        </div>

        <div class="min-w-0">
          <div class="flex items-center gap-2">
            <h1 class="text-lg font-bold text-fg truncate">
              {{ currentSpace?.name || 'Carregando...' }}
            </h1>
            <span
              v-if="currentSpace?.visibility === 'SHARED'"
              class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-accent/15 text-accent border border-accent/20"
            >
              Compartilhado
            </span>
          </div>
          <p class="text-xs text-muted mt-0.5">
            {{ statusCounts.pending + statusCounts.inProgress }} tarefas ativas
          </p>
        </div>
      </div>

      <!-- Ações do Espaço -->
      <div class="flex items-center gap-2 shrink-0">
        <!-- Membros (se compartilhado) -->
        <AppButton
          v-if="currentSpace?.visibility === 'SHARED'"
          variant="secondary"
          size="sm"
          @click="showMembersModal = true"
        >
          <Users class="w-3.5 h-3.5 mr-1" />
          Membros
        </AppButton>

        <!-- Editar Espaço (para owner) -->
        <AppButton
          v-if="isOwner"
          variant="secondary"
          size="sm"
          @click="handleEditSpace"
        >
          <Settings class="w-3.5 h-3.5 mr-1" />
          Ajustes
        </AppButton>

        <!-- Nova Tarefa -->
        <AppButton variant="primary" size="sm" @click="handleNewTask">
          <Plus class="w-3.5 h-3.5 mr-1" />
          Nova Tarefa
        </AppButton>
      </div>
    </div>

    <!-- Toolbar: Status Chips + View Switch + Filtros -->
    <div class="space-y-3">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-line pb-3">
        <!-- Status Chips -->
        <div class="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0">
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedStatus === 'ALL' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedStatus = 'ALL'"
          >
            Todas ({{ statusCounts.total }})
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedStatus === 'PENDING' ? 'bg-panel-2 text-[#edc887] border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedStatus = 'PENDING'"
          >
            Pendente ({{ statusCounts.pending }})
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedStatus === 'IN_PROGRESS' ? 'bg-panel-2 text-[#a3bee9] border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedStatus = 'IN_PROGRESS'"
          >
            Em andamento ({{ statusCounts.inProgress }})
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedStatus === 'DONE' ? 'bg-panel-2 text-[#9cd9b7] border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedStatus = 'DONE'"
          >
            Concluído ({{ statusCounts.done }})
          </button>
        </div>

        <!-- Alternador Lista / Quadro -->
        <div class="flex items-center gap-1 bg-panel-2 border border-line p-1 rounded-lg shrink-0 self-start md:self-auto">
          <button
            type="button"
            class="flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium transition cursor-pointer"
            :class="uiStore.viewMode === 'list' ? 'bg-panel text-fg shadow-xs font-semibold' : 'text-muted hover:text-fg'"
            @click="uiStore.setViewMode('list')"
          >
            <List class="w-3.5 h-3.5" />
            <span>Lista</span>
          </button>

          <button
            type="button"
            class="flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium transition cursor-pointer"
            :class="uiStore.viewMode === 'board' ? 'bg-panel text-fg shadow-xs font-semibold' : 'text-muted hover:text-fg'"
            @click="uiStore.setViewMode('board')"
          >
            <Kanban class="w-3.5 h-3.5" />
            <span>Quadro</span>
          </button>
        </div>
      </div>

      <!-- Filtros adicionais -->
      <div class="flex flex-wrap items-center gap-2">
        <div class="relative flex-1 min-w-[200px]">
          <Search class="w-3.5 h-3.5 text-muted absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Filtrar tarefas no espaço..."
            class="w-full pl-8 pr-3 py-1.5 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
          />
        </div>

        <select
          v-model="filterPriority"
          class="px-3 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
        >
          <option value="">Todas as prioridades</option>
          <option value="URGENT">Urgente</option>
          <option value="HIGH">Alta</option>
          <option value="MEDIUM">Média</option>
          <option value="LOW">Baixa</option>
        </select>

        <select
          v-model="sortBy"
          class="px-3 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
        >
          <option value="position">Ordem manual</option>
          <option value="dueDate">Prazo</option>
          <option value="priority">Prioridade</option>
          <option value="title">Título</option>
        </select>
      </div>
    </div>

    <!-- Conteúdo -->
    <div class="min-h-[400px]">
      <TaskChecklist
        v-if="uiStore.viewMode === 'list'"
        :tasks="filteredTasks"
        :allow-drag="canDragManual"
        :group-by="selectedStatus === 'ALL' && !searchQuery ? 'status' : 'none'"
      />

      <KanbanBoard
        v-else
        :tasks="filteredTasks"
        :space-id="spaceId"
      />
    </div>

    <!-- Modal de Membros -->
    <SpaceMembersPanel
      :show="showMembersModal"
      :space-id="spaceId"
      @close="showMembersModal = false"
    />
  </div>
</template>
