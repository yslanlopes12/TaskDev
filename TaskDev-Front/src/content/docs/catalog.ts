const sources = import.meta.glob('./*.md', { query: '?raw', import: 'default', eager: true }) as Record<string, string>

export const docGroups = ['Comece aqui', 'Use no dia a dia', 'Para desenvolvedores', 'Ajuda'] as const
export const docPages = [
  { slug: '', title: 'Conheça o Task.dev', group: 'Comece aqui', description: 'Seu trabalho organizado. Seu progresso visível.', file: 'introducao' },
  { slug: 'primeiros-passos', title: 'Primeiros passos', group: 'Comece aqui', description: 'Da criação da conta à sua primeira tarefa concluída.' },
  { slug: 'demonstracao', title: 'Experimente na prática', group: 'Comece aqui', description: 'Um pequeno espaço para aprender fazendo, sem precisar de conta.' },
  { slug: 'espacos', title: 'Espaços e colaboração', group: 'Use no dia a dia', description: 'Separe contextos e trabalhe com outras pessoas.' },
  { slug: 'tarefas', title: 'Tarefas e subtarefas', group: 'Use no dia a dia', description: 'Transforme objetivos em passos que você consegue acompanhar.' },
  { slug: 'visualizacoes', title: 'Lista, Kanban e filtros', group: 'Use no dia a dia', description: 'Escolha a perspectiva certa para o seu fluxo.' },
  { slug: 'assistente-ia', title: 'Assistente de IA', group: 'Use no dia a dia', description: 'Converse, revise a proposta e só então aplique as mudanças.' },
  { slug: 'gamificacao', title: 'XP, níveis e patentes', group: 'Use no dia a dia', description: 'Entenda as regras por trás de cada conquista.' },
  { slug: 'configuracoes', title: 'Configurações e acesso', group: 'Use no dia a dia', description: 'Perfil, preferências, segurança e administração.' },
  { slug: 'instalacao', title: 'Instalação e ambiente', group: 'Para desenvolvedores', description: 'Execute o frontend, a API e o banco no seu ambiente.' },
  { slug: 'arquitetura', title: 'Arquitetura e dados', group: 'Para desenvolvedores', description: 'Como interface, domínio, adaptadores e PostgreSQL se conectam.' },
  { slug: 'api', title: 'Referência da API', group: 'Para desenvolvedores', description: 'Rotas HTTP, sessão, CSRF e contratos de integração.' },
  { slug: 'deploy', title: 'Deploy e operação', group: 'Para desenvolvedores', description: 'Publique e mantenha sua própria instância.' },
  { slug: 'contribuicao', title: 'Testes e contribuição', group: 'Para desenvolvedores', description: 'Verifique mudanças e mantenha código e guias em sintonia.' },
  { slug: 'faq', title: 'Dúvidas frequentes', group: 'Ajuda', description: 'Respostas diretas para as dúvidas mais comuns.' },
  { slug: 'solucao-de-problemas', title: 'Solução de problemas', group: 'Ajuda', description: 'Sintomas, verificações e próximos passos.' },
].map(page => ({ ...page, content: sources[`./${'file' in page ? page.file : page.slug}.md`] ?? '' }))

export function normalizeSearch(value: string) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR')
}
