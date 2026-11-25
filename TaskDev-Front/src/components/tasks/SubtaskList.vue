<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import type { Subtask, TaskPriority } from '@/types'
import { subtasksApi } from '@/api/subtasks'
import { useGamificationStore } from '@/stores/gamification'
import { Plus, Trash2, CheckSquare, Square } from 'lucide-vue-next'

const props = defineProps<{
  taskId: string
  subtasks?: Subtask[]
  priority?: TaskPriority
}>()

const emit = defineEmits<{
  (e: 'update'): void
}>()

const gamificationStore = useGamificationStore()
const items = ref<Subtask[]>([])
const newTitle = ref('')
const loading = ref(false)
const expandedMap = ref<Record<string, boolean>>({})

function toggleExpand(id: string) {
  expandedMap.value[id] = !expandedMap.value[id]
}

function isExpanded(id: string): boolean {
  return !!expandedMap.value[id]
}

function getTaskTotalXp(p?: TaskPriority): number {
  switch (p) {
    case 'URGENT': return 80
    case 'HIGH': return 50
    case 'MEDIUM': return 30
    case 'LOW': return 15
    default: return 30
  }
}

const fractionXp = computed(() => {
  const count = items.value.length || 1
  const parentXp = getTaskTotalXp(props.priority)
  return Math.max(1, Math.floor(parentXp / count))
})

watch(
  () => props.subtasks,
  (newSubs) => {
    if (newSubs && newSubs.length > 0) {
      items.value = [...newSubs]
    } else if (props.taskId) {
      loadSubtasks()
    } else {
      items.value = []
    }
  },
  { immediate: true, deep: true }
)

watch(
  () => props.taskId,
  (newTaskId) => {
    if (newTaskId) {
      loadSubtasks()
    } else {
      items.value = []
    }
  }
)

async function loadSubtasks() {
  if (!props.taskId) return
  try {
    const list = await subtasksApi.list(props.taskId)
    items.value = list || []
  } catch (err) {
    console.error('Erro ao buscar subtarefas:', err)
  }
}

async function handleToggle(subtask: Subtask) {
  const previousState = subtask.isCompleted
  const willBeCompleted = !previousState
  subtask.isCompleted = willBeCompleted

  const estimatedPoints = fractionXp.value

  if (willBeCompleted) {
    gamificationStore.addXp(estimatedPoints)
  } else {
    gamificationStore.removeXp(estimatedPoints)
  }

  try {
    const updated = await subtasksApi.toggle(subtask.id)
    subtask.isCompleted = updated.isCompleted

    if (willBeCompleted) {
      const awarded = updated.pointsAwarded ?? 0
      const diff = awarded - estimatedPoints
      if (diff !== 0) {
        gamificationStore.totalXp = Math.max(0, gamificationStore.totalXp + diff)
      }
    } else {
      const deducted = updated.pointsDeducted ?? 0
      const diff = deducted - estimatedPoints
      if (diff !== 0) {
        gamificationStore.totalXp = Math.max(0, gamificationStore.totalXp - diff)
      }
    }

    emit('update')
  } catch (err) {
    if (willBeCompleted) {
      gamificationStore.removeXp(estimatedPoints)
    } else {
      gamificationStore.addXp(estimatedPoints)
    }
    subtask.isCompleted = previousState
    console.error('Erro ao alternar subtarefa:', err)
  }
}

async function handleAdd() {
  if (!newTitle.value.trim() || !props.taskId) return
  loading.value = true
  try {
    const created = await subtasksApi.create(props.taskId, newTitle.value.trim())
    items.value.push(created)
    newTitle.value = ''
    emit('update')
  } catch (err) {
    console.error('Erro ao adicionar subtarefa:', err)
  } finally {
    loading.value = false
  }
}

async function handleDelete(id: string) {
  try {
    await subtasksApi.delete(id)
    items.value = items.value.filter((s) => s.id !== id)
    emit('update')
  } catch (err) {
    console.error('Erro ao excluir subtarefa:', err)
  }
}
</script>

<template>
  <div class="space-y-2">
    <!-- Lista de subtarefas -->
    <div v-if="items && items.length > 0" class="space-y-1">
      <div
        v-for="sub in items"
        :key="sub.id"
        class="flex items-start justify-between gap-2 p-1.5 rounded hover:bg-hover group transition text-sm"
      >
        <button
          type="button"
          class="flex items-start gap-2 text-left flex-1 min-w-0 cursor-pointer"
          @click="handleToggle(sub)"
        >
          <CheckSquare v-if="sub.isCompleted" class="w-4 h-4 text-accent shrink-0 mt-0.5" />
          <Square v-else class="w-4 h-4 text-muted shrink-0 mt-0.5 group-hover:text-dim" />

          <div class="flex-1 min-w-0 leading-snug">
            <span
              class="break-words"
              :class="sub.isCompleted ? 'line-through text-muted' : 'text-fg'"
            >
              {{ isExpanded(sub.id) || sub.title.length <= 60 ? sub.title : sub.title.slice(0, 60) + '...' }}
            </span>
            <button
              v-if="sub.title.length > 60"
              type="button"
              class="inline-block text-xs font-medium text-accent hover:underline ml-1 cursor-pointer select-none"
              @click.stop="toggleExpand(sub.id)"
            >
              {{ isExpanded(sub.id) ? 'ler menos' : 'ler mais...' }}
            </button>
          </div>

          <span
            class="text-xs font-medium shrink-0 ml-auto px-1.5 py-0.5 rounded mt-0.5"
            :class="sub.isCompleted ? 'text-muted/60 bg-panel-2' : 'text-accent/80 bg-accent/10'"
          >
            +{{ fractionXp }} XP
          </span>
        </button>

        <button
          type="button"
          class="opacity-0 group-hover:opacity-100 text-muted hover:text-[#e9a0a7] p-1 transition cursor-pointer shrink-0 mt-0.5"
          title="Excluir subtarefa"
          @click="handleDelete(sub.id)"
        >
          <Trash2 class="w-3 h-3" />
        </button>
      </div>
    </div>

    <!-- Adicionar nova subtarefa -->
    <form @submit.prevent="handleAdd" class="flex items-center gap-2 pt-1">
      <input
        v-model="newTitle"
        type="text"
        placeholder="Adicionar subtarefa..."
        class="flex-1 px-2.5 py-1.5 rounded bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
      />
      <button
        type="submit"
        :disabled="loading || !newTitle.trim()"
        class="p-1.5 rounded bg-panel-2 border border-line text-dim hover:text-fg hover:bg-hover disabled:opacity-50 cursor-pointer"
      >
        <Plus class="w-3.5 h-3.5" />
      </button>
    </form>
  </div>
</template>
