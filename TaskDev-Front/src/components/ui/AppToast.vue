<script setup lang="ts">
import { useToast } from '@/composables/useToast'
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-vue-next'

const { toasts, remove } = useToast()
</script>

<template>
  <div class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex flex-col items-center gap-2 pointer-events-none w-full max-w-sm px-4">
    <TransitionGroup
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0 translate-y-4 scale-95"
      enter-to-class="opacity-100 translate-y-0 scale-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-95"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="pointer-events-auto flex items-center justify-between gap-3 px-4 py-2.5 rounded-lg border shadow-xl w-full text-xs font-medium backdrop-blur-md"
        :class="{
          'bg-[#191a1e]/95 border-[#9cd9b7]/30 text-[#9cd9b7]': toast.type === 'success',
          'bg-[#191a1e]/95 border-[#e9a0a7]/30 text-[#e9a0a7]': toast.type === 'error',
          'bg-[#191a1e]/95 border-[#a69aeb]/30 text-[#a69aeb]': toast.type === 'info',
        }"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <CheckCircle2 v-if="toast.type === 'success'" class="w-4 h-4 shrink-0 text-[#9cd9b7]" />
          <AlertCircle v-else-if="toast.type === 'error'" class="w-4 h-4 shrink-0 text-[#e9a0a7]" />
          <Info v-else class="w-4 h-4 shrink-0 text-[#a69aeb]" />
          <span class="truncate text-fg">{{ toast.message }}</span>
        </div>

        <button
          type="button"
          class="text-muted hover:text-fg p-0.5 rounded transition shrink-0 cursor-pointer"
          @click="remove(toast.id)"
        >
          <X class="w-3.5 h-3.5" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
