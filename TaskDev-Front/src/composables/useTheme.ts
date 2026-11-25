import { storeToRefs } from 'pinia'
import { useUiStore } from '@/stores/ui'

export function useTheme() {
  const uiStore = useUiStore()
  const { theme } = storeToRefs(uiStore)

  return {
    theme,
    setTheme: uiStore.setTheme,
  }
}
