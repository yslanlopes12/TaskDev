<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useUiStore } from '@/stores/ui'
import { useSpacesStore } from '@/stores/spaces'
import AppButton from '@/components/ui/AppButton.vue'
import {
  MessageSquare,
  Plus,
  ChevronRight,
  Bot,
} from 'lucide-vue-next'

const route = useRoute()
const uiStore = useUiStore()
const spacesStore = useSpacesStore()

const currentSpace = computed(() => {
  if (route.params.id) {
    return spacesStore.getSpaceById(route.params.id as string) || spacesStore.currentSpace
  }
  return null
})

const breadcrumb = computed(() => {
  if (route.path === '/') {
    return { section: 'Workspace', page: 'Visão Geral' }
  }
  if (route.path.startsWith('/spaces')) {
    return { section: 'Espaços', page: currentSpace.value?.name || 'Espaço' }
  }
  if (route.path === '/settings') {
    return { section: 'Preferências', page: 'Configurações' }
  }
  if (route.path === '/admin') {
    return { section: 'Segurança', page: 'Administração' }
  }
  return { section: 'Task.dev', page: 'Navegação' }
})

function handleNewTask() {
  const defaultSpaceId = route.params.id as string | undefined
  uiStore.openTaskDrawer(null, defaultSpaceId)
}
</script>

<template>
  <header class="h-[57px] bg-sidebar border-b border-line px-4 flex items-center justify-between shrink-0 select-none z-30">
    <!-- Esquerda: Breadcrumbs -->
    <div class="flex items-center gap-3 min-w-0">
      <!-- Breadcrumbs -->
      <div class="flex items-center gap-1.5 text-xs text-muted truncate">
        <span>{{ breadcrumb.section }}</span>
        <ChevronRight class="w-3.5 h-3.5 shrink-0 opacity-60" />
        <span class="text-fg font-medium truncate">{{ breadcrumb.page }}</span>
      </div>
    </div>

    <!-- Direita: Ações Rápidas -->
    <div class="flex items-center gap-2.5 shrink-0">
      <!-- Botão Nova Tarefa -->
      <AppButton variant="primary" size="sm" @click="handleNewTask">
        <Plus class="w-3.5 h-3.5 mr-1" />
        <span class="hidden sm:inline">Nova tarefa</span>
      </AppButton>

      <!-- Divisor vertical -->
      <div class="h-4 w-[1px] bg-line" />

      <!-- Botão Toggle Chat IA -->
      <button
        type="button"
        class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium border transition cursor-pointer"
        :class="
          uiStore.chatOpen
            ? 'bg-accent/15 border-accent text-accent'
            : 'bg-panel-2 border-line text-dim hover:text-fg hover:bg-hover'
        "
        title="Assistente de Organização IA"
        @click="uiStore.toggleChat"
      >
        <Bot class="w-4 h-4" />
        <span class="hidden md:inline">Assistente IA</span>
      </button>
    </div>
  </header>
</template>
