import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { dashboardApi } from '@/api/dashboard'
import { soundFx } from '@/utils/sound'

function calculateLevel(xp: number): number {
  return Math.floor(xp / 100) + 1
}

function calculateRank(level: number): string {
  if (level >= 35) return 'S'
  if (level >= 21) return 'A'
  if (level >= 12) return 'B'
  if (level >= 6) return 'C'
  if (level >= 3) return 'D'
  return 'E'
}

export const useGamificationStore = defineStore('gamification', () => {
  const totalXp = ref(0)
  const rankModalOpen = ref(false)
  const recentXpGained = ref<number | null>(null)
  const isGainingXp = ref(false)
  const isLevelingUp = ref(false)

  const level = computed(() => calculateLevel(totalXp.value))
  const rank = computed(() => calculateRank(level.value))
  const xpInCurrentLevel = computed(() => totalXp.value % 100)
  const xpToNextLevel = computed(() => 100 - xpInCurrentLevel.value)

  async function loadProgress() {
    try {
      const data = await dashboardApi.progress()
      totalXp.value = data.totalXp
    } catch {
      // Falha silenciosa se offline
    }
  }

  function addXp(points: number) {
    if (points <= 0) return

    const previousLevel = level.value
    totalXp.value += points
    recentXpGained.value = points
    isGainingXp.value = true

    if (level.value > previousLevel) {
      isLevelingUp.value = true
      soundFx.playLevelUp()
      setTimeout(() => {
        isLevelingUp.value = false
      }, 2000)
    } else {
      soundFx.playXpGain()
    }

    setTimeout(() => {
      isGainingXp.value = false
    }, 600)

    setTimeout(() => {
      recentXpGained.value = null
    }, 2800)
  }

  function removeXp(points: number) {
    if (points <= 0) return
    totalXp.value = Math.max(0, totalXp.value - points)
  }

  function openRankModal() {
    rankModalOpen.value = true
  }

  function closeRankModal() {
    rankModalOpen.value = false
  }

  return {
    totalXp,
    level,
    rank,
    xpInCurrentLevel,
    xpToNextLevel,
    rankModalOpen,
    recentXpGained,
    isGainingXp,
    isLevelingUp,
    loadProgress,
    addXp,
    removeXp,
    openRankModal,
    closeRankModal,
  }
})
