<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import { X } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    show: boolean
    title?: string
    maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl'
  }>(),
  {
    show: false,
    title: '',
    maxWidth: 'md',
  }
)

const emit = defineEmits<{
  (e: 'close'): void
}>()

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === 'Escape' && props.show) {
    emit('close')
  }
}

watch(
  () => props.show,
  (val) => {
    if (val) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = ''
    }
  }
)

onMounted(() => {
  window.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="show"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
        @click.self="emit('close')"
      >
        <div
          class="w-full bg-[#191a1e] border border-line rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh] transition-all transform"
          :class="{
            'max-w-sm': maxWidth === 'sm',
            'max-w-md': maxWidth === 'md',
            'max-w-lg': maxWidth === 'lg',
            'max-w-xl': maxWidth === 'xl',
            'max-w-2xl': maxWidth === '2xl',
          }"
        >
          <!-- Header -->
          <div v-if="title || $slots.header" class="flex items-center justify-between px-5 py-4 border-b border-line">
            <slot name="header">
              <h3 class="text-sm font-semibold text-fg tracking-wide">{{ title }}</h3>
            </slot>
            <button
              type="button"
              class="text-muted hover:text-fg p-1 rounded-lg hover:bg-hover transition cursor-pointer"
              @click="emit('close')"
            >
              <X class="w-4 h-4" />
            </button>
          </div>

          <!-- Body -->
          <div class="p-5 overflow-y-auto">
            <slot />
          </div>

          <!-- Footer (opcional) -->
          <div v-if="$slots.footer" class="px-5 py-3 border-t border-line bg-panel-2 flex items-center justify-end gap-2">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
