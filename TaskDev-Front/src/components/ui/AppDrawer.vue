<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import { X } from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    show: boolean
    title?: string
    width?: string
  }>(),
  {
    show: false,
    title: '',
    width: 'max-w-[480px]',
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
    <div>
      <!-- Backdrop -->
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
          class="fixed inset-0 z-40 bg-black/60 backdrop-blur-[2px]"
          @click.self="emit('close')"
        />
      </Transition>

      <!-- Panel -->
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="show"
          class="fixed inset-y-0 right-0 z-50 w-full bg-[#141518] border-l border-line shadow-2xl flex flex-col"
          :class="width"
          @click.stop
          @mousedown.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-5 py-3.5 border-b border-line bg-panel shrink-0">
            <slot name="header">
              <h3 class="text-xs font-semibold uppercase tracking-wider text-dim">{{ title }}</h3>
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
          <div class="flex-1 overflow-y-auto p-5">
            <slot />
          </div>

          <!-- Footer -->
          <div v-if="$slots.footer" class="p-4 border-t border-line bg-panel shrink-0 flex items-center justify-between gap-3">
            <slot name="footer" />
          </div>
        </div>
      </Transition>
    </div>
  </Teleport>
</template>
