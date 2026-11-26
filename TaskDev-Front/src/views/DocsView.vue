<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { marked, Renderer } from 'marked'
import DOMPurify from 'dompurify'
import { ArrowRight, ArrowUpRight, BookOpen, Bot, Check, ChevronLeft, ChevronRight, Code2, Copy, FileText, Layers, Menu, Search, Sparkles, X } from 'lucide-vue-next'
import { docGroups, docPages, normalizeSearch } from '@/content/docs/catalog'
import DocsPlayground from '@/components/docs/DocsPlayground.vue'
import '@/styles/docs.css'

const route = useRoute()
const router = useRouter()
const slug = computed(() => String(route.params.slug || ''))
const page = computed(() => docPages.find(item => item.slug === slug.value))
const index = computed(() => docPages.findIndex(item => item.slug === slug.value))
const previous = computed(() => index.value > 0 ? docPages[index.value - 1] : undefined)
const following = computed(() => index.value >= 0 ? docPages[index.value + 1] : undefined)
const query = ref('')
const searchInput = ref<HTMLInputElement>()
const mobileMenu = ref(false)
const copied = ref(false)
const dialog = ref<HTMLDialogElement>()
const enlargedImage = ref({ src: '', alt: '' })
const originalTitle = document.title
let copyTimer: ReturnType<typeof setTimeout> | undefined

const results = computed(() => {
  const terms = normalizeSearch(query.value.trim()).split(/\s+/).filter(Boolean)
  if (!terms.length) return []
  return docPages.filter(item => terms.every(term => normalizeSearch(`${item.title} ${item.description} ${item.content}`).includes(term)))
    .sort((a, b) => Number(terms.some(term => normalizeSearch(b.title).includes(term))) - Number(terms.some(term => normalizeSearch(a.title).includes(term))))
})

const rendered = computed(() => {
  const headings: { id: string; text: string; depth: number }[] = []
  const used = new Map<string, number>()
  const renderer = new Renderer()
  renderer.paragraph = function({ tokens }) {
    const content = this.parser.parseInline(tokens)
    return tokens.length === 1 && tokens[0]?.type === 'image' ? content : `<p>${content}</p>`
  }
  renderer.heading = function({ tokens, depth, text }) {
    const base = normalizeSearch(text).replace(/[^a-z0-9\s-]/g, '').trim().replace(/\s+/g, '-') || 'secao'
    const count = used.get(base) || 0
    used.set(base, count + 1)
    const id = count ? `${base}-${count}` : base
    if (depth === 2 || depth === 3) headings.push({ id, text: text.replace(/[*`_]/g, ''), depth })
    return `<h${depth} id="${id}">${this.parser.parseInline(tokens)}</h${depth}>`
  }
  renderer.image = ({ href, text }) => {
    const escape = (value: string) => value.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    return `<figure><button type="button" class="docs-image-button" aria-label="Ampliar imagem: ${escape(text)}"><img src="${escape(href)}" alt="${escape(text)}" loading="lazy" decoding="async" width="1918" height="912" /></button><figcaption>${escape(text)} · Clique para ampliar</figcaption></figure>`
  }
  const html = DOMPurify.sanitize(marked.parse(page.value?.content || '', { renderer, async: false }) as string)
  return { html, headings }
})

function articlePath(articleSlug: string) { return articleSlug ? `/docs/${articleSlug}` : '/docs' }

async function onArticleClick(event: MouseEvent) {
  if (!(event.target instanceof Element)) return
  const imageButton = event.target.closest('.docs-image-button')
  if (imageButton) {
    const img = imageButton.querySelector('img')
    if (img) { enlargedImage.value = { src: img.src, alt: img.alt }; dialog.value?.showModal() }
    return
  }
  const anchor = event.target.closest('a')
  if (anchor && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey && event.button === 0 && !anchor.target) {
    const url = new URL(anchor.href)
    if (url.origin === location.origin && (url.pathname === '/docs' || url.pathname.startsWith('/docs/'))) {
      event.preventDefault()
      await router.push(url.pathname + url.hash)
    }
  }
}

async function copyLink() {
  try {
    await navigator.clipboard.writeText(location.href)
    copied.value = true
    clearTimeout(copyTimer)
    copyTimer = setTimeout(() => { copied.value = false }, 2000)
  } catch { copied.value = false }
}

function keyboardSearch(event: KeyboardEvent) {
  const target = event.target as HTMLElement | null
  if (target?.matches('input, textarea, select, [contenteditable="true"]') || dialog.value?.open) return
  if (event.key === '/' && !event.ctrlKey && !event.metaKey && !event.altKey) { event.preventDefault(); searchInput.value?.focus() }
}

watch(() => route.fullPath, async () => {
  query.value = ''
  mobileMenu.value = false
  copied.value = false
  document.title = `${page.value?.title || 'Artigo não encontrado'} · Task.dev Docs`
  await nextTick()
  if (route.hash) {
    try { document.getElementById(decodeURIComponent(route.hash.slice(1)))?.scrollIntoView() } catch { /* Invalid hash stays at the article. */ }
  }
}, { immediate: true })

onMounted(() => { window.addEventListener('keydown', keyboardSearch) })
onBeforeUnmount(() => { window.removeEventListener('keydown', keyboardSearch); clearTimeout(copyTimer); document.title = originalTitle })
</script>

<template>
  <div class="docs-shell">
    <a class="docs-skip" href="#docs-main">Pular para o conteúdo</a>
    <header class="docs-topbar">
      <RouterLink to="/docs" class="docs-brand" aria-label="Task.dev documentação, início"><span class="docs-logo">T/</span><strong>Task.dev</strong><span class="docs-brand-separator" /><span class="docs-brand-label">docs</span></RouterLink>
      <div class="docs-search-area">
        <label class="docs-search"><Search :size="17" /><input ref="searchInput" v-model="query" type="search" placeholder="O que você quer aprender?" aria-label="Buscar em toda a documentação" aria-controls="docs-search-results" @keydown.esc="query = ''" /><kbd>/</kbd></label>
        <div v-if="query.trim()" id="docs-search-results" class="docs-search-results" aria-label="Resultados da busca">
          <p role="status">{{ results.length }} {{ results.length === 1 ? 'artigo encontrado' : 'artigos encontrados' }}</p>
          <RouterLink v-for="result in results" :key="result.slug" :to="articlePath(result.slug)" @click="query = ''; mobileMenu = false"><FileText :size="18" /><span><strong>{{ result.title }}</strong><small>{{ result.description }}</small></span><ChevronRight :size="16" /></RouterLink>
          <p v-if="!results.length">Tente “senha”, “Kanban”, “Docker” ou “XP”.</p>
        </div>
      </div>
      <RouterLink to="/login" class="docs-app-link">Abrir aplicativo <ArrowUpRight :size="16" /></RouterLink>
      <button class="docs-menu-toggle" :aria-expanded="mobileMenu" aria-controls="docs-navigation" aria-label="Alternar menu da documentação" @click="mobileMenu = !mobileMenu"><X v-if="mobileMenu" :size="22" /><Menu v-else :size="22" /></button>
    </header>

    <div class="docs-layout">
      <aside id="docs-navigation" class="docs-sidebar" :class="{ 'is-open': mobileMenu }">
        <div class="docs-sidebar-heading"><BookOpen :size="18" /> Central de documentação</div>
        <nav aria-label="Documentação">
          <section v-for="group in docGroups" :key="group" class="docs-nav-group"><h2>{{ group }}</h2><RouterLink v-for="item in docPages.filter(item => item.group === group)" :key="item.slug" :to="articlePath(item.slug)" :class="{ active: item.slug === slug }" :aria-current="item.slug === slug ? 'page' : undefined"><span class="docs-nav-mark" />{{ item.title }}<ChevronRight v-if="item.slug === slug" :size="14" /></RouterLink></section>
        </nav>
        <div class="docs-sidebar-note"><span class="docs-dot" /> Pública. Aberta. Feita para ajudar.<small>Do primeiro acesso ao seu próprio deploy.</small></div>
      </aside>
      <button v-if="mobileMenu" class="docs-menu-backdrop" aria-label="Fechar menu" @click="mobileMenu = false" />

      <main id="docs-main" class="docs-main" tabindex="-1">
        <template v-if="page">
          <div class="docs-breadcrumb"><RouterLink to="/docs">Documentação</RouterLink><ChevronRight :size="13" /><span>{{ page.group }}</span></div>
          <section v-if="!slug" class="docs-hero">
            <div class="docs-hero-copy"><span class="docs-pill"><span class="docs-dot" /> MANUAL DO PROJETO</span><h1>Menos dúvidas.<br /><span>Mais progresso.</span></h1><p>Conheça o Task.dev por dentro. Aprenda a organizar suas tarefas, colaborar e transformar planos em próximos passos.</p><div class="docs-hero-actions"><RouterLink to="/docs/primeiros-passos" class="docs-primary-link">Comece por aqui <ArrowRight :size="17" /></RouterLink><RouterLink to="/docs/demonstracao" class="docs-text-link">Experimentar <Sparkles :size="16" /></RouterLink></div></div>
            <RouterLink to="/docs/visualizacoes" class="docs-hero-preview" aria-label="Conhecer o quadro Kanban"><div class="docs-preview-bar"><span /><span /><span /><small>Seu trabalho, em perspectiva</small></div><img src="/docs/screens/kanban.png" alt="Prévia do quadro Kanban do Task.dev" width="1918" height="912" /><div class="docs-preview-caption"><Layers :size="16" /> Organize. Execute. Evolua.<ArrowUpRight :size="16" /></div></RouterLink>
          </section>
          <header v-else class="docs-article-header"><span class="docs-eyebrow">{{ page.group }}</span><h1>{{ page.title }}</h1><p>{{ page.description }}</p><div class="docs-article-meta"><span><BookOpen :size="14" /> {{ Math.max(1, Math.ceil(page.content.split(/\s+/).length / 180)) }} min de leitura</span><button @click="copyLink"><Check v-if="copied" :size="14" /><Copy v-else :size="14" />{{ copied ? 'Link copiado' : 'Copiar link' }}</button></div></header>
          <div v-if="!slug" class="docs-quick-links"><RouterLink to="/docs/tarefas"><Layers :size="21" /><span><strong>Organize sua rotina</strong><small>Espaços, tarefas e checklists</small></span><ArrowUpRight :size="17" /></RouterLink><RouterLink to="/docs/assistente-ia"><Bot :size="21" /><span><strong>Trabalhe com IA</strong><small>Da conversa à ação revisada</small></span><ArrowUpRight :size="17" /></RouterLink><RouterLink to="/docs/instalacao"><Code2 :size="21" /><span><strong>Execute o projeto</strong><small>Ambiente, código e deploy</small></span><ArrowUpRight :size="17" /></RouterLink></div>
          <details v-if="rendered.headings.length" class="docs-mobile-toc"><summary>Nesta página</summary><a v-for="heading in rendered.headings" :key="heading.id" :href="`#${heading.id}`">{{ heading.text }}</a></details>
          <DocsPlayground v-if="slug === 'demonstracao'" />
          <!-- Only bundled Markdown sanitized with DOMPurify reaches this element. -->
          <article class="docs-prose" @click="onArticleClick" v-html="rendered.html" />
          <nav class="docs-pagination" aria-label="Artigos anterior e próximo"><RouterLink v-if="previous" :to="articlePath(previous.slug)"><ChevronLeft :size="18" /><span><small>Anterior</small><strong>{{ previous.title }}</strong></span></RouterLink><RouterLink v-if="following" :to="articlePath(following.slug)" class="docs-next"><span><small>Continue explorando</small><strong>{{ following.title }}</strong></span><ChevronRight :size="18" /></RouterLink></nav>
        </template>
        <section v-else class="docs-not-found"><span class="docs-eyebrow">404 · DOCUMENTAÇÃO</span><h1>Esse artigo não existe.</h1><p>Use a busca ou escolha um assunto no menu para continuar.</p><RouterLink to="/docs" class="docs-primary-link">Voltar à documentação <ArrowRight :size="17" /></RouterLink></section>
        <footer class="docs-footer"><span><strong>Task.dev</strong> · Documentação do projeto</span><span>Feito para quem constrói. Licença MIT.</span></footer>
      </main>

      <aside v-if="page && rendered.headings.length" class="docs-toc" aria-label="Sumário do artigo"><span>NESTA PÁGINA</span><a v-for="heading in rendered.headings" :key="heading.id" :href="`#${heading.id}`" :class="{ nested: heading.depth === 3 }">{{ heading.text }}</a><div class="docs-toc-help"><BookOpen :size="18" /><strong>Encontrou uma dúvida?</strong><p>O FAQ pode ser seu próximo passo.</p><RouterLink to="/docs/faq">Consultar respostas <ArrowRight :size="14" /></RouterLink></div></aside>
    </div>

    <dialog ref="dialog" class="docs-lightbox" aria-label="Captura de tela ampliada" @click="($event.target === dialog) && dialog?.close()"><button class="docs-lightbox-close" aria-label="Fechar imagem ampliada" @click="dialog?.close()"><X :size="22" /></button><img :src="enlargedImage.src || undefined" :alt="enlargedImage.alt" /><p>{{ enlargedImage.alt }}</p></dialog>
  </div>
</template>
