<script setup lang="ts">
withDefaults(
  defineProps<{
    value: number
    max?: number
    color?: string
    height?: string
  }>(),
  {
    max: 100,
    color: 'bg-accent',
    height: 'h-1.5',
  }
)
</script>

<template>
  <div class="w-full bg-[#202126] rounded-full overflow-hidden p-0.5" :class="height">
    <div
      class="h-full rounded-full transition-all duration-500 ease-[cubic-bezier(0.34,1.4,0.64,1)] relative"
      :class="color"
      :style="{ width: `${Math.min(100, Math.max(0, (value / max) * 100))}%` }"
    >
      <div class="absolute inset-0 bg-white/20 rounded-full animate-pulse opacity-50" />
    </div>
  </div>
</template>
