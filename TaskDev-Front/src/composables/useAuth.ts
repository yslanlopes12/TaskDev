import { storeToRefs } from 'pinia'
import { useAuthStore } from '@/stores/auth'

export function useAuth() {
  const authStore = useAuthStore()
  const { user, initialized, isAuthenticated, isAdmin, isOperator } = storeToRefs(authStore)

  return {
    user,
    initialized,
    isAuthenticated,
    isAdmin,
    isOperator,
    initialize: authStore.initialize,
    login: authStore.login,
    logout: authStore.logout,
  }
}
