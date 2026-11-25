<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToast } from '@/composables/useToast'
import AppButton from '@/components/ui/AppButton.vue'
import { Copy, Check, AlertTriangle, ArrowRight, User, Lock, Eye, EyeOff } from 'lucide-vue-next'

const router = useRouter()
const authStore = useAuthStore()
const { success, error: toastError } = useToast()

const firstName = ref('')
const lastName = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const showResultPassword = ref(false)
const loading = ref(false)
const errorMessage = ref('')

// Dados gerados após cadastro
const generatedCredentials = ref<{
  username: string
  password: string
} | null>(null)

const copied = ref(false)

async function handleRegister() {
  if (!firstName.value.trim() || !lastName.value.trim()) {
    errorMessage.value = 'Preencha seu nome e sobrenome.'
    return
  }

  if (password.value) {
    if (password.value.length < 6) {
      errorMessage.value = 'A senha deve conter no mínimo 6 caracteres.'
      return
    }

    if (password.value !== confirmPassword.value) {
      errorMessage.value = 'As senhas digitadas não coincidem.'
      return
    }
  }

  loading.value = true
  errorMessage.value = ''

  try {
    const res = await authStore.register({
      firstName: firstName.value.trim(),
      lastName: lastName.value.trim(),
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      password: password.value ? password.value.trim() : undefined,
    })

    generatedCredentials.value = {
      username: res.username,
      password: res.password,
    }
    success('Conta criada com sucesso!')
  } catch (err: any) {
    errorMessage.value = err.message || 'Erro ao realizar cadastro.'
    toastError(errorMessage.value)
  } finally {
    loading.value = false
  }
}

async function copyCredentials() {
  if (!generatedCredentials.value) return
  const text = `Usuário: ${generatedCredentials.value.username}\nSenha: ${generatedCredentials.value.password}`
  await navigator.clipboard.writeText(text)
  copied.value = true
  success('Credenciais copiadas para a área de transferência!')
  setTimeout(() => {
    copied.value = false
  }, 2500)
}

function goToLogin() {
  router.push('/login')
}
</script>

<template>
  <div class="w-full max-w-sm">
    <div class="p-8 rounded-2xl bg-[#191a1e] border border-line shadow-2xl space-y-6">
      <!-- Header -->
      <div class="text-center space-y-2">
        <div class="inline-flex w-12 h-12 rounded-xl bg-accent-bg border border-accent/30 items-center justify-center text-accent font-mono text-xl font-bold mb-1 shadow-inner">
          T/
        </div>
        <h1 class="text-xl font-bold tracking-tight text-fg">Criar Conta</h1>
        <p class="text-xs text-muted">Junte-se ao Task.dev</p>
      </div>

      <!-- Alerta de Erro -->
      <div
        v-if="errorMessage"
        class="p-3 rounded-lg bg-[#e9a0a7]/10 border border-[#e9a0a7]/30 text-xs text-[#e9a0a7]"
      >
        {{ errorMessage }}
      </div>

      <!-- CASO 1: Formulário de cadastro -->
      <form v-if="!generatedCredentials" @submit.prevent="handleRegister" class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Nome
          </label>
          <div class="relative">
            <User class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="firstName"
              type="text"
              placeholder="Seu primeiro nome..."
              required
              class="w-full pl-9 pr-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
            />
          </div>
        </div>

        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Sobrenome
          </label>
          <div class="relative">
            <User class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="lastName"
              type="text"
              placeholder="Seu sobrenome..."
              required
              class="w-full pl-9 pr-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
            />
          </div>
        </div>

        <!-- Campo Senha -->
        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Senha
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              placeholder="Crie sua senha (mínimo 6 caracteres)..."
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
          <p class="text-[10px] text-muted mt-1">
            Opcional. Se deixar em branco, geraremos uma senha segura para você.
          </p>
        </div>

        <!-- Campo Confirmar Senha (exibido caso o usuário tenha preenchido a senha) -->
        <div v-if="password">
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
            Confirmar Senha
          </label>
          <div class="relative">
            <Lock class="w-4 h-4 text-muted absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              v-model="confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              autocomplete="new-password"
              placeholder="Repita sua senha..."
              class="w-full pl-9 pr-10 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
            />
            <button
              type="button"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted hover:text-fg p-1 rounded focus:outline-none transition cursor-pointer"
              :title="showConfirmPassword ? 'Ocultar senha' : 'Ver senha'"
              @click="showConfirmPassword = !showConfirmPassword"
            >
              <EyeOff v-if="showConfirmPassword" class="w-4 h-4" />
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
          Criar Conta
        </AppButton>
      </form>

      <!-- CASO 2: Exibição das credenciais geradas / salvas -->
      <div v-else class="space-y-4">
        <div class="p-3 rounded-lg bg-[#edc887]/15 border border-[#edc887]/30 text-xs text-dim flex items-start gap-2.5">
          <AlertTriangle class="w-4 h-4 text-[#edc887] shrink-0 mt-0.5" />
          <span>
            <strong class="text-fg font-semibold">Conta criada com sucesso!</strong>
            Guarde seu nome de usuário e sua senha para realizar o login.
          </span>
        </div>

        <!-- Username gerado -->
        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
            Seu Usuário
          </label>
          <input
            :value="generatedCredentials.username"
            readonly
            class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg font-mono text-xs focus:outline-none select-all"
          />
        </div>

        <!-- Senha -->
        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
            Sua Senha
          </label>
          <div class="relative">
            <input
              :value="generatedCredentials.password"
              :type="showResultPassword ? 'text' : 'password'"
              readonly
              class="w-full pl-3 pr-20 py-2 rounded-lg bg-panel-2 border border-line text-accent font-mono font-bold text-xs focus:outline-none select-all"
            />
            <div class="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
              <button
                type="button"
                class="p-1.5 rounded hover:bg-hover text-muted hover:text-fg transition cursor-pointer"
                :title="showResultPassword ? 'Ocultar senha' : 'Ver senha'"
                @click="showResultPassword = !showResultPassword"
              >
                <EyeOff v-if="showResultPassword" class="w-4 h-4" />
                <Eye v-else class="w-4 h-4" />
              </button>
              <button
                type="button"
                class="p-1.5 rounded hover:bg-hover text-muted hover:text-fg transition cursor-pointer"
                title="Copiar credenciais"
                @click="copyCredentials"
              >
                <Check v-if="copied" class="w-4 h-4 text-[#9cd9b7]" />
                <Copy v-else class="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        <!-- Botão Ir para o Login -->
        <AppButton
          variant="primary"
          class="w-full mt-4"
          @click="goToLogin"
        >
          Ir para o Login
          <ArrowRight class="w-4 h-4 ml-1" />
        </AppButton>
      </div>

      <!-- Rodapé do Card -->
      <div v-if="!generatedCredentials" class="text-center pt-2 border-t border-line text-xs text-muted">
        Já possui conta?
        <router-link to="/login" class="text-accent hover:underline font-semibold ml-1">
          Fazer login
        </router-link>
      </div>
    </div>
  </div>
</template>
