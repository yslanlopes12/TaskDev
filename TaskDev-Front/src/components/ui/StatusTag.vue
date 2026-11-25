<script setup lang="ts">
import type { TaskStatus } from '@/types'

defineProps<{
  status: TaskStatus
}>()

const statusLabels: Record<TaskStatus, string> = {
  PENDING: 'Pendente',
  IN_PROGRESS: 'Em andamento',
  DONE: 'Concluído',
}
</script>

<template>
  <span
    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border"
    :class="{
      'bg-[#edc887]/10 text-[#edc887] border-[#edc887]/30': status === 'PENDING',
      'bg-[#a3bee9]/10 text-[#a3bee9] border-[#a3bee9]/30': status === 'IN_PROGRESS',
      'bg-[#9cd9b7]/10 text-[#9cd9b7] border-[#9cd9b7]/30': status === 'DONE',
    }"
  >
    {{ statusLabels[status] }}
  </span>
</template>
