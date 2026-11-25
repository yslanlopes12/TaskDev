<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'default' | 'accent' | 'success' | 'warning' | 'danger'
    size?: 'sm' | 'md'
  }>(),
  {
    variant: 'default',
    size: 'md',
  }
)
</script>

<template>
  <span
    class="inline-flex items-center font-medium rounded-full"
    :class="[
      size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-0.5 text-sm',
      {
        'bg-line text-dim': variant === 'default',
        'bg-accent-bg text-accent border border-accent/20': variant === 'accent',
        'bg-[#9cd9b7]/15 text-[#9cd9b7] border border-[#9cd9b7]/20': variant === 'success',
        'bg-[#edc887]/15 text-[#edc887] border border-[#edc887]/20': variant === 'warning',
        'bg-[#e9a0a7]/15 text-[#e9a0a7] border border-[#e9a0a7]/20': variant === 'danger',
      },
    ]"
  >
    <slot />
  </span>
</template>
