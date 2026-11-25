<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppButton from '@/components/ui/AppButton.vue'
import { Home } from 'lucide-vue-next'

const router = useRouter()
</script>

<template>
  <div class="min-h-[70vh] flex flex-col items-center justify-center text-center p-6 space-y-4">
    <div class="w-16 h-16 rounded-2xl bg-panel-2 border border-line flex items-center justify-center text-accent text-2xl font-bold font-mono">
      404
    </div>
    <div class="space-y-1">
      <h1 class="text-xl font-bold text-fg">Página não encontrada</h1>
      <p class="text-xs text-muted max-w-sm">
        O link que você seguiu pode estar quebrado ou o recurso pode ter sido removido.
      </p>
    </div>
    <AppButton variant="primary" size="md" @click="router.push('/')">
      <Home class="w-4 h-4 mr-1.5" />
      Voltar para o Início
    </AppButton>
  </div>
</template>
