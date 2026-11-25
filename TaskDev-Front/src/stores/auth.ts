import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, RegisterResponse } from '@/types'
import { authApi } from '@/api/auth'
import { apiClient } from '@/api/client'
import { useGamificationStore } from './gamification'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const initialized = ref(false)
  const loading = ref(false)

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.role === 'ADMIN')
  const isOperator = computed(() => user.value?.role === 'OPERATOR' || user.value?.role === 'ADMIN')

  async function initialize() {
    try {
      const data = await authApi.me()
      user.value = data
      if (data && typeof (data as any).totalXp === 'number') {
        const gamificationStore = useGamificationStore()
        gamificationStore.totalXp = (data as any).totalXp
      }
    } catch {
      user.value = null
    } finally {
      initialized.value = true
    }
  }

  async function login(username: string, password: string) {
    loading.value = true
    try {
      const res = await authApi.login(username, password)
      if (res.csrfToken) {
        apiClient.setCsrfToken(res.csrfToken)
      }
      await initialize()
    } finally {
      loading.value = false
    }
  }

  async function register(data: { firstName: string; lastName: string; timezone?: string; password?: string }): Promise<RegisterResponse> {
    loading.value = true
    try {
      return await authApi.register(data)
    } finally {
      loading.value = false
    }
  }

  async function logout() {
    try {
      await authApi.logout()
    } finally {
      user.value = null
    }
  }

  return {
    user,
    initialized,
    loading,
    isAuthenticated,
    isAdmin,
    isOperator,
    initialize,
    login,
    register,
    logout,
  }
})
