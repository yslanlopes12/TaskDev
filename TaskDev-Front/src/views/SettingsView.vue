<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'
import { useGamificationStore } from '@/stores/gamification'
import { useToast } from '@/composables/useToast'
import { agentApi } from '@/api/agent'
import { AI_PROVIDER_PRESETS, DEFAULT_AI_PRESET, detectProviderPresetId } from '@/constants/aiProviders'
import AppButton from '@/components/ui/AppButton.vue'
import type { ThemeMode, ViewMode, AgentProviderType } from '@/types'
import {
  User,
  Palette,
  Bot,
  KeyRound,
  CheckCircle2,
  AlertCircle,
  HelpCircle,
  ShieldCheck,
  ChevronDown,
  ChevronUp,
  RefreshCw,
} from 'lucide-vue-next'

const authStore = useAuthStore()
const uiStore = useUiStore()
const gamificationStore = useGamificationStore()
const { success, error: toastError } = useToast()

const activeTab = ref<'profile' | 'appearance' | 'agent' | 'password'>('profile')

// Estado do Agente IA
const selectedPresetId = ref<string>(DEFAULT_AI_PRESET.id)
const agentName = ref('')
const endpointUrl = ref(DEFAULT_AI_PRESET.defaultEndpointUrl)
const providerType = ref<AgentProviderType>(DEFAULT_AI_PRESET.providerType)
const selectedModelOption = ref<string>(DEFAULT_AI_PRESET.defaultModel)
const customModel = ref('')
const apiKey = ref('')
const isEnabled = ref(true)
const hasApiKey = ref(false)
const showAdvancedEndpoint = ref(false)
const agentLoading = ref(false)
const testLoading = ref(false)
const modelsLoading = ref(false)
const liveModels = ref<{ id: string; label: string }[]>([])
const savedPresetId = ref<string | null>(null)
const modelsHint = ref<string | null>(null)
const testResult = ref<{ success: boolean; message: string } | null>(null)
let apiKeyDebounceTimer: ReturnType<typeof setTimeout> | null = null

const currentPreset = computed(
  () => AI_PROVIDER_PRESETS.find((p) => p.id === selectedPresetId.value) ?? DEFAULT_AI_PRESET
)

const availableModels = computed(() => {
  if (liveModels.value.length > 0) {
    const presetIds = new Set(currentPreset.value.models.map((m) => m.id))
    const liveIds = new Set(liveModels.value.map((m) => m.id))
    const prioritized = currentPreset.value.models.filter((m) => liveIds.has(m.id))
    const others = liveModels.value.filter((m) => !presetIds.has(m.id))
    return [...prioritized, ...others]
  }
  return currentPreset.value.models
})

const resolvedModel = computed(() => {
  if (availableModels.value.length === 0 || selectedModelOption.value === '__custom__') {
    return customModel.value.trim()
  }
  return selectedModelOption.value
})

function handlePresetChange(presetId: string) {
  selectedPresetId.value = presetId
  const preset = AI_PROVIDER_PRESETS.find((p) => p.id === presetId)
  if (!preset) return

  providerType.value = preset.providerType
  if (preset.defaultEndpointUrl) {
    endpointUrl.value = preset.defaultEndpointUrl
  } else {
    endpointUrl.value = ''
  }

  if (!agentName.value.trim() || AI_PROVIDER_PRESETS.some((p) => p.label === agentName.value.trim())) {
    agentName.value = preset.id === 'custom' ? '' : preset.label
  }

  if (preset.models.length > 0) {
    selectedModelOption.value = preset.defaultModel
    customModel.value = ''
  } else {
    selectedModelOption.value = '__custom__'
    customModel.value = ''
  }

  showAdvancedEndpoint.value = preset.allowCustomEndpoint
  liveModels.value = []
  modelsHint.value = null
  testResult.value = null

  const hasTypedKey = Boolean(apiKey.value.trim())
  const hasSavedKeyForPreset = hasApiKey.value && savedPresetId.value === presetId
  const canQueryWithoutKey = preset.id === 'ollama' || preset.id === 'openrouter'

  if (endpointUrl.value.trim() && preset.providerType !== 'WEBHOOK' && (hasTypedKey || hasSavedKeyForPreset || canQueryWithoutKey)) {
    void fetchLiveModels(true)
  } else if (preset.requiresApiKey) {
    modelsHint.value = 'Cole sua API Key para buscar os modelos disponíveis deste provedor.'
  }
}

async function fetchLiveModels(silent = false) {
  if (!endpointUrl.value.trim()) {
    if (!silent) toastError('Informe o Endpoint URL antes de buscar os modelos.')
    return
  }
  modelsLoading.value = true
  modelsHint.value = null
  try {
    const res = await agentApi.listModels({
      endpointUrl: endpointUrl.value.trim(),
      apiKey: apiKey.value.trim() || undefined,
      providerType: providerType.value,
    })
    if (res.models && res.models.length > 0) {
      liveModels.value = res.models.map((id) => ({ id, label: id }))
      const currentSelected = selectedModelOption.value === '__custom__' ? customModel.value.trim() : selectedModelOption.value
      if (currentSelected && res.models.includes(currentSelected)) {
        selectedModelOption.value = currentSelected
        customModel.value = ''
      } else {
        const preferredFromPreset = currentPreset.value.models.find((m) => res.models.includes(m.id))
        selectedModelOption.value = preferredFromPreset ? preferredFromPreset.id : res.models[0]!
        customModel.value = ''
      }
      modelsHint.value = `${res.models.length} modelos de chat ativos carregados da API.`
      if (!silent) success(`${res.models.length} modelos carregados do provedor!`)
    } else {
      if (!silent) toastError('Nenhum modelo retornado pelo provedor.')
    }
  } catch (err: any) {
    if (!silent) {
      toastError(err?.message || 'Erro ao buscar modelos disponíveis.')
    } else {
      modelsHint.value = 'Usando lista padrão (informe uma API Key válida para atualizar da API).'
    }
  } finally {
    modelsLoading.value = false
  }
}

watch(apiKey, (newVal) => {
  if (apiKeyDebounceTimer) clearTimeout(apiKeyDebounceTimer)
  const trimmed = newVal.trim()
  if (trimmed.length < 10 || !endpointUrl.value.trim() || providerType.value === 'WEBHOOK') {
    return
  }
  apiKeyDebounceTimer = setTimeout(() => {
    void fetchLiveModels(true)
  }, 600)
})

onMounted(async () => {
  try {
    const config = await agentApi.getConfig()
    if (config && config.name) {
      agentName.value = config.name
      endpointUrl.value = config.endpointUrl
      providerType.value = config.providerType
      isEnabled.value = config.isEnabled
      hasApiKey.value = config.hasApiKey

      const presetId = detectProviderPresetId(config.endpointUrl, config.providerType)
      selectedPresetId.value = presetId
      savedPresetId.value = presetId
      const preset = AI_PROVIDER_PRESETS.find((p) => p.id === presetId)

      if (preset && preset.models.length > 0) {
        const match = preset.models.find((m) => m.id === config.model)
        if (match) {
          selectedModelOption.value = match.id
          customModel.value = ''
        } else if (config.model) {
          selectedModelOption.value = '__custom__'
          customModel.value = config.model
        } else {
          selectedModelOption.value = preset.defaultModel
        }
      } else {
        selectedModelOption.value = '__custom__'
        customModel.value = config.model || ''
      }

      showAdvancedEndpoint.value = preset?.allowCustomEndpoint ?? false

      if (config.hasApiKey || presetId === 'ollama' || presetId === 'openrouter') {
        void fetchLiveModels(true)
      }
    }
  } catch {}
})

async function saveAgentConfig() {
  if (currentPreset.value.requiresApiKey && !apiKey.value.trim() && !hasApiKey.value) {
    toastError('Informe a API Key para este provedor.')
    return
  }

  agentLoading.value = true
  testResult.value = null
  try {
    await agentApi.saveConfig({
      name: agentName.value.trim() || currentPreset.value.label,
      endpointUrl: endpointUrl.value.trim(),
      providerType: providerType.value,
      model: resolvedModel.value || undefined,
      apiKey: apiKey.value.trim() || undefined,
      isEnabled: isEnabled.value,
    })
    success('Configuração de IA salva com sucesso!')
    savedPresetId.value = selectedPresetId.value
    if (apiKey.value.trim()) {
      apiKey.value = ''
      hasApiKey.value = true
    }
  } catch (err: any) {
    toastError(err.message || 'Erro ao salvar configurações de IA')
  } finally {
    agentLoading.value = false
  }
}

async function testAgentConnection() {
  testLoading.value = true
  testResult.value = null
  try {
    const res = await agentApi.test({
      endpointUrl: endpointUrl.value.trim() || undefined,
      apiKey: apiKey.value.trim() || undefined,
      providerType: providerType.value,
      model: resolvedModel.value || undefined,
    })
    testResult.value = res
    if (res.success) {
      success('Conexão estabelecida com sucesso!')
    } else {
      toastError(res.message)
    }
  } catch (err: any) {
    testResult.value = {
      success: false,
      message: err.message || 'Falha ao conectar com o endpoint de IA.',
    }
    toastError(testResult.value.message)
  } finally {
    testLoading.value = false
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-xl font-bold text-fg">Configurações</h1>
      <p class="text-xs text-muted mt-0.5">Gerencie seu perfil, preferências de interface e agentes de IA</p>
    </div>

    <!-- Layout: Tabs Laterais + Painel de Conteúdo -->
    <div class="flex flex-col md:flex-row gap-6 items-start">
      <!-- Navegação lateral das abas -->
      <nav class="w-full md:w-56 space-y-1 shrink-0">
        <button
          type="button"
          class="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer"
          :class="activeTab === 'profile' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
          @click="activeTab = 'profile'"
        >
          <User class="w-4 h-4" />
          <span>Perfil & Patente</span>
        </button>

        <button
          type="button"
          class="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer"
          :class="activeTab === 'appearance' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
          @click="activeTab = 'appearance'"
        >
          <Palette class="w-4 h-4" />
          <span>Aparência</span>
        </button>

        <button
          type="button"
          class="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer"
          :class="activeTab === 'agent' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
          @click="activeTab = 'agent'"
        >
          <Bot class="w-4 h-4" />
          <span>Integração de IA</span>
        </button>

        <button
          type="button"
          class="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-lg text-xs font-semibold transition cursor-pointer"
          :class="activeTab === 'password' ? 'bg-panel-2 text-accent border border-line' : 'text-dim hover:text-fg hover:bg-hover'"
          @click="activeTab = 'password'"
        >
          <KeyRound class="w-4 h-4" />
          <span>Segurança & Senha</span>
        </button>
      </nav>

      <!-- Painel Principal de Configurações -->
      <div class="flex-1 w-full bg-[#141518] border border-line rounded-2xl p-6 min-h-[420px]">
        <!-- SEÇÃO 1: PERFIL -->
        <div v-if="activeTab === 'profile'" class="space-y-6">
          <div class="border-b border-line pb-4">
            <h2 class="text-sm font-bold text-fg">Perfil de Usuário</h2>
            <p class="text-xs text-muted">Informações básicas e dados de gamificação</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">Nome</label>
              <input
                :value="authStore.user?.firstName"
                readonly
                class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg text-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">Sobrenome</label>
              <input
                :value="authStore.user?.lastName"
                readonly
                class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg text-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">Nome de Usuário</label>
              <input
                :value="`@${authStore.user?.username}`"
                readonly
                class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-muted font-mono text-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">Função / Role</label>
              <input
                :value="authStore.user?.role"
                readonly
                class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-accent font-semibold text-xs"
              />
            </div>
          </div>

          <!-- Resumo de Gamificação -->
          <div class="p-4 rounded-xl bg-panel-2 border border-line flex items-center justify-between">
            <div>
              <span class="text-xs font-semibold text-dim uppercase tracking-wider">Patente Atual</span>
              <div class="flex items-center gap-2 mt-1">
                <span class="text-base font-bold text-fg">Nível {{ gamificationStore.level }}</span>
                <span class="px-2 py-0.5 rounded border text-xs font-bold text-accent border-accent/30 bg-accent-bg">
                  RANK {{ gamificationStore.rank }}
                </span>
              </div>
            </div>

            <div class="text-right">
              <span class="text-xs text-muted">XP Acumulado</span>
              <p class="text-base font-bold text-fg">{{ gamificationStore.totalXp }} XP</p>
            </div>
          </div>
        </div>

        <!-- SEÇÃO 2: APARÊNCIA -->
        <div v-else-if="activeTab === 'appearance'" class="space-y-6">
          <div class="border-b border-line pb-4">
            <h2 class="text-sm font-bold text-fg">Aparência & Interface</h2>
            <p class="text-xs text-muted">Personalize as cores e modo de visualização padrão</p>
          </div>

          <!-- Tema -->
          <div class="space-y-2">
            <label class="block text-xs font-semibold text-dim uppercase tracking-wider">Tema de Cores</label>
            <div class="grid grid-cols-3 gap-3">
              <button
                type="button"
                class="p-3 rounded-lg border text-center transition cursor-pointer"
                :class="uiStore.theme === 'dark' ? 'bg-accent/15 border-accent text-accent font-bold' : 'bg-panel-2 border-line text-dim hover:text-fg'"
                @click="uiStore.setTheme('dark')"
              >
                Escuro (Padrão)
              </button>
              <button
                type="button"
                class="p-3 rounded-lg border text-center transition cursor-pointer"
                :class="uiStore.theme === 'light' ? 'bg-accent/15 border-accent text-accent font-bold' : 'bg-panel-2 border-line text-dim hover:text-fg'"
                @click="uiStore.setTheme('light')"
              >
                Claro
              </button>
              <button
                type="button"
                class="p-3 rounded-lg border text-center transition cursor-pointer"
                :class="uiStore.theme === 'system' ? 'bg-accent/15 border-accent text-accent font-bold' : 'bg-panel-2 border-line text-dim hover:text-fg'"
                @click="uiStore.setTheme('system')"
              >
                Sistema
              </button>
            </div>
          </div>

          <!-- Modo de exibição preferencial -->
          <div class="space-y-2">
            <label class="block text-xs font-semibold text-dim uppercase tracking-wider">Visualização Padrão</label>
            <div class="grid grid-cols-2 gap-3">
              <button
                type="button"
                class="p-3 rounded-lg border text-left transition cursor-pointer"
                :class="uiStore.viewMode === 'list' ? 'bg-accent/15 border-accent text-accent font-bold' : 'bg-panel-2 border-line text-dim hover:text-fg'"
                @click="uiStore.setViewMode('list')"
              >
                <span class="block text-xs font-semibold">Lista / Checklist</span>
                <span class="block text-[11px] text-muted font-normal mt-0.5">Visão vertical agrupada</span>
              </button>
              <button
                type="button"
                class="p-3 rounded-lg border text-left transition cursor-pointer"
                :class="uiStore.viewMode === 'board' ? 'bg-accent/15 border-accent text-accent font-bold' : 'bg-panel-2 border-line text-dim hover:text-fg'"
                @click="uiStore.setViewMode('board')"
              >
                <span class="block text-xs font-semibold">Quadro / Kanban</span>
                <span class="block text-[11px] text-muted font-normal mt-0.5">Colunas por status</span>
              </button>
            </div>
          </div>
        </div>

        <!-- SEÇÃO 3: INTEGRAÇÃO IA -->
        <div v-else-if="activeTab === 'agent'" class="space-y-6">
          <div class="border-b border-line pb-4 flex items-center justify-between">
            <div>
              <h2 class="text-sm font-bold text-fg">Provedor de IA (Assistente)</h2>
              <p class="text-xs text-muted">Configure o agente OpenAI-compatible ou webhook</p>
            </div>

            <!-- Toggle Habilitado -->
            <label class="flex items-center gap-2 cursor-pointer text-xs font-semibold text-dim">
              <input
                v-model="isEnabled"
                type="checkbox"
                class="w-4 h-4 rounded border-line text-accent focus:ring-0 cursor-pointer"
              />
              <span>Ativo</span>
            </label>
          </div>

          <form @submit.prevent="saveAgentConfig" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <!-- Nome da Conexão -->
              <div>
                <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
                  Nome da Conexão
                </label>
                <input
                  v-model="agentName"
                  type="text"
                  placeholder="Ex: NVIDIA NIM, DeepSeek, ChatGPT..."
                  required
                  class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs"
                />
              </div>

              <!-- Provedor -->
              <div>
                <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
                  Provedor de IA
                </label>
                <select
                  :value="selectedPresetId"
                  class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
                  @change="handlePresetChange(($event.target as HTMLSelectElement).value)"
                >
                  <option v-for="preset in AI_PROVIDER_PRESETS" :key="preset.id" :value="preset.id">
                    {{ preset.label }}
                  </option>
                </select>
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <!-- Modelo Disponível -->
              <div>
                <div class="flex items-center justify-between mb-1">
                  <label class="block text-xs font-semibold text-dim uppercase tracking-wider">
                    Modelo
                  </label>
                  <button
                    v-if="providerType !== 'WEBHOOK'"
                    type="button"
                    class="inline-flex items-center gap-1 text-[11px] font-medium text-accent hover:underline cursor-pointer disabled:opacity-50"
                    :disabled="modelsLoading"
                    title="Buscar modelos ativos diretamente do provedor"
                    @click="fetchLiveModels(false)"
                  >
                    <RefreshCw class="w-3 h-3" :class="modelsLoading ? 'animate-spin' : ''" />
                    <span>{{ modelsLoading ? 'Buscando...' : 'Buscar da API' }}</span>
                  </button>
                </div>
                <select
                  v-if="availableModels.length > 0"
                  v-model="selectedModelOption"
                  class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
                >
                  <option v-for="m in availableModels" :key="m.id" :value="m.id">
                    {{ m.label }}
                  </option>
                  <option value="__custom__">Outro modelo (digitar)...</option>
                </select>

                <input
                  v-if="availableModels.length === 0 || selectedModelOption === '__custom__'"
                  v-model="customModel"
                  type="text"
                  placeholder="Ex: meta/llama-3.3-70b-instruct, gpt-4o..."
                  :class="availableModels.length > 0 ? 'mt-2' : ''"
                  class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs font-mono"
                />

                <p v-if="modelsHint" class="mt-1 text-[11px] text-muted">
                  {{ modelsHint }}
                </p>
              </div>

              <!-- API Key / Token -->
              <div>
                <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
                  API Key {{ currentPreset.requiresApiKey ? '' : '(opcional)' }}
                </label>
                <input
                  v-model="apiKey"
                  type="password"
                  :placeholder="hasApiKey ? '••••••••••••••• (já salva e criptografada)' : currentPreset.apiKeyPlaceholder"
                  class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs font-mono"
                />
                <p class="mt-1 text-[11px] text-muted flex items-center gap-1">
                  <ShieldCheck class="w-3.5 h-3.5 text-[#9cd9b7] shrink-0" />
                  <span>Criptografada com AES-256 no servidor</span>
                </p>
              </div>
            </div>

            <!-- Endpoint URL (automático para provedores conhecidos, expansível no avançado) -->
            <div class="pt-1">
              <button
                type="button"
                class="flex items-center gap-1.5 text-[11px] font-medium text-muted hover:text-fg transition cursor-pointer"
                @click="showAdvancedEndpoint = !showAdvancedEndpoint"
              >
                <ChevronUp v-if="showAdvancedEndpoint" class="w-3.5 h-3.5" />
                <ChevronDown v-else class="w-3.5 h-3.5" />
                <span>Configurações avançadas (Endpoint URL)</span>
              </button>

              <div v-if="showAdvancedEndpoint" class="mt-3 p-3 rounded-xl bg-panel-2/60 border border-line space-y-3">
                <div>
                  <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
                    Endpoint URL
                  </label>
                  <input
                    v-model="endpointUrl"
                    type="url"
                    placeholder="https://api.openai.com/v1/chat/completions"
                    required
                    class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg placeholder:text-muted focus:outline-none focus:border-accent text-xs font-mono"
                  />
                </div>

                <div v-if="selectedPresetId === 'custom'">
                  <label class="block text-xs font-semibold text-dim uppercase tracking-wider mb-1">
                    Protocolo da API
                  </label>
                  <select
                    v-model="providerType"
                    class="w-full px-3 py-2 rounded-lg bg-panel-2 border border-line text-fg text-xs focus:outline-none focus:border-accent"
                  >
                    <option value="OPENAI_COMPATIBLE">OpenAI-Compatible (Chat Completions)</option>
                    <option value="ANTHROPIC">Anthropic (Messages API)</option>
                    <option value="GEMINI">Google Gemini API</option>
                    <option value="WEBHOOK">Webhook Personalizado</option>
                  </select>
                </div>
              </div>
            </div>

            <!-- Feedback de Teste de Conexão -->
            <div
              v-if="testResult"
              class="p-3 rounded-lg border text-xs flex items-center gap-2"
              :class="testResult.success ? 'bg-[#9cd9b7]/10 border-[#9cd9b7]/30 text-[#9cd9b7]' : 'bg-[#e9a0a7]/10 border-[#e9a0a7]/30 text-[#e9a0a7]'"
            >
              <CheckCircle2 v-if="testResult.success" class="w-4 h-4 shrink-0" />
              <AlertCircle v-else class="w-4 h-4 shrink-0" />
              <span>{{ testResult.message }}</span>
            </div>

            <!-- Ações -->
            <div class="flex items-center justify-between pt-3 border-t border-line">
              <AppButton
                type="button"
                variant="secondary"
                size="sm"
                :loading="testLoading"
                @click="testAgentConnection"
              >
                Testar Conexão
              </AppButton>

              <AppButton
                type="submit"
                variant="primary"
                size="sm"
                :loading="agentLoading"
              >
                Salvar Configuração
              </AppButton>
            </div>
          </form>
        </div>

        <!-- SEÇÃO 4: SENHA & SEGURANÇA -->
        <div v-else-if="activeTab === 'password'" class="space-y-6">
          <div class="border-b border-line pb-4">
            <h2 class="text-sm font-bold text-fg">Segurança & Redefinição de Senha</h2>
            <p class="text-xs text-muted">Política de segurança do sistema Task.dev</p>
          </div>

          <div class="p-4 rounded-xl bg-panel-2 border border-line space-y-3">
            <div class="flex items-center gap-2 text-accent font-semibold text-xs">
              <HelpCircle class="w-4 h-4" />
              <span>Como redefinir sua senha?</span>
            </div>

            <p class="text-xs text-dim leading-relaxed">
              No Task.dev, as senhas são geradas com criptografia forte e nunca são armazenadas em texto plano ou expostas por e-mail.
            </p>

            <p class="text-xs text-dim leading-relaxed">
              Se você perdeu o acesso à sua senha, solicite um reset diretamente a um
              <strong class="text-fg">Administrador</strong> ou <strong class="text-fg">Operador</strong> da organização. Ele gerará uma nova senha de uso único no painel administrativo.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
