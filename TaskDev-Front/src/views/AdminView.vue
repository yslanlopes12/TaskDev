<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useToast } from '@/composables/useToast'
import { adminApi } from '@/api/admin'
import type { User, UserRole } from '@/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import {
  ShieldAlert,
  KeyRound,
  UserCheck,
  UserX,
  Copy,
  Check,
  AlertTriangle,
  UserPlus,
} from 'lucide-vue-next'

const authStore = useAuthStore()
const { success, error: toastError } = useToast()

const users = ref<User[]>([])
const loading = ref(false)

// Modal de nova senha gerada pelo reset
const resetModalOpen = ref(false)
const resetUserData = ref<{ username: string; newPassword: string } | null>(null)
const passwordCopied = ref(false)

// Bootstrap form (se nenhum usuário existir)
const showBootstrap = ref(false)
const bootstrapFirstName = ref('')
const bootstrapLastName = ref('')
const bootstrapLoading = ref(false)

onMounted(() => {
  loadUsers()
})

async function loadUsers() {
  loading.value = true
  try {
    users.value = await adminApi.getUsers()
    if (users.value.length === 0) {
      showBootstrap.value = true
    }
  } catch (err: any) {
    toastError(err.message || 'Erro ao carregar lista de usuários')
  } finally {
    loading.value = false
  }
}

async function handleResetPassword(user: User) {
  if (!confirm(`Deseja resetar a senha do usuário @${user.username}? Uma nova senha única será gerada.`)) return

  try {
    const res = await adminApi.resetPassword(user.id)
    resetUserData.value = {
      username: user.username,
      newPassword: res.newPassword,
    }
    resetModalOpen.value = true
    success('Senha resetada com sucesso!')
  } catch (err: any) {
    toastError(err.message || 'Erro ao resetar senha')
  }
}

async function handleRoleChange(user: User, newRole: string) {
  try {
    await adminApi.updateUserRole(user.id, newRole)
    user.role = newRole as UserRole
    success(`Função de @${user.username} alterada para ${newRole}`)
  } catch (err: any) {
    toastError(err.message || 'Erro ao alterar função')
  }
}

async function handleToggleStatus(user: User) {
  try {
    await adminApi.toggleUserStatus(user.id)
    user.isActive = !user.isActive
    success(`Usuário @${user.username} ${user.isActive ? 'ativado' : 'desativado'}`)
  } catch (err: any) {
    toastError(err.message || 'Erro ao alterar status')
  }
}

async function copyNewPassword() {
  if (!resetUserData.value) return
  await navigator.clipboard.writeText(resetUserData.value.newPassword)
  passwordCopied.value = true
  success('Senha copiada!')
  setTimeout(() => {
    passwordCopied.value = false
  }, 2500)
}

async function handleBootstrap() {
  if (!bootstrapFirstName.value.trim() || !bootstrapLastName.value.trim()) return
  bootstrapLoading.value = true
  try {
    const res = await adminApi.bootstrap({
      firstName: bootstrapFirstName.value.trim(),
      lastName: bootstrapLastName.value.trim(),
    })
    resetUserData.value = {
      username: res.username,
      newPassword: res.password,
    }
    resetModalOpen.value = true
    showBootstrap.value = false
    await loadUsers()
  } catch (err: any) {
    toastError(err.message || 'Erro ao criar primeiro administrador')
  } finally {
    bootstrapLoading.value = false
  }
}
</script>

<template>
  <div class="max-w-5xl mx-auto space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <div class="flex items-center gap-2">
          <ShieldAlert class="w-5 h-5 text-accent" />
          <h1 class="text-xl font-bold text-fg">Painel Administrativo</h1>
        </div>
        <p class="text-xs text-muted mt-0.5">Gerenciamento de acessos, papéis e segurança dos usuários</p>
      </div>

      <AppButton
        v-if="showBootstrap"
        variant="primary"
        size="sm"
        @click="showBootstrap = true"
      >
        <UserPlus class="w-4 h-4 mr-1" />
        Criar Primeiro Admin
      </AppButton>
    </div>

    <!-- Bootstrap Box (quando necessário) -->
    <div
      v-if="showBootstrap"
      class="p-6 rounded-2xl bg-panel-2 border border-accent/30 space-y-4"
    >
      <div>
        <h3 class="text-sm font-bold text-fg">Inicializar Administrador do Sistema</h3>
        <p class="text-xs text-muted">Nenhum administrador detectado. Crie a primeira conta administradora.</p>
      </div>

      <form @submit.prevent="handleBootstrap" class="grid grid-cols-1 sm:grid-cols-2 gap-3 max-w-lg">
        <input
          v-model="bootstrapFirstName"
          type="text"
          placeholder="Nome..."
          required
          class="px-3 py-2 rounded-lg bg-panel border border-line text-fg text-xs"
        />
        <input
          v-model="bootstrapLastName"
          type="text"
          placeholder="Sobrenome..."
          required
          class="px-3 py-2 rounded-lg bg-panel border border-line text-fg text-xs"
        />
        <div class="sm:col-span-2">
          <AppButton variant="primary" type="submit" size="sm" :loading="bootstrapLoading">
            Criar Administrador Raiz
          </AppButton>
        </div>
      </form>
    </div>

    <!-- Tabela de Usuários -->
    <div class="rounded-xl border border-line bg-[#141518] overflow-hidden shadow-sm">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-panel-2 border-b border-line text-muted uppercase tracking-wider text-[10px]">
            <tr>
              <th class="px-4 py-3 font-semibold">Usuário</th>
              <th class="px-4 py-3 font-semibold">Função</th>
              <th class="px-4 py-3 font-semibold">Status</th>
              <th class="px-4 py-3 font-semibold text-right">Ações</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-line">
            <tr
              v-for="u in users"
              :key="u.id"
              class="hover:bg-hover/50 transition"
            >
              <!-- Nome e Username -->
              <td class="px-4 py-3">
                <div class="flex items-center gap-2.5">
                  <div class="w-7 h-7 rounded-full bg-panel-2 border border-line flex items-center justify-center font-bold text-xs text-dim">
                    {{ u.firstName.charAt(0) }}
                  </div>
                  <div>
                    <span class="font-medium text-fg block">{{ u.firstName }} {{ u.lastName }}</span>
                    <span class="text-[11px] text-muted font-mono">@{{ u.username }}</span>
                  </div>
                </div>
              </td>

              <!-- Role select ou badge -->
              <td class="px-4 py-3">
                <!-- Se for ADMIN pode alterar role (exceto a si próprio para evitar lockout) -->
                <select
                  v-if="authStore.isAdmin && u.id !== authStore.user?.id"
                  :value="u.role"
                  class="px-2 py-1 rounded bg-panel-2 border border-line text-fg text-[11px] font-semibold focus:outline-none focus:border-accent"
                  @change="handleRoleChange(u, ($event.target as HTMLSelectElement).value)"
                >
                  <option value="MEMBER">MEMBER</option>
                  <option value="OPERATOR">OPERATOR</option>
                  <option value="ADMIN">ADMIN</option>
                </select>
                <span
                  v-else
                  class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider"
                  :class="u.role === 'ADMIN' ? 'bg-accent/15 text-accent border border-accent/20' : 'bg-line text-dim'"
                >
                  {{ u.role }}
                </span>
              </td>

              <!-- Ativo / Inativo -->
              <td class="px-4 py-3">
                <span
                  class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-medium"
                  :class="u.isActive ? 'bg-[#9cd9b7]/10 text-[#9cd9b7]' : 'bg-[#e9a0a7]/10 text-[#e9a0a7]'"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="u.isActive ? 'bg-[#9cd9b7]' : 'bg-[#e9a0a7]'" />
                  {{ u.isActive ? 'Ativo' : 'Desativado' }}
                </span>
              </td>

              <!-- Ações -->
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <!-- Reset de senha (ADMIN pode em todos, OPERATOR só em MEMBER) -->
                  <button
                    v-if="authStore.isAdmin || (authStore.isOperator && u.role === 'MEMBER')"
                    type="button"
                    class="p-1.5 rounded text-dim hover:text-accent hover:bg-hover transition cursor-pointer"
                    title="Resetar senha"
                    @click="handleResetPassword(u)"
                  >
                    <KeyRound class="w-4 h-4" />
                  </button>

                  <!-- Toggle Ativar/Desativar (apenas ADMIN) -->
                  <button
                    v-if="authStore.isAdmin && u.id !== authStore.user?.id"
                    type="button"
                    class="p-1.5 rounded transition cursor-pointer"
                    :class="u.isActive ? 'text-dim hover:text-[#e9a0a7] hover:bg-hover' : 'text-dim hover:text-[#9cd9b7] hover:bg-hover'"
                    :title="u.isActive ? 'Desativar usuário' : 'Ativar usuário'"
                    @click="handleToggleStatus(u)"
                  >
                    <UserX v-if="u.isActive" class="w-4 h-4" />
                    <UserCheck v-else class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal de Exibição Única da Nova Senha -->
    <AppModal
      :show="resetModalOpen"
      title="Nova Senha Gerada"
      max-width="sm"
      @close="resetModalOpen = false"
    >
      <div v-if="resetUserData" class="space-y-4">
        <div class="p-3 rounded-lg bg-[#edc887]/15 border border-[#edc887]/30 text-xs text-dim flex items-start gap-2.5">
          <AlertTriangle class="w-4 h-4 text-[#edc887] shrink-0 mt-0.5" />
          <span>
            Copie e envie esta senha com segurança para <strong class="text-fg">@{{ resetUserData.username }}</strong>. Ela não será exibida novamente.
          </span>
        </div>

        <div>
          <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
            Nova Senha
          </label>
          <div class="relative">
            <input
              :value="resetUserData.newPassword"
              readonly
              class="w-full pl-3 pr-10 py-2 rounded-lg bg-panel-2 border border-line text-accent font-mono font-bold text-xs focus:outline-none select-all"
            />
            <button
              type="button"
              class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 rounded hover:bg-hover text-muted hover:text-fg transition cursor-pointer"
              title="Copiar senha"
              @click="copyNewPassword"
            >
              <Check v-if="passwordCopied" class="w-4 h-4 text-[#9cd9b7]" />
              <Copy v-else class="w-4 h-4" />
            </button>
          </div>
        </div>

        <div class="flex justify-end pt-2">
          <AppButton variant="primary" size="sm" @click="resetModalOpen = false">
            Concluído
          </AppButton>
        </div>
      </div>
    </AppModal>
  </div>
</template>
