import type { AgentProviderType } from '@/types'

export interface AiProviderPreset {
  id: string
  label: string
  providerType: AgentProviderType
  defaultEndpointUrl: string
  defaultModel: string
  models: { id: string; label: string }[]
  requiresApiKey: boolean
  apiKeyPlaceholder: string
  allowCustomEndpoint: boolean
}

export const AI_PROVIDER_PRESETS: AiProviderPreset[] = [
  {
    id: 'nvidia',
    label: 'NVIDIA NIM',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: 'https://integrate.api.nvidia.com/v1/chat/completions',
    defaultModel: 'meta/llama-3.3-70b-instruct',
    models: [
      { id: 'meta/llama-3.3-70b-instruct', label: 'Llama 3.3 70B Instruct (Recomendado)' },
      { id: 'meta/llama-3.1-8b-instruct', label: 'Llama 3.1 8B Instruct (Mais rápido)' },
      { id: 'qwen/qwen2.5-7b-instruct', label: 'Qwen 2.5 7B Instruct (Rápido)' },
      { id: 'google/gemma-2-9b-it', label: 'Gemma 2 9B IT (Rápido)' },
      { id: 'meta/llama-3.1-70b-instruct', label: 'Llama 3.1 70B Instruct' },
      { id: 'qwen/qwen2.5-72b-instruct', label: 'Qwen 2.5 72B Instruct' },
      { id: 'deepseek-ai/deepseek-r1', label: 'DeepSeek R1' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'nvapi-...',
    allowCustomEndpoint: false,
  },
  {
    id: 'deepseek',
    label: 'DeepSeek',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: 'https://api.deepseek.com/chat/completions',
    defaultModel: 'deepseek-chat',
    models: [
      { id: 'deepseek-chat', label: 'DeepSeek Chat (V3)' },
      { id: 'deepseek-reasoner', label: 'DeepSeek Reasoner (R1)' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'sk-...',
    allowCustomEndpoint: false,
  },
  {
    id: 'openai',
    label: 'ChatGPT (OpenAI)',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: 'https://api.openai.com/v1/chat/completions',
    defaultModel: 'gpt-4o-mini',
    models: [
      { id: 'gpt-4o-mini', label: 'GPT-4o Mini' },
      { id: 'gpt-4o', label: 'GPT-4o' },
      { id: 'gpt-4.1', label: 'GPT-4.1' },
      { id: 'o3-mini', label: 'o3-mini' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'sk-...',
    allowCustomEndpoint: false,
  },
  {
    id: 'anthropic',
    label: 'Claude (Anthropic)',
    providerType: 'ANTHROPIC',
    defaultEndpointUrl: 'https://api.anthropic.com/v1/messages',
    defaultModel: 'claude-3-5-sonnet-latest',
    models: [
      { id: 'claude-3-7-sonnet-latest', label: 'Claude 3.7 Sonnet' },
      { id: 'claude-3-5-sonnet-latest', label: 'Claude 3.5 Sonnet' },
      { id: 'claude-3-5-haiku-latest', label: 'Claude 3.5 Haiku' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'sk-ant-...',
    allowCustomEndpoint: false,
  },
  {
    id: 'gemini',
    label: 'Gemini (Google)',
    providerType: 'GEMINI',
    defaultEndpointUrl: 'https://generativelanguage.googleapis.com/v1beta',
    defaultModel: 'gemini-2.0-flash',
    models: [
      { id: 'gemini-2.5-flash', label: 'Gemini 2.5 Flash' },
      { id: 'gemini-2.0-flash', label: 'Gemini 2.0 Flash' },
      { id: 'gemini-1.5-pro', label: 'Gemini 1.5 Pro' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'AIza...',
    allowCustomEndpoint: false,
  },
  {
    id: 'openrouter',
    label: 'OpenRouter',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: 'https://openrouter.ai/api/v1/chat/completions',
    defaultModel: 'openai/gpt-4o-mini',
    models: [
      { id: 'openai/gpt-4o-mini', label: 'OpenAI GPT-4o Mini' },
      { id: 'anthropic/claude-3.5-sonnet', label: 'Claude 3.5 Sonnet' },
      { id: 'google/gemini-2.0-flash-001', label: 'Gemini 2.0 Flash' },
      { id: 'deepseek/deepseek-chat', label: 'DeepSeek Chat' },
      { id: 'meta-llama/llama-3.3-70b-instruct:free', label: 'Llama 3.3 70B (Free)' },
    ],
    requiresApiKey: true,
    apiKeyPlaceholder: 'sk-or-...',
    allowCustomEndpoint: false,
  },
  {
    id: 'ollama',
    label: 'Llama (Ollama Local)',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: 'http://localhost:11434/v1/chat/completions',
    defaultModel: 'llama3.2',
    models: [
      { id: 'llama3.2', label: 'Llama 3.2' },
      { id: 'llama3.1', label: 'Llama 3.1' },
      { id: 'qwen2.5', label: 'Qwen 2.5' },
      { id: 'deepseek-r1', label: 'DeepSeek R1' },
      { id: 'mistral', label: 'Mistral' },
    ],
    requiresApiKey: false,
    apiKeyPlaceholder: 'Opcional para Ollama local',
    allowCustomEndpoint: true,
  },
  {
    id: 'custom',
    label: 'Personalizado / Webhook',
    providerType: 'OPENAI_COMPATIBLE',
    defaultEndpointUrl: '',
    defaultModel: '',
    models: [],
    requiresApiKey: false,
    apiKeyPlaceholder: 'Cole sua chave/token (se necessário)...',
    allowCustomEndpoint: true,
  },
]

export const DEFAULT_AI_PRESET: AiProviderPreset = AI_PROVIDER_PRESETS[0]!

export function detectProviderPresetId(endpointUrl: string, providerType: AgentProviderType): string {
  const url = (endpointUrl || '').toLowerCase()
  if (providerType === 'ANTHROPIC' || url.includes('anthropic.com')) return 'anthropic'
  if (providerType === 'GEMINI' || url.includes('generativelanguage.googleapis.com')) return 'gemini'
  if (url.includes('integrate.api.nvidia.com')) return 'nvidia'
  if (url.includes('api.deepseek.com')) return 'deepseek'
  if (url.includes('api.openai.com')) return 'openai'
  if (url.includes('openrouter.ai')) return 'openrouter'
  if (url.includes('localhost:11434') || url.includes('127.0.0.1:11434')) return 'ollama'
  return 'custom'
}
