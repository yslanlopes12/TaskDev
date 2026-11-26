<script setup lang="ts">
import { computed, ref } from 'vue'
import { Bot, Check, Kanban, List, RotateCcw, Sparkles } from 'lucide-vue-next'
import AppButton from '@/components/ui/AppButton.vue'
import StatusTag from '@/components/ui/StatusTag.vue'
import PriorityDot from '@/components/ui/PriorityDot.vue'
import type { TaskStatus } from '@/types'

const view = ref<'list' | 'board'>('list')
const status = ref<TaskStatus>('PENDING')
const steps = ['Estudar sintaxe e lógica', 'Criar um projeto com o básico', 'Estudar orientação a objetos', 'Praticar com um projeto de POO', 'Conhecer o Spring Boot', 'Construir uma pequena API']
const checked = ref<boolean[]>(steps.map(() => false))
const subtaskXp = ref<number[]>(steps.map(() => 0))
const parentXp = ref(0)
const preview = ref(false)
const taskTitle = ref('Estudar Java')
const titleDraft = ref('Estudar Java com projetos práticos')
const count = computed(() => checked.value.filter(Boolean).length)
const xp = computed(() => subtaskXp.value.reduce((sum, value) => sum + value, 0) + parentXp.value)
function setStatus(value: TaskStatus) {
  if (value === 'DONE' && status.value !== 'DONE') {
    parentXp.value = Math.max(0, 30 - subtaskXp.value.reduce((sum, item) => sum + item, 0))
    checked.value = steps.map(() => true)
  } else if (value !== 'DONE' && status.value === 'DONE') parentXp.value = 0
  status.value = value
}
function selectStatus(event: Event) { setStatus((event.target as HTMLSelectElement).value as TaskStatus) }
function toggleStep(index: number) { subtaskXp.value[index] = checked.value[index] ? 5 : 0 }
const columns: { status: TaskStatus; title: string }[] = [
  { status: 'PENDING', title: 'Pendente' }, { status: 'IN_PROGRESS', title: 'Em andamento' }, { status: 'DONE', title: 'Concluído' },
]
function reset() {
  checked.value = steps.map(() => false)
  subtaskXp.value = steps.map(() => 0)
  parentXp.value = 0
  status.value = 'PENDING'
  view.value = 'list'
  preview.value = false
  taskTitle.value = 'Estudar Java'
  titleDraft.value = 'Estudar Java com projetos práticos'
}
function applyProposal() {
  if (!titleDraft.value.trim()) return
  taskTitle.value = titleDraft.value.trim()
  preview.value = false
}
</script>

<template>
  <section class="docs-demo" aria-label="Demonstração interativa de tarefas">
    <div class="demo-toolbar">
      <span class="demo-label"><span class="demo-dot" /> DEMONSTRAÇÃO LOCAL</span>
      <button class="demo-reset" @click="reset"><RotateCcw :size="15" /> Recomeçar</button>
    </div>
    <div class="demo-space">
      <div><span class="docs-eyebrow">ESPAÇO DE EXEMPLO</span><h2>Meu próximo passo</h2><p>Explore os controles. Os dados ficam só nesta página.</p></div>
      <div class="demo-switch" role="group" aria-label="Visualização da demonstração">
        <button :aria-pressed="view === 'list'" @click="view = 'list'"><List :size="16" /> Lista</button>
        <button :aria-pressed="view === 'board'" @click="view = 'board'"><Kanban :size="16" /> Quadro</button>
      </div>
    </div>
    <div v-if="view === 'board'" class="demo-board">
      <div v-for="column in columns" :key="column.status" class="demo-column">
        <h3><span :class="`demo-status-${column.status}`" />{{ column.title }} <small>{{ status === column.status ? 1 : 0 }}</small></h3>
        <div v-if="status === column.status" class="demo-mini-card"><PriorityDot priority="MEDIUM" /><strong>{{ taskTitle }}</strong><span>{{ count }}/6 subtarefas</span></div>
        <p v-else class="demo-empty">Nenhuma tarefa</p>
      </div>
    </div>
    <div class="demo-task">
      <div class="demo-task-header">
        <button class="demo-complete" :class="{ completed: status === 'DONE' }" :aria-pressed="status === 'DONE'" :aria-label="status === 'DONE' ? 'Reabrir tarefa de exemplo' : 'Concluir tarefa de exemplo'" @click="setStatus(status === 'DONE' ? 'PENDING' : 'DONE')"><Check v-if="status === 'DONE'" :size="16" /></button>
        <strong :class="{ 'demo-strike': status === 'DONE' }">{{ taskTitle }}</strong>
        <StatusTag :status="status" />
      </div>
      <p>Um plano de estudos com seis pequenas etapas. Marque um item para observar o progresso.</p>
      <label v-for="(step, index) in steps" :key="step" class="demo-step">
        <input v-model="checked[index]" type="checkbox" @change="toggleStep(index)" />
        <span :class="{ 'demo-strike': checked[index] }">{{ step }}</span><small>+5 XP</small>
      </label>
      <div class="demo-task-footer">
        <PriorityDot priority="MEDIUM" show-label />
        <label>Status <select :value="status" aria-label="Status da tarefa de exemplo" @change="selectStatus"><option v-for="column in columns" :key="column.status" :value="column.status">{{ column.title }}</option></select></label>
      </div>
    </div>
    <div class="demo-progress" role="status" aria-live="polite">
      <Sparkles :size="18" /><strong>{{ xp }} XP</strong><span>{{ count }}/6 etapas concluídas</span><progress :value="count" max="6" :aria-label="`${count} de 6 subtarefas concluídas`" />
    </div>
    <div class="demo-ai">
      <div class="demo-ai-intro"><Bot :size="22" /><div><strong>Entenda a revisão de propostas</strong><p>Exemplo fixo de edição de título, sem chamada de IA.</p></div></div>
      <AppButton v-if="!preview" variant="secondary" @click="preview = true">Ver proposta de exemplo</AppButton>
      <form v-else class="demo-proposal" @submit.prevent="applyProposal">
        <label for="demo-title">Título proposto</label><input id="demo-title" v-model="titleDraft" maxlength="200" required />
        <div><AppButton type="submit" variant="primary">Confirmar edição no exemplo</AppButton><AppButton type="button" variant="ghost" @click="preview = false">Cancelar</AppButton></div>
      </form>
    </div>
  </section>
</template>
