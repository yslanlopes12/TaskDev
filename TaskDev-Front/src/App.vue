<script setup lang="ts">
import { watch } from 'vue'
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'
import { useSpacesStore } from '@/stores/spaces'
import { useGamificationStore } from '@/stores/gamification'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppTopbar from '@/components/layout/AppTopbar.vue'
import AiChatPanel from '@/components/layout/AiChatPanel.vue'
import AppToast from '@/components/ui/AppToast.vue'
import XpToast from '@/components/gamification/XpToast.vue'
import RankModal from '@/components/gamification/RankModal.vue'
import SpaceModal from '@/components/spaces/SpaceModal.vue'
import TaskDetailDrawer from '@/components/tasks/TaskDetailDrawer.vue'

const authStore = useAuthStore()
const uiStore = useUiStore()
const spacesStore = useSpacesStore()
const gamificationStore = useGamificationStore()

watch(
  () => authStore.isAuthenticated,
  async (isAuth) => {
    if (isAuth) {
      await Promise.all([
        spacesStore.loadSpaces(),
        gamificationStore.loadProgress(),
      ])
    }
  },
  { immediate: true }
)
</script>

<template>
  <div class="min-h-screen bg-bg text-fg font-sans antialiased overflow-hidden">
    <!-- Quando NÃO autenticado (ex: Login, Register) -->
    <template v-if="!authStore.isAuthenticated">
      <main class="min-h-screen flex items-center justify-center p-4">
        <RouterView />
      </main>
    </template>

    <!-- Quando AUTENTICADO: Grid de 3 colunas (Sidebar | Main | Chat) -->
    <template v-else>
      <div class="h-screen w-screen flex overflow-hidden">
        <!-- Sidebar esquerda -->
        <AppSidebar />

        <!-- Coluna central (Topbar + Content) -->
        <div class="flex-1 flex flex-col min-w-0 h-full overflow-hidden">
          <AppTopbar />
          <main class="flex-1 overflow-y-auto bg-bg p-4 md:p-6">
            <RouterView />
          </main>
        </div>

        <!-- Painel de Chat IA à direita -->
        <AiChatPanel />

        <!-- Scrim para mobile quando sidebar ou chat abertos em telas menores -->
        <div
          v-if="uiStore.sidebarOpen"
          class="fixed inset-0 bg-black/60 z-30 md:hidden"
          @click="uiStore.setSidebarOpen(false)"
        />
        <div
          v-if="uiStore.chatOpen"
          class="fixed inset-0 bg-black/60 z-30 lg:hidden"
          @click="uiStore.setChatOpen(false)"
        />
      </div>
    </template>

    <!-- Componentes Globais Flutuantes -->
    <AppToast />
    <XpToast />
    <RankModal />
    <SpaceModal />
    <TaskDetailDrawer />
  </div>
</template>
