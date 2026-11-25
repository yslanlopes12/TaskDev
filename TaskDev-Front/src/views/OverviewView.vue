<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useTasksStore } from '@/stores/tasks'
import { useSpacesStore } from '@/stores/spaces'
import { useUiStore } from '@/stores/ui'
import { dashboardApi } from '@/api/dashboard'
import type { TaskPriority, DashboardData } from '@/types'
import TaskChecklist from '@/components/tasks/TaskChecklist.vue'
import KanbanBoard from '@/components/tasks/KanbanBoard.vue'
import {
  List,
  Kanban,
  Search,
  Filter,
  CheckCircle2,
  Clock,
  Calendar,
  AlertCircle,
  Plus,
} from 'lucide-vue-next'
import AppButton from '@/components/ui/AppButton.vue'

const authStore = useAuthStore()
const tasksStore = useTasksStore()
const spacesStore = useSpacesStore()
const uiStore = useUiStore()

const dashboardData = ref<DashboardData | null>(null)
const selectedTab = ref<'ALL' | 'TODAY' | 'PENDING' | 'PLANNED' | 'DONE'>('ALL')
const searchQuery = ref('')
const filterSpaceId = ref('')
const filterPriority = ref<TaskPriority | ''>('')
const sortBy = ref<'position' | 'dueDate' | 'priority' | 'title'>('position')

const currentDateFormatted = computed(() => {
  return new Intl.DateTimeFormat('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date())
})

onMounted(async () => {
  await Promise.all([
    tasksStore.loadTasks(),
    spacesStore.loadSpaces(),
    loadDashboardMetrics(),
  ])
})

async function loadDashboardMetrics() {
  try {
    dashboardData.value = await dashboardApi.get()
  } catch {}
}

// Filtros e ordenação combinados
const filteredTasks = computed(() => {
  const today = new Date().toISOString().slice(0, 10)
  let list = [...tasksStore.tasks]

  // Filtro por Tab
  if (selectedTab.value === 'TODAY') {
    list = list.filter((t) => t.status !== 'DONE' && t.dueDate?.slice(0, 10) === today)
  } else if (selectedTab.value === 'PENDING') {
    list = list.filter((t) => t.status === 'PENDING')
  } else if (selectedTab.value === 'PLANNED') {
    list = list.filter((t) => t.status !== 'DONE' && t.dueDate && t.dueDate.slice(0, 10) > today)
  } else if (selectedTab.value === 'DONE') {
    list = list.filter((t) => t.status === 'DONE')
  }

  // Filtro por Busca
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter((t) => t.title.toLowerCase().includes(q) || t.description?.toLowerCase().includes(q))
  }

  // Filtro por Espaço
  if (filterSpaceId.value) {
    list = list.filter((t) => t.spaceId === filterSpaceId.value)
  }

  // Filtro por Prioridade
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

// Drag só é permitido quando a tab for ALL e sem filtros adicionais de busca/ordenação
const canDragManual = computed(() => {
  return (
    selectedTab.value === 'ALL' &&
    !searchQuery.value &&
    !filterPriority.value &&
    sortBy.value === 'position'
  )
})

// Contadores de métricas
const metrics = computed(() => {
  const today = new Date().toISOString().slice(0, 10)
  const all = tasksStore.tasks

  const doneCount = all.filter((t) => t.status === 'DONE').length
  const notDone = all.filter((t) => t.status !== 'DONE')

  const todayCount = notDone.filter((t) => t.dueDate?.slice(0, 10) === today).length
  const pendingCount = notDone.filter((t) => t.status === 'PENDING').length
  const plannedCount = notDone.filter((t) => t.dueDate && t.dueDate.slice(0, 10) > today).length

  return { todayCount, pendingCount, plannedCount, doneCount }
})

function handleNewTask() {
  uiStore.openTaskDrawer(null)
}
</script>

<template>
  <div class="space-y-6 max-w-6xl mx-auto">
    <!-- Header: Saudação e Data -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <p class="text-xs font-semibold text-accent uppercase tracking-wider capitalize">
          {{ currentDateFormatted }}
        </p>
        <h1 class="text-xl font-bold text-fg mt-0.5">
          Olá, {{ authStore.user?.firstName || 'Desenvolvedor' }}
        </h1>
      </div>

      <div class="flex items-center gap-2">
        <AppButton variant="primary" size="sm" @click="handleNewTask">
          <Plus class="w-3.5 h-3.5 mr-1" />
          Nova Tarefa
        </AppButton>
      </div>
    </div>

    <!-- Cards de Métricas (Hoje, Pendentes, Planejadas, Concluídas) -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
      <!-- Hoje -->
      <div
        class="p-4 rounded-xl bg-panel-2 border border-line hover:border-line-soft transition cursor-pointer"
        :class="{ 'border-accent bg-accent/5': selectedTab === 'TODAY' }"
        @click="selectedTab = 'TODAY'"
      >
        <div class="flex items-center justify-between text-muted mb-2">
          <span class="text-xs font-semibold uppercase tracking-wider">Hoje</span>
          <Clock class="w-4 h-4 text-accent" />
        </div>
        <div class="text-2xl font-bold text-fg">{{ metrics.todayCount }}</div>
      </div>

      <!-- Pendentes -->
      <div
        class="p-4 rounded-xl bg-panel-2 border border-line hover:border-line-soft transition cursor-pointer"
        :class="{ 'border-[#edc887] bg-[#edc887]/5': selectedTab === 'PENDING' }"
        @click="selectedTab = 'PENDING'"
      >
        <div class="flex items-center justify-between text-muted mb-2">
          <span class="text-xs font-semibold uppercase tracking-wider">Pendentes</span>
          <AlertCircle class="w-4 h-4 text-[#edc887]" />
        </div>
        <div class="text-2xl font-bold text-fg">{{ metrics.pendingCount }}</div>
      </div>

      <!-- Planejadas -->
      <div
        class="p-4 rounded-xl bg-panel-2 border border-line hover:border-line-soft transition cursor-pointer"
        :class="{ 'border-[#a3bee9] bg-[#a3bee9]/5': selectedTab === 'PLANNED' }"
        @click="selectedTab = 'PLANNED'"
      >
        <div class="flex items-center justify-between text-muted mb-2">
          <span class="text-xs font-semibold uppercase tracking-wider">Planejadas</span>
          <Calendar class="w-4 h-4 text-[#a3bee9]" />
        </div>
        <div class="text-2xl font-bold text-fg">{{ metrics.plannedCount }}</div>
      </div>

      <!-- Concluídas -->
      <div
        class="p-4 rounded-xl bg-panel-2 border border-line hover:border-line-soft transition cursor-pointer"
        :class="{ 'border-[#9cd9b7] bg-[#9cd9b7]/5': selectedTab === 'DONE' }"
        @click="selectedTab = 'DONE'"
      >
        <div class="flex items-center justify-between text-muted mb-2">
          <span class="text-xs font-semibold uppercase tracking-wider">Concluídas</span>
          <CheckCircle2 class="w-4 h-4 text-[#9cd9b7]" />
        </div>
        <div class="text-2xl font-bold text-fg">{{ metrics.doneCount }}</div>
      </div>
    </div>

    <!-- Barra de Controle: Tabs + Filtros + Modo Lista/Kanban -->
    <div class="space-y-3">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-line pb-3">
        <!-- Tabs -->
        <div class="flex items-center gap-1 overflow-x-auto pb-1 md:pb-0">
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedTab === 'ALL' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedTab = 'ALL'"
          >
            Todas
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedTab === 'TODAY' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedTab = 'TODAY'"
          >
            Hoje
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedTab === 'PENDING' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedTab = 'PENDING'"
          >
            Pendentes
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedTab === 'PLANNED' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedTab = 'PLANNED'"
          >
            Planejadas
          </button>
          <button
            type="button"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer shrink-0"
            :class="selectedTab === 'DONE' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
            @click="selectedTab = 'DONE'"
          >
            Concluídas
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

      <!-- Linha de Filtros Adicionais -->
      <div class="flex flex-wrap items-center gap-2">
        <!-- Busca -->
        <div class="relative flex-1 min-w-[200px]">
          <Search class="w-3.5 h-3.5 text-muted absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Filtrar por título ou descrição..."
            class="w-full pl-8 pr-3 py-1.5 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
          />
        </div>

        <!-- Filtro por Espaço -->
        <select
          v-model="filterSpaceId"
          class="px-3 py-1.5 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
        >
          <option value="">Todos os espaços</option>
          <option v-for="sp in spacesStore.spaces" :key="sp.id" :value="sp.id">
            {{ sp.name }}
          </option>
        </select>

        <!-- Filtro por Prioridade -->
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

        <!-- Ordenação -->
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

    <!-- Renderização do Conteúdo: Lista ou Kanban -->
    <div class="min-h-[400px]">
      <!-- Modo Lista -->
      <TaskChecklist
        v-if="uiStore.viewMode === 'list'"
        :tasks="filteredTasks"
        :allow-drag="canDragManual"
        :group-by="selectedTab === 'ALL' && !searchQuery ? 'timeline' : 'none'"
      />

      <!-- Modo Kanban -->
      <KanbanBoard
        v-else
        :tasks="filteredTasks"
      />
    </div>
  </div>
</template>
