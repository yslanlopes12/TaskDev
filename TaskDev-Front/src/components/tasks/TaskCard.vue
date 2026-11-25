<script setup lang="ts">
import { computed } from 'vue'
import type { TaskItem } from '@/types'
import { useUiStore } from '@/stores/ui'
import PriorityDot from '@/components/ui/PriorityDot.vue'
import { Calendar, CheckSquare, User, Folder } from 'lucide-vue-next'

const props = defineProps<{
  task: TaskItem
}>()

const uiStore = useUiStore()

const isDone = computed(() => props.task.status === 'DONE')

const subtasksCount = computed(() => {
  if (!props.task.subtasks) return { total: 0, completed: 0 }
  const total = props.task.subtasks.length
  const completed = props.task.subtasks.filter((s) => s.isCompleted).length
  return { total, completed }
})

const formattedDueDate = computed(() => {
  if (!props.task.dueDate) return null
  const d = new Date(props.task.dueDate)
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
})

function handleClick() {
  uiStore.openTaskDrawer(props.task.id)
}
</script>

<template>
  <div
    class="p-3 rounded-lg border border-line bg-[#141518] hover:border-line-soft hover:bg-[#191a1e] transition cursor-pointer shadow-sm select-none"
    @click="handleClick"
  >
    <div class="flex items-start justify-between gap-2 mb-2">
      <h4
        class="text-sm font-medium leading-snug break-words"
        :class="isDone ? 'line-through text-muted' : 'text-fg'"
      >
        {{ task.title }}
      </h4>
      <PriorityDot :priority="task.priority" class="shrink-0 mt-0.5" />
    </div>

    <!-- Space tag -->
    <div v-if="task.space" class="mb-2">
      <span class="inline-flex items-center gap-1.5 text-xs text-dim px-2 py-0.5 rounded bg-panel-2 border border-line">
        <Folder class="w-3 h-3" :style="{ color: task.space.color || '#a69aeb' }" />
        <span class="max-w-[120px] truncate">{{ task.space.name }}</span>
      </span>
    </div>

    <!-- Metadados de rodapé -->
    <div class="flex items-center justify-between text-xs text-muted pt-1.5 border-t border-line/50">
      <div class="flex items-center gap-2.5">
        <span v-if="formattedDueDate" class="flex items-center gap-1">
          <Calendar class="w-3.5 h-3.5" />
          {{ formattedDueDate }}
        </span>

        <span v-if="subtasksCount.total > 0" class="flex items-center gap-1">
          <CheckSquare class="w-3.5 h-3.5" />
          {{ subtasksCount.completed }}/{{ subtasksCount.total }}
        </span>
      </div>

      <div v-if="task.assigneeUsername" class="flex items-center gap-1 text-dim" :title="`Responsável: @${task.assigneeUsername}`">
        <User class="w-3.5 h-3.5 text-muted" />
        <span class="text-xs">@{{ task.assigneeUsername }}</span>
      </div>
    </div>
  </div>
</template>
