<script setup lang="ts">
import type { TaskPriority } from '@/types'

defineProps<{
  priority: TaskPriority
  showLabel?: boolean
}>()

const priorityLabels: Record<TaskPriority, string> = {
  LOW: 'Baixa',
  MEDIUM: 'Média',
  HIGH: 'Alta',
  URGENT: 'Urgente',
}
</script>

<template>
  <span class="inline-flex items-center gap-1.5" :title="priorityLabels[priority]">
    <span
      class="w-2 h-2 rounded-full shrink-0 shadow-xs"
      :class="{
        'bg-[#e9a0a7]': priority === 'URGENT',
        'bg-[#edc887]': priority === 'HIGH',
        'bg-[#a3bee9]': priority === 'MEDIUM',
        'bg-[#777982]': priority === 'LOW',
      }"
    />
    <span v-if="showLabel" class="text-xs text-dim">
      {{ priorityLabels[priority] }}
    </span>
  </span>
</template>
