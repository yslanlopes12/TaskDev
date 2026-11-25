<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useSpacesStore } from '@/stores/spaces'
import { useGamificationStore } from '@/stores/gamification'
import { useToast } from '@/composables/useToast'
import AppButton from '@/components/ui/AppButton.vue'
import { Lock, User, Eye, EyeOff } from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()
const spacesStore = useSpacesStore()
const gamificationStore = useGamificationStore()
const { error: toastError } = useToast()

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const errorMessage = ref('')

async function handleLogin() {
  if (!username.value || !password.value) {
    errorMessage.value = 'Preencha usuário e senha.'
    return
  }

  loading.value = true
  errorMessage.value = ''

  try {
    await authStore.login(username.value.trim(), password.value)
    await Promise.all([
      spacesStore.loadSpaces(),
      gamificationStore.loadProgress(),
    ])
    router.push('/')
  } catch (err: any) {
    errorMessage.value = err.message || 'Credenciais inválidas. Verifique seu usuário e senha.'
    toastError(errorMessage.value)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="w-full max-w-sm">
    <div class="p-8 rounded-2xl bg-[#191a1e] border border-line shadow-2xl space-y-6">
      <!-- Logo e Título -->
      <div class="text-center space-y-2">
        <div class="inline-flex w-12 h-12 rounded-xl bg-accent-bg border border-accent/30 items-center justify-center text-accent font-mono text-xl font-bold mb-1 shadow-inner">
          T/
        </div>
        <h1 class="text-xl font-bold tracking-tight text-fg">Task.dev</h1>
        <p class="text-xs text-muted">Acesse seu ambiente de trabalho</p>
      </div>

      <!-- Alerta de Erro -->
      <div
        v-if="errorMessage"
        class="p-3 rounded-lg bg-[#e9a0a7]/10 border border-[#e9a0a7]/30 text-xs text-[#e9a0a7]"
      >
        {{ errorMessage }}
      </div>

      <!-- Formulário de Login -->
      <form @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Usuário
          </label>
          <div class="relative">
            <User class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="username"
              type="text"
              autocomplete="username"
              placeholder="Digite seu usuário..."
              required
              class="w-full pl-9 pr-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
            />
          </div>
        </div>

        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Senha
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="current-password"
              placeholder="Digite sua senha..."
              required
              class="w-full pl-9 pr-10 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
            />
            <button
              type="button"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted hover:text-fg p-1 rounded focus:outline-none transition cursor-pointer"
              :title="showPassword ? 'Ocultar senha' : 'Ver senha'"
              @click="showPassword = !showPassword"
            >
              <EyeOff v-if="showPassword" class="w-4 h-4" />
              <Eye v-else class="w-4 h-4" />
            </button>
          </div>
        </div>

        <AppButton
          variant="primary"
          type="submit"
          class="w-full mt-2"
          :loading="loading"
        >
          Entrar
        </AppButton>
      </form>

      <!-- Rodapé do Card -->
      <div class="text-center pt-2 border-t border-line text-xs text-muted">
        Não possui uma conta?
        <router-link to="/register" class="text-accent hover:underline font-semibold ml-1">
          Cadastre-se
        </router-link>
      </div>
    </div>
  </div>
</template>
