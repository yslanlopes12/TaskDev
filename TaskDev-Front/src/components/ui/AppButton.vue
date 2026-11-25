<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
    size?: 'sm' | 'md' | 'lg'
    loading?: boolean
    disabled?: boolean
    type?: 'button' | 'submit' | 'reset'
  }>(),
  {
    variant: 'secondary',
    size: 'md',
    loading: false,
    disabled: false,
    type: 'button',
  }
)
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="inline-flex items-center justify-center font-medium transition cursor-pointer select-none rounded disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none"
    :class="[
      // Sizes
      {
        'px-2.5 py-1 text-xs gap-1.5': size === 'sm',
        'px-3.5 py-1.5 text-xs gap-2': size === 'md',
        'px-4 py-2 text-sm gap-2.5': size === 'lg',
      },
      // Variants
      {
        'bg-accent text-[#101114] hover:bg-[#b8acf2] active:bg-[#9789e0] font-semibold shadow-sm': variant === 'primary',
        'bg-panel-2 border border-line text-fg hover:bg-hover active:bg-panel': variant === 'secondary',
        'bg-[#e9a0a7]/15 border border-[#e9a0a7]/30 text-[#e9a0a7] hover:bg-[#e9a0a7]/25': variant === 'danger',
        'bg-transparent text-dim hover:text-fg hover:bg-hover': variant === 'ghost',
      },
    ]"
  >
    <svg
      v-if="loading"
      class="animate-spin -ml-0.5 mr-1.5 h-3.5 w-3.5 text-current"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
    </svg>
    <slot />
  </button>
</template>
