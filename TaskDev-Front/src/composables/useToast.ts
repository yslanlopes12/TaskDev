import { ref } from 'vue'
import type { ToastMessage } from '@/types'

const toasts = ref<ToastMessage[]>([])

export function showToast(message: string, type: ToastMessage['type'] = 'info', duration = 2500) {
  const id = Math.random().toString(36).substring(2, 9)
  const toast: ToastMessage = { id, message, type, duration }
  toasts.value.push(toast)

  if (duration > 0) {
    setTimeout(() => {
      removeToast(id)
    }, duration)
  }
}

export function removeToast(id: string) {
  toasts.value = toasts.value.filter((t) => t.id !== id)
}

export function useToast() {
  function show(message: string, type: ToastMessage['type'] = 'info', duration = 2500) {
    showToast(message, type, duration)
  }

  function remove(id: string) {
    removeToast(id)
  }

  function success(message: string, duration = 2500) {
    showToast(message, 'success', duration)
  }

  function error(message: string, duration = 3500) {
    showToast(message, 'error', duration)
  }

  function info(message: string, duration = 2500) {
    showToast(message, 'info', duration)
  }

  return {
    toasts,
    show,
    remove,
    success,
    error,
    info,
  }
}
