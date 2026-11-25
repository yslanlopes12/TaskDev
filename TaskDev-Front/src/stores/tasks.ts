import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { TaskItem, TaskStatus, CreateTaskData, UpdateTaskData } from '@/types'
import { tasksApi } from '@/api/tasks'
import { useGamificationStore } from './gamification'

function getXpEstimate(priority?: string): number {
  switch (priority?.toUpperCase()) {
    case 'LOW': return 15
    case 'MEDIUM': return 30
    case 'HIGH': return 50
    case 'URGENT': return 80
    default: return 30
  }
}

export const useTasksStore = defineStore('tasks', () => {
  const tasks = ref<TaskItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const gamificationStore = useGamificationStore()

  const tasksBySpace = computed(() => {
    return (spaceId: string) => tasks.value.filter((t) => t.spaceId === spaceId)
  })

  const tasksByStatus = computed(() => {
    return (status: TaskStatus) => tasks.value.filter((t) => t.status === status)
  })

  async function loadTasks(spaceId?: string) {
    loading.value = true
    error.value = null
    try {
      const data = await tasksApi.list(spaceId)
      if (spaceId) {
        // Substitui ou mescla
        tasks.value = [
          ...tasks.value.filter((t) => t.spaceId !== spaceId),
          ...data,
        ]
      } else {
        tasks.value = data
      }
    } catch (err: any) {
      error.value = err.message || 'Erro ao carregar tarefas'
    } finally {
      loading.value = false
    }
  }

  async function createTask(data: CreateTaskData): Promise<TaskItem> {
    const newTask = await tasksApi.create(data)
    tasks.value.unshift(newTask)
    return newTask
  }

  async function updateTask(id: string, data: UpdateTaskData): Promise<TaskItem> {
    const updated = await tasksApi.update(id, data)
    const index = tasks.value.findIndex((t) => t.id === id)
    if (index !== -1) {
      tasks.value[index] = { ...tasks.value[index], ...updated }
    }
    return updated
  }

  async function deleteTask(id: string): Promise<void> {
    await tasksApi.delete(id)
    tasks.value = tasks.value.filter((t) => t.id !== id)
  }

  async function updateStatus(id: string, status: TaskStatus): Promise<TaskItem> {
    const task = tasks.value.find((t) => t.id === id)
    const oldStatus = task?.status

    if (task) {
      task.status = status
    }

    try {
      const updated = await tasksApi.updateStatus(id, status)
      const index = tasks.value.findIndex((t) => t.id === id)
      if (index !== -1) {
        tasks.value[index] = { ...tasks.value[index], ...updated }
      }
      return updated
    } catch (err) {
      if (task && oldStatus) {
        task.status = oldStatus
      }
      throw err
    }
  }

  async function moveTask(id: string, data: { status: TaskStatus; position: number }): Promise<void> {
    const taskIndex = tasks.value.findIndex((t) => t.id === id)
    if (taskIndex === -1) return

    const previousTask = { ...tasks.value[taskIndex] } as TaskItem
    const wasDone = previousTask.status === 'DONE'
    const isNowDone = data.status === 'DONE'

    // Optimistic update
    tasks.value[taskIndex] = {
      ...tasks.value[taskIndex],
      status: data.status,
      position: data.position,
    } as TaskItem

    // Se moveu para DONE no Kanban e ainda não pontuou, dispara premiação otimista instantânea
    if (isNowDone && !wasDone && !previousTask.xpAwarded) {
      completeTask(id).catch(() => {})
      return
    }

    // Se moveu de DONE para outro status no Kanban, desconclui e reverte o XP
    if (!isNowDone && wasDone && previousTask.xpAwarded) {
      uncompleteTask(id).catch(() => {})
      return
    }

    try {
      await tasksApi.move(id, data)
    } catch (err) {
      // Reverter em caso de falha
      tasks.value[taskIndex] = previousTask
      throw err
    }
  }

  async function completeTask(id: string): Promise<{ xpAwarded: boolean; points: number }> {
    const task = tasks.value.find((t) => t.id === id)
    if (!task) return { xpAwarded: false, points: 0 }

    const wasAlreadyAwarded = task.xpAwarded
    const estimatedPoints = getXpEstimate(task.priority)

    // Update Otimista Instantâneo (0ms de atraso visual)
    task.status = 'DONE'
    task.completedAt = new Date().toISOString()

    if (!wasAlreadyAwarded) {
      task.xpAwarded = true
      gamificationStore.addXp(estimatedPoints)
    }

    try {
      const result = await tasksApi.complete(id)
      if (result.xpAwarded && result.points !== estimatedPoints && !wasAlreadyAwarded) {
        gamificationStore.totalXp += (result.points - estimatedPoints)
      }
      return result
    } catch (err) {
      if (!wasAlreadyAwarded) {
        task.xpAwarded = false
        gamificationStore.totalXp = Math.max(0, gamificationStore.totalXp - estimatedPoints)
      }
      task.status = 'PENDING'
      throw err
    }
  }

  async function uncompleteTask(id: string): Promise<{ pointsDeducted: number }> {
    const task = tasks.value.find((t) => t.id === id)
    if (!task) return { pointsDeducted: 0 }

    const wasAwarded = task.xpAwarded
    const estimatedPoints = getXpEstimate(task.priority)

    // Update Otimista Instantâneo
    task.status = 'PENDING'
    task.completedAt = undefined

    if (wasAwarded) {
      task.xpAwarded = false
      gamificationStore.removeXp(estimatedPoints)
    }

    try {
      const result = await tasksApi.uncomplete(id)
      return result
    } catch (err) {
      task.status = 'DONE'
      task.xpAwarded = wasAwarded
      if (wasAwarded) {
        gamificationStore.addXp(estimatedPoints)
      }
      throw err
    }
  }

  return {
    tasks,
    loading,
    error,
    tasksBySpace,
    tasksByStatus,
    loadTasks,
    createTask,
    updateTask,
    deleteTask,
    updateStatus,
    moveTask,
    completeTask,
    uncompleteTask,
  }
})
