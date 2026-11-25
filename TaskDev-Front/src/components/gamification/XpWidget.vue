<script setup lang="ts">
import { computed } from 'vue'
import { useGamificationStore } from '@/stores/gamification'
import ProgressBar from '@/components/ui/ProgressBar.vue'

defineProps<{
  collapsed?: boolean
}>()

const gamificationStore = useGamificationStore()

const rankColors: Record<string, string> = {
  S: 'text-[#edc887] border-[#edc887]/30 bg-[#edc887]/15',
  A: 'text-[#a69aeb] border-[#a69aeb]/30 bg-[#a69aeb]/15',
  B: 'text-[#a3bee9] border-[#a3bee9]/30 bg-[#a3bee9]/15',
  C: 'text-[#9cd9b7] border-[#9cd9b7]/30 bg-[#9cd9b7]/15',
  D: 'text-dim border-line bg-panel-2',
  E: 'text-muted border-line bg-panel',
}

const currentRankColor = computed(() => rankColors[gamificationStore.rank] || 'text-dim border-line bg-panel-2')
</script>

<template>
  <button
    type="button"
    class="w-full text-left rounded-lg transition-all duration-300 p-2 cursor-pointer group relative overflow-hidden"
    :class="[
      collapsed ? 'flex justify-center hover:bg-hover' : 'bg-[#191a1e] border border-line hover:border-line-soft',
      gamificationStore.isGainingXp ? 'ring-1 ring-accent border-accent shadow-[0_0_20px_rgba(166,154,235,0.25)] scale-[1.01]' : '',
      gamificationStore.isLevelingUp ? 'ring-2 ring-yellow border-yellow shadow-[0_0_25px_rgba(237,200,135,0.4)] animate-pulse' : ''
    ]"
    title="Clique para ver o ranking e histórico de XP"
    @click="gamificationStore.openRankModal"
  >
    <!-- Collapsed View -->
    <div v-if="collapsed" class="flex flex-col items-center gap-1">
      <span
        class="w-7 h-7 rounded border font-bold text-xs flex items-center justify-center transition-all duration-300 group-hover:scale-105"
        :class="[currentRankColor, gamificationStore.isGainingXp ? 'scale-110' : '']"
      >
        {{ gamificationStore.rank }}
      </span>
      <span class="text-xs text-muted">Lv.{{ gamificationStore.level }}</span>
    </div>

    <!-- Expanded View -->
    <div v-else class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span
            class="px-2 py-0.5 rounded border text-xs font-bold transition-all duration-300"
            :class="[currentRankColor, gamificationStore.isGainingXp ? 'scale-105' : '']"
          >
            RANK {{ gamificationStore.rank }}
          </span>
          <span class="text-xs font-semibold text-fg flex items-center gap-1.5">
            <span>Nível {{ gamificationStore.level }}</span>
            <span
              v-if="gamificationStore.isLevelingUp"
              class="text-xs uppercase font-bold text-yellow animate-bounce"
            >
              UP!
            </span>
          </span>
        </div>
        <div class="flex items-center gap-1.5">
          <span
            v-if="gamificationStore.isGainingXp && gamificationStore.recentXpGained"
            class="text-xs font-bold text-accent animate-fade-in"
          >
            +{{ gamificationStore.recentXpGained }}
          </span>
          <span class="text-xs text-muted tabular-nums">
            {{ gamificationStore.xpInCurrentLevel }}/100 XP
          </span>
        </div>
      </div>

      <ProgressBar
        :value="gamificationStore.xpInCurrentLevel"
        :max="100"
        color="bg-accent"
        height="h-1.5"
      />
    </div>
  </button>
</template>
