<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useUiStore } from '@/stores/ui'
import { useSpacesStore } from '@/stores/spaces'
import { useAuthStore } from '@/stores/auth'
import { useToast } from '@/composables/useToast'
import AppModal from '@/components/ui/AppModal.vue'
import AppButton from '@/components/ui/AppButton.vue'
import {
  Folder,
  Code,
  Briefcase,
  GraduationCap,
  BookOpen,
  Layers,
  Target,
  Flag,
  Trash2,
} from 'lucide-vue-next'
import type { SpaceVisibility } from '@/types'

const router = useRouter()
const uiStore = useUiStore()
const spacesStore = useSpacesStore()
const authStore = useAuthStore()
const { success, error } = useToast()

const name = ref('')
const selectedIcon = ref('Folder')
const selectedColor = ref('#a69aeb')
const visibility = ref<SpaceVisibility>('PRIVATE')
const loading = ref(false)
const showDeleteConfirm = ref(false)

const iconOptions = [
  { name: 'Folder', component: Folder },
  { name: 'Code', component: Code },
  { name: 'Briefcase', component: Briefcase },
  { name: 'GraduationCap', component: GraduationCap },
  { name: 'BookOpen', component: BookOpen },
  { name: 'Layers', component: Layers },
  { name: 'Target', component: Target },
  { name: 'Flag', component: Flag },
]

const colorOptions = ['#a69aeb', '#9cd9b7', '#edc887', '#a3bee9', '#e9a0a7']

const isEditing = computed(() => uiStore.editingSpaceId !== null)
const editingSpace = computed(() => {
  if (!uiStore.editingSpaceId) return null
  return spacesStore.getSpaceById(uiStore.editingSpaceId) || spacesStore.currentSpace
})

const isOwner = computed(() => {
  if (!editingSpace.value) return true
  return editingSpace.value.ownerUserId === authStore.user?.id || authStore.isAdmin
})

watch(
  () => uiStore.spaceModalOpen,
  (open) => {
    if (open) {
      showDeleteConfirm.value = false
      if (editingSpace.value) {
        name.value = editingSpace.value.name
        selectedIcon.value = editingSpace.value.icon || 'Folder'
        selectedColor.value = editingSpace.value.color || '#a69aeb'
        visibility.value = editingSpace.value.visibility || 'PRIVATE'
      } else {
        name.value = ''
        selectedIcon.value = 'Folder'
        selectedColor.value = '#a69aeb'
        visibility.value = 'PRIVATE'
      }
    }
  }
)

async function handleSubmit() {
  if (!name.value.trim()) {
    error('O nome do espaço é obrigatório')
    return
  }

  loading.value = true
  try {
    if (isEditing.value && uiStore.editingSpaceId) {
      await spacesStore.updateSpace(uiStore.editingSpaceId, {
        name: name.value.trim(),
        icon: selectedIcon.value,
        color: selectedColor.value,
        visibility: visibility.value,
      })
      success('Espaço atualizado com sucesso!')
    } else {
      const created = await spacesStore.createSpace({
        name: name.value.trim(),
        icon: selectedIcon.value,
        color: selectedColor.value,
        visibility: visibility.value,
      })
      success('Espaço criado com sucesso!')
      router.push(`/spaces/${created.id}`)
    }
    uiStore.closeSpaceModal()
  } catch (err: any) {
    error(err.message || 'Erro ao salvar espaço')
  } finally {
    loading.value = false
  }
}

async function handleDelete() {
  if (!uiStore.editingSpaceId) return

  loading.value = true
  try {
    await spacesStore.deleteSpace(uiStore.editingSpaceId)
    success('Espaço removido com sucesso')
    uiStore.closeSpaceModal()
    router.push('/')
  } catch (err: any) {
    error(err.message || 'Erro ao remover espaço')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AppModal
    :show="uiStore.spaceModalOpen"
    :title="isEditing ? 'Editar Espaço' : 'Criar Novo Espaço'"
    max-width="md"
    @close="uiStore.closeSpaceModal"
  >
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <!-- Nome -->
      <div>
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
          Nome do Espaço
        </label>
        <input
          v-model="name"
          type="text"
          placeholder="Ex: Backend Core, Mobile App..."
          required
          class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-sm"
        />
      </div>

      <!-- Ícone -->
      <div>
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
          Ícone
        </label>
        <div class="grid grid-cols-4 gap-2">
          <button
            v-for="item in iconOptions"
            :key="item.name"
            type="button"
            class="p-2.5 rounded-lg border flex items-center justify-center transition cursor-pointer"
            :class="
              selectedIcon === item.name
                ? 'bg-accent/20 border-accent text-accent'
                : 'bg-panel-2 border-line text-muted hover:text-fg hover:border-line-soft'
            "
            @click="selectedIcon = item.name"
          >
            <component :is="item.component" class="w-5 h-5" />
          </button>
        </div>
      </div>

      <!-- Cor -->
      <div>
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
          Cor
        </label>
        <div class="flex items-center gap-3">
          <button
            v-for="c in colorOptions"
            :key="c"
            type="button"
            class="w-8 h-8 rounded-full border-2 transition transform cursor-pointer flex items-center justify-center"
            :class="selectedColor === c ? 'scale-110 border-white' : 'border-transparent opacity-75 hover:opacity-100'"
            :style="{ backgroundColor: c }"
            @click="selectedColor = c"
          />
        </div>
      </div>

      <!-- Visibilidade -->
      <div>
        <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1.5">
          Visibilidade
        </label>
        <div class="grid grid-cols-2 gap-2">
          <button
            type="button"
            class="p-3 rounded-lg border text-left transition cursor-pointer"
            :class="
              visibility === 'PRIVATE'
                ? 'bg-accent/15 border-accent text-fg'
                : 'bg-panel-2 border-line text-dim hover:text-fg'
            "
            @click="visibility = 'PRIVATE'"
          >
            <span class="block text-xs font-semibold">Pessoal</span>
            <span class="block text-[11px] text-muted mt-0.5">Apenas você terá acesso</span>
          </button>

          <button
            type="button"
            class="p-3 rounded-lg border text-left transition cursor-pointer"
            :class="
              visibility === 'SHARED'
                ? 'bg-accent/15 border-accent text-fg'
                : 'bg-panel-2 border-line text-dim hover:text-fg'
            "
            @click="visibility = 'SHARED'"
          >
            <span class="block text-xs font-semibold">Compartilhado</span>
            <span class="block text-[11px] text-muted mt-0.5">Colabore com outros membros</span>
          </button>
        </div>
      </div>

      <!-- Confirmação de Exclusão -->
      <div v-if="isEditing && isOwner" class="pt-3 border-t border-line">
        <div v-if="!showDeleteConfirm" class="flex justify-between items-center">
          <span class="text-xs text-muted">Zona de perigo</span>
          <AppButton variant="danger" size="sm" @click="showDeleteConfirm = true">
            <Trash2 class="w-3.5 h-3.5 mr-1" />
            Excluir espaço
          </AppButton>
        </div>

        <div v-else class="p-3 rounded-lg bg-[#e9a0a7]/10 border border-[#e9a0a7]/30 space-y-2">
          <p class="text-xs text-[#e9a0a7]">Tem certeza? Todas as tarefas deste espaço serão excluídas permanentemente.</p>
          <div class="flex gap-2 justify-end">
            <AppButton variant="ghost" size="sm" @click="showDeleteConfirm = false">Cancelar</AppButton>
            <AppButton variant="danger" size="sm" :loading="loading" @click="handleDelete">Sim, excluir</AppButton>
          </div>
        </div>
      </div>

      <!-- Footer botões -->
      <div class="flex justify-end gap-2 pt-3">
        <AppButton variant="ghost" @click="uiStore.closeSpaceModal">Cancelar</AppButton>
        <AppButton variant="primary" type="submit" :loading="loading">
          {{ isEditing ? 'Salvar Alterações' : 'Criar Espaço' }}
        </AppButton>
      </div>
    </form>
  </AppModal>
</template>
