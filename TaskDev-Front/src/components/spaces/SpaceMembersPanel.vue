<script setup lang="ts">
import { ref } from 'vue'
import { useSpacesStore } from '@/stores/spaces'
import { useAuthStore } from '@/stores/auth'
import { useToast } from '@/composables/useToast'
import AppModal from '@/components/ui/AppModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import { UserPlus, UserX, Users } from 'lucide-vue-next'

const props = defineProps<{
  show: boolean
  spaceId: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const spacesStore = useSpacesStore()
const authStore = useAuthStore()
const { success, error } = useToast()

const newUsername = ref('')
const loading = ref(false)

async function handleAddMember() {
  if (!newUsername.value.trim()) return
  loading.value = true
  try {
    await spacesStore.addMember(props.spaceId, newUsername.value.trim())
    success(`Usuário ${newUsername.value} adicionado!`)
    newUsername.value = ''
  } catch (err: any) {
    error(err.message || 'Erro ao adicionar membro')
  } finally {
    loading.value = false
  }
}

async function handleRemoveMember(userId: string) {
  loading.value = true
  try {
    await spacesStore.removeMember(props.spaceId, userId)
    success('Membro removido do espaço')
  } catch (err: any) {
    error(err.message || 'Erro ao remover membro')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AppModal
    :show="show"
    title="Membros do Espaço"
    max-width="md"
    @close="emit('close')"
  >
    <div class="space-y-4">
      <!-- Input para adicionar -->
      <form @submit.prevent="handleAddMember" class="flex gap-2">
        <input
          v-model="newUsername"
          type="text"
          placeholder="Nome de usuário (@username)..."
          required
          class="flex-1 px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
        />
        <AppButton variant="primary" type="submit" size="sm" :loading="loading">
          <UserPlus class="w-3.5 h-3.5 mr-1" />
          Adicionar
        </AppButton>
      </form>

      <!-- Lista de membros -->
      <div class="space-y-1.5">
        <div class="text-xs font-semibold text-dim uppercase tracking-wider flex items-center gap-1.5">
          <Users class="w-3.5 h-3.5 text-accent" />
          Membros com acesso ({{ spacesStore.currentMembers.length }})
        </div>

        <div class="divide-y divide-line rounded-lg border border-line bg-panel-2 overflow-hidden">
          <div
            v-for="member in spacesStore.currentMembers"
            :key="member.userId"
            class="flex items-center justify-between p-3"
          >
            <div class="flex items-center gap-2.5">
              <div class="w-8 h-8 rounded-full bg-accent-bg border border-accent/20 flex items-center justify-center font-bold text-accent text-xs">
                {{ member.firstName?.charAt(0) || member.username?.charAt(0) || 'U' }}
              </div>
              <div>
                <p class="text-xs font-semibold text-fg">
                  {{ member.firstName }} {{ member.lastName }}
                </p>
                <p class="text-[11px] text-muted">@{{ member.username }}</p>
              </div>
            </div>

            <!-- Botão remover membro se for permitido -->
            <button
              v-if="member.userId !== authStore.user?.id"
              type="button"
              class="text-muted hover:text-[#e9a0a7] p-1 rounded-lg hover:bg-hover transition cursor-pointer"
              title="Remover membro"
              @click="handleRemoveMember(member.userId)"
            >
              <UserX class="w-4 h-4" />
            </button>
          </div>

          <div v-if="spacesStore.currentMembers.length === 0" class="p-4 text-center text-xs text-muted">
            Nenhum membro compartilhado além do proprietário.
          </div>
        </div>
      </div>
    </div>
  </AppModal>
</template>
