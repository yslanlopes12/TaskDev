<script setup lang="ts">
import { computed } from 'vue'
import type { TaskItem } from '@/types'
import { VueDraggable } from 'vue-draggable-plus'
import TaskRow from './TaskRow.vue'
import { useTasksStore } from '@/stores/tasks'
import { useToast } from '@/composables/useToast'
import { AlertCircle, CheckCircle2 } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    tasks: TaskItem[]
    groupBy?: 'status' | 'timeline' | 'none'
    allowDrag?: boolean
  }>(),
  {
    groupBy: 'none',
    allowDrag: true,
  }
)

const emit = defineEmits<{
  (e: 'update:tasks', value: TaskItem[]): void
}>()

const tasksStore = useTasksStore()
const { error: toastError } = useToast()

const localTasks = computed({
  get: () => props.tasks,
  set: (val) => emit('update:tasks', val),
})

// Agrupamento por status
const groupedByStatus = computed(() => [
  {
    title: 'Pendentes',
    tasks: props.tasks.filter((t) => t.status === 'PENDING'),
    color: 'bg-[#edc887]',
  },
  {
    title: 'Em Andamento',
    tasks: props.tasks.filter((t) => t.status === 'IN_PROGRESS'),
    color: 'bg-[#a3bee9]',
  },
  {
    title: 'Concluídas',
    tasks: props.tasks.filter((t) => t.status === 'DONE'),
    color: 'bg-[#9cd9b7]',
  },
])

// Agrupamento por timeline (Hoje, Pendentes, Planejadas, Concluídas)
const groupedByTimeline = computed(() => {
  const today = new Date().toISOString().slice(0, 10)

  const done = props.tasks.filter((t) => t.status === 'DONE')
  const notDone = props.tasks.filter((t) => t.status !== 'DONE')

  const todayTasks = notDone.filter((t) => t.dueDate?.slice(0, 10) === today)
  const pendingTasks = notDone.filter((t) => !t.dueDate || t.dueDate.slice(0, 10) < today)
  const plannedTasks = notDone.filter((t) => t.dueDate && t.dueDate.slice(0, 10) > today)

  return [
    { title: 'Hoje', tasks: todayTasks, color: 'bg-accent' },
    { title: 'Pendentes / Atrasadas', tasks: pendingTasks, color: 'bg-[#edc887]' },
    { title: 'Planejadas', tasks: plannedTasks, color: 'bg-[#a3bee9]' },
    { title: 'Concluídas', tasks: done, color: 'bg-[#9cd9b7]' },
  ]
})

async function onUpdate(event: any) {
  const item = event.data as TaskItem | undefined
  const taskId = item ? item.id : event.item?.dataset?.id
  if (!taskId) return

  const list = localTasks.value
  const targetIndex = event.newIndex
  let newPosition = 1000

  if (targetIndex === 0) {
    const next = list[1]
    newPosition = next ? Math.floor(next.position / 2) : 500
  } else if (targetIndex >= list.length - 1) {
    const prev = list[list.length - 2]
    newPosition = prev ? prev.position + 1000 : 1000
  } else {
    const prev = list[targetIndex - 1]
    const next = list[targetIndex + 1]
    if (prev && next) {
      newPosition = Math.floor((prev.position + next.position) / 2)
    }
  }

  const currentTask = list.find((t) => t.id === taskId)
  if (!currentTask) return

  try {
    await tasksStore.moveTask(taskId, {
      status: currentTask.status,
      position: newPosition,
    })
  } catch (err: any) {
    toastError(err.message || 'Erro ao reordenar tarefa')
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Aviso se ordenação temporária estiver ativa impedindo drag -->
    <div
      v-if="!allowDrag"
      class="flex items-center gap-2 p-2.5 rounded-lg bg-panel-2 border border-line text-xs text-muted"
    >
      <AlertCircle class="w-4 h-4 text-[#edc887] shrink-0" />
      <span>Reordenação manual desabilitada enquanto filtros ou ordenações temporárias estiverem ativos.</span>
    </div>

    <!-- Lista Simples (sem agrupamento) com Drag and Drop -->
    <div v-if="groupBy === 'none'" class="space-y-2">
      <VueDraggable
        v-if="allowDrag"
        v-model="localTasks"
        handle=".drag-handle"
        item-key="id"
        ghost-class="kanban-ghost"
        drag-class="kanban-drag"
        class="space-y-2"
        @update="onUpdate"
      >
        <div v-for="task in localTasks" :key="task.id" :data-id="task.id">
          <TaskRow :task="task" :can-drag="allowDrag" />
        </div>
      </VueDraggable>

      <div v-else class="space-y-2">
        <TaskRow
          v-for="task in tasks"
          :key="task.id"
          :task="task"
          :can-drag="false"
        />
      </div>

      <div
        v-if="tasks.length === 0"
        class="p-8 text-center border border-dashed border-line rounded-xl text-muted text-xs"
      >
        <CheckCircle2 class="w-8 h-8 mx-auto mb-2 opacity-30" />
        Nenhuma tarefa encontrada neste filtro.
      </div>
    </div>

    <!-- Agrupado por Status -->
    <div v-else-if="groupBy === 'status'" class="space-y-6">
      <div v-for="group in groupedByStatus" :key="group.title" class="space-y-2">
        <div class="flex items-center gap-2 px-1">
          <span class="w-2 h-2 rounded-full" :class="group.color" />
          <h3 class="text-xs font-semibold text-fg tracking-wide uppercase">{{ group.title }}</h3>
          <span class="text-[11px] text-muted font-bold">({{ group.tasks.length }})</span>
        </div>

        <div v-if="group.tasks.length > 0" class="space-y-2">
          <TaskRow
            v-for="task in group.tasks"
            :key="task.id"
            :task="task"
            :can-drag="false"
          />
        </div>
        <div v-else class="text-xs text-muted/60 italic px-3 py-1">
          Nenhuma tarefa neste status.
        </div>
      </div>
    </div>

    <!-- Agrupado por Timeline -->
    <div v-else-if="groupBy === 'timeline'" class="space-y-6">
      <div v-for="group in groupedByTimeline" :key="group.title" class="space-y-2">
        <div class="flex items-center gap-2 px-1">
          <span class="w-2 h-2 rounded-full" :class="group.color" />
          <h3 class="text-xs font-semibold text-fg tracking-wide uppercase">{{ group.title }}</h3>
          <span class="text-[11px] text-muted font-bold">({{ group.tasks.length }})</span>
        </div>

        <div v-if="group.tasks.length > 0" class="space-y-2">
          <TaskRow
            v-for="task in group.tasks"
            :key="task.id"
            :task="task"
            :can-drag="false"
          />
        </div>
        <div v-else class="text-xs text-muted/60 italic px-3 py-1">
          Nenhuma tarefa para este período.
        </div>
      </div>
    </div>
  </div>
</template>
