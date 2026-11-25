import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ThemeMode, ViewMode } from '@/types'

export const useUiStore = defineStore('ui', () => {
  const sidebarOpen = ref(window.innerWidth > 900)
  const chatOpen = ref(false)
  const theme = ref<ThemeMode>((localStorage.getItem('theme') as ThemeMode) || 'dark')
  const viewMode = ref<ViewMode>((localStorage.getItem('viewMode') as ViewMode) || 'list')

  // Task Drawer
  const taskDrawerOpen = ref(false)
  const selectedTaskId = ref<string | null>(null)
  const targetSpaceIdForNewTask = ref<string | undefined>(undefined)

  // Space Modal
  const spaceModalOpen = ref(false)
  const editingSpaceId = ref<string | null>(null)

  // Drag and drop state
  const isDraggingTask = ref(false)

  function setDraggingTask(val: boolean) {
    isDraggingTask.value = val
  }

  function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value
  }

  function toggleChat() {
    chatOpen.value = !chatOpen.value
  }

  function setSidebarOpen(open: boolean) {
    sidebarOpen.value = open
  }

  function setChatOpen(open: boolean) {
    chatOpen.value = open
  }

  function setTheme(t: ThemeMode) {
    theme.value = t
    localStorage.setItem('theme', t)
    applyTheme()
  }

  function setViewMode(m: ViewMode) {
    viewMode.value = m
    localStorage.setItem('viewMode', m)
  }

  function applyTheme() {
    const isDark = theme.value === 'dark' ||
      (theme.value === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)
    document.documentElement.classList.toggle('dark', isDark)
  }

  function openTaskDrawer(taskId: string | null = null, defaultSpaceId?: string) {
    selectedTaskId.value = taskId
    targetSpaceIdForNewTask.value = defaultSpaceId
    taskDrawerOpen.value = true
  }

  function closeTaskDrawer() {
    taskDrawerOpen.value = false
    selectedTaskId.value = null
    targetSpaceIdForNewTask.value = undefined
  }

  function openSpaceModal(spaceId: string | null = null) {
    editingSpaceId.value = spaceId
    spaceModalOpen.value = true
  }

  function closeSpaceModal() {
    spaceModalOpen.value = false
    editingSpaceId.value = null
  }

  applyTheme()

  return {
    sidebarOpen,
    chatOpen,
    theme,
    viewMode,
    taskDrawerOpen,
    selectedTaskId,
    targetSpaceIdForNewTask,
    spaceModalOpen,
    editingSpaceId,
    isDraggingTask,
    setDraggingTask,
    toggleSidebar,
    toggleChat,
    setSidebarOpen,
    setChatOpen,
    setTheme,
    setViewMode,
    openTaskDrawer,
    closeTaskDrawer,
    openSpaceModal,
    closeSpaceModal,
  }
})
