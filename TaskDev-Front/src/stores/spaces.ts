import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Space, CreateSpaceData, UpdateSpaceData, SpaceMember } from '@/types'
import { spacesApi } from '@/api/spaces'

export const useSpacesStore = defineStore('spaces', () => {
  const spaces = ref<Space[]>([])
  const currentSpace = ref<Space | null>(null)
  const currentMembers = ref<SpaceMember[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const spacesById = computed(() => {
    const map = new Map<string, Space>()
    for (const space of spaces.value) {
      map.set(space.id, space)
    }
    return map
  })

  function getSpaceById(id: string): Space | undefined {
    return spacesById.value.get(id)
  }

  async function loadSpaces() {
    loading.value = true
    error.value = null
    try {
      spaces.value = await spacesApi.list()
    } catch (err: any) {
      error.value = err.message || 'Erro ao carregar espaços'
    } finally {
      loading.value = false
    }
  }

  async function selectSpace(id: string) {
    loading.value = true
    error.value = null
    try {
      const space = await spacesApi.get(id)
      currentSpace.value = space
      // Atualiza também na lista
      const index = spaces.value.findIndex((s) => s.id === id)
      if (index !== -1) {
        spaces.value[index] = space
      }
      return space
    } catch (err: any) {
      error.value = err.message || 'Erro ao carregar espaço'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function createSpace(data: CreateSpaceData): Promise<Space> {
    const newSpace = await spacesApi.create(data)
    spaces.value.unshift(newSpace)
    return newSpace
  }

  async function updateSpace(id: string, data: UpdateSpaceData): Promise<Space> {
    const updated = await spacesApi.update(id, data)
    const index = spaces.value.findIndex((s) => s.id === id)
    if (index !== -1) {
      spaces.value[index] = updated
    }
    if (currentSpace.value?.id === id) {
      currentSpace.value = updated
    }
    return updated
  }

  async function deleteSpace(id: string): Promise<void> {
    await spacesApi.delete(id)
    spaces.value = spaces.value.filter((s) => s.id !== id)
    if (currentSpace.value?.id === id) {
      currentSpace.value = null
    }
  }

  async function loadMembers(spaceId: string) {
    try {
      currentMembers.value = await spacesApi.getMembers(spaceId)
    } catch {
      currentMembers.value = []
    }
  }

  async function addMember(spaceId: string, username: string) {
    await spacesApi.addMember(spaceId, username)
    await loadMembers(spaceId)
  }

  async function removeMember(spaceId: string, userId: string) {
    await spacesApi.removeMember(spaceId, userId)
    currentMembers.value = currentMembers.value.filter((m) => m.userId !== userId)
  }

  return {
    spaces,
    currentSpace,
    currentMembers,
    loading,
    error,
    getSpaceById,
    loadSpaces,
    selectSpace,
    createSpace,
    updateSpace,
    deleteSpace,
    loadMembers,
    addMember,
    removeMember,
  }
})
