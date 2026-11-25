<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import type { TaskItem, TaskStatus } from '@/types'
import { useTasksStore } from '@/stores/tasks'
import { useToast } from '@/composables/useToast'
import KanbanColumn from './KanbanColumn.vue'

const props = defineProps<{
  tasks: TaskItem[]
  spaceId?: string
}>()

const tasksStore = useTasksStore()
const { error: toastError } = useToast()

const pendingTasks = ref<TaskItem[]>([])
const inProgressTasks = ref<TaskItem[]>([])
const doneTasks = ref<TaskItem[]>([])

// Sincroniza tarefas locais com a lista de props
function syncColumns() {
  const sorted = [...props.tasks].sort((a, b) => a.position - b.position)
  pendingTasks.value = sorted.filter((t) => t.status === 'PENDING')
  inProgressTasks.value = sorted.filter((t) => t.status === 'IN_PROGRESS')
  doneTasks.value = sorted.filter((t) => t.status === 'DONE')
}

watch(
  () => props.tasks,
  () => {
    syncColumns()
  },
  { immediate: true, deep: true }
)

function calculateNewPosition(list: TaskItem[], targetIndex: number): number {
  if (list.length === 0 || targetIndex < 0) return 1000

  if (targetIndex === 0) {
    const next = list[0]
    return next && next.position > 1 ? Math.floor(next.position / 2) : 500
  }

  if (targetIndex >= list.length - 1) {
    const prev = list[list.length - 1]
    return prev ? prev.position + 1000 : 1000
  }

  const prev = list[targetIndex - 1]
  const next = list[targetIndex + 1]
  if (prev && next) {
    return Math.floor((prev.position + next.position) / 2)
  }

  return 1000
}

async function handleMove(event: { taskId: string; targetStatus: TaskStatus; newIndex: number }) {
  const { taskId, targetStatus, newIndex } = event

  let targetList: TaskItem[] = []
  if (targetStatus === 'PENDING') targetList = pendingTasks.value
  else if (targetStatus === 'IN_PROGRESS') targetList = inProgressTasks.value
  else if (targetStatus === 'DONE') targetList = doneTasks.value

  const newPosition = calculateNewPosition(targetList, newIndex)

  try {
    await tasksStore.moveTask(taskId, {
      status: targetStatus,
      position: newPosition,
    })
  } catch (err: any) {
    syncColumns()
    toastError(err.message || 'Erro ao mover tarefa')
  }
}
</script>

<template>
  <div class="h-full flex gap-4 overflow-x-auto pb-4 pt-1 items-start">
    <KanbanColumn
      v-model:tasks="pendingTasks"
      title="Pendente"
      status="PENDING"
      color="bg-[#edc887]"
      :space-id="spaceId"
      @move="handleMove"
    />

    <KanbanColumn
      v-model:tasks="inProgressTasks"
      title="Em Andamento"
      status="IN_PROGRESS"
      color="bg-[#a3bee9]"
      :space-id="spaceId"
      @move="handleMove"
    />

    <KanbanColumn
      v-model:tasks="doneTasks"
      title="Concluído"
      status="DONE"
      color="bg-[#9cd9b7]"
      :space-id="spaceId"
      @move="handleMove"
    />
  </div>
</template>
