<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'
import { useSpacesStore } from '@/stores/spaces'
import XpWidget from '@/components/gamification/XpWidget.vue'
import {
  LayoutDashboard,
  Settings,
  LogOut,
  Plus,
  Menu,
  Folder,
  Code,
  Briefcase,
  GraduationCap,
  BookOpen,
  Layers,
  Target,
  Flag,
  Shield,
} from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()
const spacesStore = useSpacesStore()

const iconMap: Record<string, any> = {
  Folder,
  Code,
  Briefcase,
  GraduationCap,
  BookOpen,
  Layers,
  Target,
  Flag,
}

function getSpaceIcon(name?: string) {
  if (name && iconMap[name]) return iconMap[name]
  return Folder
}

const isExpanded = computed(() => uiStore.sidebarOpen)

function handleNewSpace() {
  uiStore.openSpaceModal(null)
}

async function handleLogout() {
  await authStore.logout()
  router.push('/login')
}
</script>

<template>
  <aside
    class="h-full bg-sidebar border-r border-line flex flex-col justify-between transition-all duration-300 z-40 select-none overflow-hidden"
    :class="isExpanded ? 'w-[246px]' : 'w-[62px]'"
  >
    <!-- Topo: Header com Brand e Toggle (ícone de 3 traços) -->
    <div>
      <div class="h-[57px] flex items-center gap-2.5 border-b border-line px-3">
        <!-- Toggle Collapse Button: sempre à esquerda -->
        <button
          type="button"
          class="text-muted hover:text-fg p-1.5 rounded-lg hover:bg-hover transition cursor-pointer shrink-0 flex items-center justify-center"
          :title="isExpanded ? 'Recolher sidebar' : 'Expandir sidebar'"
          @click="uiStore.toggleSidebar"
        >
          <Menu class="w-5 h-5" />
        </button>

        <!-- Brand (apenas quando expandido) -->
        <router-link
          v-if="isExpanded"
          to="/"
          class="flex items-center gap-2 font-bold tracking-tight text-fg overflow-hidden min-w-0"
        >
          <div class="w-7 h-7 rounded-lg bg-accent-bg border border-accent/30 flex items-center justify-center text-accent font-mono text-xs shrink-0">
            T/
          </div>
          <span class="font-semibold text-sm truncate">
            Task.dev
          </span>
        </router-link>
      </div>

      <!-- Navegação Principal -->
      <nav class="p-2 space-y-1">
        <!-- Visão Geral -->
        <router-link
          to="/"
          class="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium transition"
          :class="
            route.path === '/'
              ? 'bg-accent/15 text-accent font-semibold'
              : 'text-dim hover:text-fg hover:bg-hover'
          "
          :title="!isExpanded ? 'Visão Geral' : undefined"
        >
          <LayoutDashboard class="w-4 h-4 shrink-0" />
          <span v-if="isExpanded" class="truncate">Visão Geral</span>
        </router-link>

        <!-- Divisor e Seção de Espaços -->
        <div class="pt-4 pb-1">
          <div
            v-if="isExpanded"
            class="flex items-center justify-between px-2.5 text-xs font-bold text-muted uppercase tracking-wider mb-1"
          >
            <span>Espaços</span>
            <button
              type="button"
              class="text-muted hover:text-accent p-0.5 rounded transition cursor-pointer"
              title="Novo espaço"
              @click="handleNewSpace"
            >
              <Plus class="w-3.5 h-3.5" />
            </button>
          </div>
          <div v-else class="h-[1px] bg-line my-2 mx-1" />

          <!-- Lista de Espaços -->
          <div class="space-y-0.5">
            <router-link
              v-for="space in spacesStore.spaces"
              :key="space.id"
              :to="`/spaces/${space.id}`"
              class="flex items-center justify-between px-2.5 py-2 rounded-lg text-sm font-medium transition group"
              :class="
                route.path === `/spaces/${space.id}`
                  ? 'bg-panel-2 text-fg font-semibold border border-line'
                  : 'text-dim hover:text-fg hover:bg-hover'
              "
              :title="!isExpanded ? space.name : undefined"
            >
              <div class="flex items-center gap-2.5 min-w-0">
                <component
                  :is="getSpaceIcon(space.icon)"
                  class="w-4 h-4 shrink-0"
                  :style="{ color: space.color || '#a69aeb' }"
                />
                <span v-if="isExpanded" class="truncate text-sm">{{ space.name }}</span>
              </div>

              <!-- Badge de pendentes (se expanded) -->
              <span
                v-if="isExpanded && space.pendingTasksCount"
                class="px-2 py-0.5 rounded-full bg-panel text-xs text-muted border border-line"
              >
                {{ space.pendingTasksCount }}
              </span>
            </router-link>

            <!-- Botão Novo Espaço rápido quando expanded -->
            <button
              v-if="isExpanded"
              type="button"
              class="w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-lg text-xs text-muted hover:text-accent hover:bg-hover transition cursor-pointer mt-1"
              @click="handleNewSpace"
            >
              <Plus class="w-3.5 h-3.5" />
              <span>Novo espaço</span>
            </button>
          </div>
        </div>
      </nav>
    </div>

    <!-- Base / Rodapé da Sidebar -->
    <div class="p-2 space-y-2 border-t border-line">
      <!-- Widget de XP / Ranks -->
      <XpWidget :collapsed="!isExpanded" />

      <!-- Links secundários -->
      <div class="space-y-0.5 pt-1">
        <router-link to="/docs" class="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium text-dim hover:text-fg hover:bg-hover" title="Documentação">
          <BookOpen class="w-4 h-4 shrink-0" />
          <span v-if="isExpanded">Documentação</span>
        </router-link>
        <!-- Admin Link (se admin ou operador) -->
        <router-link
          v-if="authStore.isOperator"
          to="/admin"
          class="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium transition"
          :class="
            route.path === '/admin'
              ? 'bg-accent/15 text-accent font-semibold'
              : 'text-dim hover:text-fg hover:bg-hover'
          "
          :title="!isExpanded ? 'Painel Admin' : undefined"
        >
          <Shield class="w-4 h-4 shrink-0 text-accent" />
          <span v-if="isExpanded" class="truncate">Administração</span>
        </router-link>

        <!-- Configurações -->
        <router-link
          to="/settings"
          class="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium transition"
          :class="
            route.path === '/settings'
              ? 'bg-accent/15 text-accent font-semibold'
              : 'text-dim hover:text-fg hover:bg-hover'
          "
          :title="!isExpanded ? 'Configurações' : undefined"
        >
          <Settings class="w-4 h-4 shrink-0" />
          <span v-if="isExpanded" class="truncate">Configurações</span>
        </router-link>

        <!-- Logout -->
        <button
          type="button"
          class="w-full flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium text-dim hover:text-[#e9a0a7] hover:bg-hover transition cursor-pointer"
          :title="!isExpanded ? 'Sair' : undefined"
          @click="handleLogout"
        >
          <LogOut class="w-4 h-4 shrink-0" />
          <span v-if="isExpanded" class="truncate">Sair da conta</span>
        </button>
      </div>
    </div>
  </aside>
</template>
