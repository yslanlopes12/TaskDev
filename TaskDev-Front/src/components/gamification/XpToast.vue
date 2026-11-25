<script setup lang="ts">
import { useGamificationStore } from '@/stores/gamification'
import { Sparkles } from 'lucide-vue-next'

const gamificationStore = useGamificationStore()
</script>

<template>
  <Transition
    enter-active-class="transition duration-400 ease-[cubic-bezier(0.34,1.56,0.64,1)]"
    enter-from-class="opacity-0 translate-y-12 scale-75"
    enter-to-class="opacity-100 translate-y-0 scale-100"
    leave-active-class="transition duration-300 ease-in"
    leave-from-class="opacity-100 scale-100 translate-y-0"
    leave-to-class="opacity-0 scale-90 -translate-y-4"
  >
    <div
      v-if="gamificationStore.recentXpGained !== null"
      class="fixed bottom-12 left-1/2 -translate-x-1/2 z-50 pointer-events-none"
    >
      <div class="flex items-center gap-2.5 px-4 py-2.5 rounded-full bg-accent text-[#101114] shadow-[0_10px_35px_rgba(166,154,235,0.4)] font-bold text-sm tracking-wide border border-white/30 backdrop-blur-md">
        <Sparkles class="w-4 h-4 fill-current animate-spin" style="animation-duration: 3s;" />
        <span class="tracking-wider">+{{ gamificationStore.recentXpGained }} XP CONQUISTADO!</span>
      </div>
    </div>
  </Transition>
</template>
