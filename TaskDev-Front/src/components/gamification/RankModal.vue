<script setup lang="ts">
import { useGamificationStore } from '@/stores/gamification'
import AppModal from '@/components/ui/AppModal.vue'
import ProgressBar from '@/components/ui/ProgressBar.vue'
import { Trophy, Award } from 'lucide-vue-next'

const gamificationStore = useGamificationStore()

const ranks = [
  { rank: 'S', level: 'Nível 35+', desc: 'Lendário — Domínio supremo das entregas', color: 'text-[#edc887] border-[#edc887]/30 bg-[#edc887]/15' },
  { rank: 'A', level: 'Nível 21 - 34', desc: 'Mestre — Alto ritmo e consistência', color: 'text-[#a69aeb] border-[#a69aeb]/30 bg-[#a69aeb]/15' },
  { rank: 'B', level: 'Nível 12 - 20', desc: 'Sênior — Produtividade refinada', color: 'text-[#a3bee9] border-[#a3bee9]/30 bg-[#a3bee9]/15' },
  { rank: 'C', level: 'Nível 6 - 11', desc: 'Pleno — Fluxo constante de tarefas', color: 'text-[#9cd9b7] border-[#9cd9b7]/30 bg-[#9cd9b7]/15' },
  { rank: 'D', level: 'Nível 3 - 5', desc: 'Júnior — Construindo o ritmo', color: 'text-dim border-line bg-panel-2' },
  { rank: 'E', level: 'Nível 1 - 2', desc: 'Iniciante — Começo da jornada', color: 'text-muted border-line bg-panel' },
]
</script>

<template>
  <AppModal
    :show="gamificationStore.rankModalOpen"
    title="Sistema de Ranks & Progresso"
    max-width="md"
    @close="gamificationStore.closeRankModal"
  >
    <div class="space-y-5">
      <!-- Status atual -->
      <div class="p-4 rounded-xl bg-panel-2 border border-line flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-xl bg-accent-bg border border-accent/30 flex items-center justify-center text-accent">
            <Trophy class="w-6 h-6" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-fg">Nível {{ gamificationStore.level }}</span>
              <span class="px-2 py-0.5 rounded border text-xs font-bold text-accent border-accent/30 bg-accent-bg">
                RANK {{ gamificationStore.rank }}
              </span>
            </div>
            <p class="text-xs text-muted mt-0.5">Total acumulado: {{ gamificationStore.totalXp }} XP</p>
          </div>
        </div>

        <div class="text-right">
          <span class="text-xs font-semibold text-accent">+{{ gamificationStore.xpToNextLevel }} XP</span>
          <p class="text-[11px] text-muted">para o Nível {{ gamificationStore.level + 1 }}</p>
        </div>
      </div>

      <!-- Barra de progresso para próximo nível -->
      <div class="space-y-1.5">
        <div class="flex justify-between text-xs text-dim">
          <span>Progresso do Nível {{ gamificationStore.level }}</span>
          <span>{{ gamificationStore.xpInCurrentLevel }} / 100 XP</span>
        </div>
        <ProgressBar :value="gamificationStore.xpInCurrentLevel" :max="100" color="bg-accent" height="h-2" />
      </div>

      <!-- Tabela explicativa de Ranks -->
      <div class="space-y-2">
        <h4 class="text-xs font-semibold text-dim uppercase tracking-wider flex items-center gap-1.5">
          <Award class="w-3.5 h-3.5 text-accent" />
          Hierarquia de Patentes
        </h4>

        <div class="divide-y divide-line rounded-lg border border-line bg-[#141518] overflow-hidden">
          <div
            v-for="r in ranks"
            :key="r.rank"
            class="flex items-center gap-3 p-3 transition"
            :class="gamificationStore.rank === r.rank ? 'bg-accent/10' : 'hover:bg-hover'"
          >
            <span
              class="w-7 h-7 rounded border font-bold text-xs flex items-center justify-center shrink-0"
              :class="r.color"
            >
              {{ r.rank }}
            </span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-xs font-semibold text-fg">{{ r.level }}</span>
                <span v-if="gamificationStore.rank === r.rank" class="text-[10px] text-accent font-bold uppercase tracking-wider">
                  (Seu Rank)
                </span>
              </div>
              <p class="text-[11px] text-muted truncate">{{ r.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </AppModal>
</template>
