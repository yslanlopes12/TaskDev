<script setup lang="ts">
import { computed } from 'vue'
import type { TaskItem, TaskStatus } from '@/types'
import { VueDraggable } from 'vue-draggable-plus'
import TaskCard from './TaskCard.vue'
import { Plus } from 'lucide-vue-next'
import { useUiStore } from '@/stores/ui'

const props = defineProps<{
  title: string
  status: TaskStatus
  tasks: TaskItem[]
  color: string
  spaceId?: string
}>()

const emit = defineEmits<{
  (e: 'update:tasks', value: TaskItem[]): void
  (e: 'move', event: { taskId: string; targetStatus: TaskStatus; newIndex: number }): void
}>()

const uiStore = useUiStore()

const localTasks = computed({
  get: () => props.tasks,
  set: (val) => emit('update:tasks', val),
})

function onAdd(event: any) {
  const item = event.data as TaskItem | undefined
  const taskId = item ? item.id : event.item?.dataset?.id
  if (taskId) {
    emit('move', {
      taskId,
      targetStatus: props.status,
      newIndex: event.newIndex,
    })
  }
}

function onUpdate(event: any) {
  const item = event.data as TaskItem | undefined
  const taskId = item ? item.id : event.item?.dataset?.id
  if (taskId) {
    emit('move', {
      taskId,
      targetStatus: props.status,
      newIndex: event.newIndex,
    })
  }
}

function handleAddTask() {
  uiStore.openTaskDrawer(null, props.spaceId)
}
</script>

<template>
  <div class="flex flex-col bg-[#101114] rounded-xl border border-line p-3 w-80 shrink-0 max-h-full">
    <!-- Header da coluna -->
    <div class="flex items-center justify-between mb-3 px-1">
      <div class="flex items-center gap-2">
        <span class="w-2 h-2 rounded-full" :class="color" />
        <h3 class="text-xs font-semibold text-fg tracking-wide uppercase">{{ title }}</h3>
        <span class="px-1.5 py-0.2 rounded-full bg-panel-2 border border-line text-[10px] text-muted font-bold">
          {{ tasks.length }}
        </span>
      </div>

      <button
        type="button"
        class="text-muted hover:text-fg p-1 rounded hover:bg-hover transition cursor-pointer"
        title="Adicionar tarefa"
        @click="handleAddTask"
      >
        <Plus class="w-3.5 h-3.5" />
      </button>
    </div>

    <!-- Lista de cards Draggable -->
    <div class="flex-1 overflow-y-auto space-y-2 pr-1 min-h-[150px]">
      <VueDraggable
        v-model="localTasks"
        group="kanban-tasks"
        item-key="id"
        ghost-class="kanban-ghost"
        drag-class="kanban-drag"
        class="space-y-2 min-h-[150px]"
        @start="uiStore.setDraggingTask(true)"
        @end="uiStore.setDraggingTask(false)"
        @add="onAdd"
        @update="onUpdate"
      >
        <div
          v-for="task in localTasks"
          :key="task.id"
          :data-id="task.id"
        >
          <TaskCard :task="task" />
        </div>
      </VueDraggable>

      <div
        v-if="tasks.length === 0 && uiStore.isDraggingTask"
        class="h-28 border border-dashed border-accent/40 bg-accent/5 rounded-lg flex items-center justify-center text-xs text-accent transition-all animate-pulse pointer-events-none mt-2"
      >
        Solte uma tarefa aqui
      </div>
    </div>
  </div>
</template>
