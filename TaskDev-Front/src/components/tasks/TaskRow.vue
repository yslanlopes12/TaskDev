<script setup lang="ts">
import { ref, computed } from 'vue'
import type { TaskItem } from '@/types'
import { useTasksStore } from '@/stores/tasks'
import { useUiStore } from '@/stores/ui'
import PriorityDot from '@/components/ui/PriorityDot.vue'
import StatusTag from '@/components/ui/StatusTag.vue'
import SubtaskList from '@/components/tasks/SubtaskList.vue'
import {
  Check,
  Calendar,
  CheckSquare,
  ChevronRight,
  ChevronDown,
  GripVertical,
  Folder,
} from 'lucide-vue-next'

const props = defineProps<{
  task: TaskItem
  canDrag?: boolean
}>()

const tasksStore = useTasksStore()
const uiStore = useUiStore()

const isExpanded = ref(false)
const completing = ref(false)

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

async function handleComplete() {
  completing.value = true
  try {
    if (isDone.value) {
      await tasksStore.uncompleteTask(props.task.id)
    } else {
      await tasksStore.completeTask(props.task.id)
    }
  } finally {
    completing.value = false
  }
}

function openDetail() {
  uiStore.openTaskDrawer(props.task.id)
}
</script>

<template>
  <div class="rounded-lg border border-line bg-[#141518] hover:border-line-soft transition overflow-hidden">
    <!-- Main row bar -->
    <div
      class="flex items-center gap-2.5 px-3 py-2.5 cursor-pointer select-none"
      @click="openDetail"
    >
      <!-- Drag handle -->
      <button
        v-if="canDrag"
        type="button"
        class="drag-handle text-muted hover:text-dim p-0.5 cursor-grab active:cursor-grabbing shrink-0"
        title="Arrastar para reordenar"
        @click.stop
      >
        <GripVertical class="w-4 h-4" />
      </button>

      <!-- Complete Checkbox -->
      <button
        type="button"
        class="w-4 h-4 rounded border transition flex items-center justify-center shrink-0 cursor-pointer"
        :class="
          isDone
            ? 'bg-accent border-accent text-[#101114]'
            : 'border-muted hover:border-accent text-transparent'
        "
        :disabled="completing"
        @click.stop="handleComplete"
      >
        <Check class="w-3 h-3 stroke-[3]" />
      </button>

      <!-- Task Title (click to open detail) -->
      <div class="flex-1 min-w-0">
        <span
          class="text-sm font-medium block truncate"
          :class="isDone ? 'line-through text-muted' : 'text-fg'"
        >
          {{ task.title }}
        </span>
      </div>

      <!-- Metadados -->
      <div class="flex items-center gap-2.5 shrink-0">
        <!-- Espaço Tag (se disponível) -->
        <span
          v-if="task.space"
          class="hidden sm:inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs bg-panel-2 text-dim border border-line"
        >
          <Folder class="w-3.5 h-3.5" :style="{ color: task.space.color || '#a69aeb' }" />
          <span class="max-w-[100px] truncate">{{ task.space.name }}</span>
        </span>

        <!-- Prazo -->
        <span
          v-if="formattedDueDate"
          class="hidden md:inline-flex items-center gap-1.5 text-xs text-muted"
        >
          <Calendar class="w-3.5 h-3.5 text-muted" />
          {{ formattedDueDate }}
        </span>

        <!-- Subtarefas indicator -->
        <span
          v-if="subtasksCount.total > 0"
          class="inline-flex items-center gap-1.5 text-xs text-muted"
        >
          <CheckSquare class="w-3.5 h-3.5" />
          {{ subtasksCount.completed }}/{{ subtasksCount.total }}
        </span>

        <!-- Priority Dot -->
        <PriorityDot :priority="task.priority" />

        <!-- Status Tag -->
        <StatusTag :status="task.status" />

        <!-- Expand button -->
        <button
          type="button"
          class="text-muted hover:text-fg p-1 rounded hover:bg-hover transition cursor-pointer"
          @click.stop="isExpanded = !isExpanded"
        >
          <ChevronDown v-if="isExpanded" class="w-3.5 h-3.5" />
          <ChevronRight v-else class="w-3.5 h-3.5" />
        </button>
      </div>
    </div>

    <!-- Expanded content -->
    <div
      v-if="isExpanded"
      class="px-4 py-3 bg-[#101114] border-t border-line space-y-3"
    >
      <div v-if="task.description" class="text-xs text-dim whitespace-pre-wrap leading-relaxed">
        {{ task.description }}
      </div>

      <div class="pt-1">
        <div class="text-xs font-semibold text-muted uppercase tracking-wider mb-1.5">
          Subtarefas
        </div>
        <SubtaskList
          :task-id="task.id"
          :subtasks="task.subtasks"
          :priority="task.priority"
          @update="tasksStore.loadTasks(task.spaceId)"
        />
      </div>
    </div>
  </div>
</template>
