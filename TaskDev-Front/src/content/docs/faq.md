## Preciso de conta para ler a documentação?

Não. `/docs` é público e abre mesmo quando a API não está disponível. A demonstração também funciona sem conta.

## Preciso configurar IA para usar o Task.dev?

Não. Você pode criar espaços, tarefas e subtarefas manualmente. Só o assistente depende da conexão com um provedor.

## A IA aplica mudanças automaticamente?

O fluxo do chat apresenta propostas para revisão e confirmação. Confira os dados e o espaço de destino antes de aplicar. Veja [assistente de IA](/docs/assistente-ia).

## Posso usar uma IA local?

Há uma predefinição para Ollama. A API precisa alcançar o endpoint informado. Em Docker, o endereço deve apontar para o serviço na rede acessível ao contêiner.

## A lista de modelos é sempre a mesma?

Não. As opções iniciais vêm de predefinições, e a consulta à API pode carregar modelos da sua conta. Limites e disponibilidade dependem do provedor.

## Um espaço compartilhado fica público?

Não. Ele é compartilhado entre pessoas com acesso. O proprietário gerencia membros pelo nome de usuário. Apenas a documentação é pública por proposta de design.

## Concluir a tarefa marca todas as subtarefas?

Sim. A transação de conclusão do backend atual marca todas as subtarefas como concluídas e calcula o XP restante. A interface pode precisar recarregar os dados para refletir todo o checklist. Reabrir a tarefa não desmarca automaticamente as subtarefas.

## Por que o XP muda ao reabrir um item?

A API reverte a recompensa correspondente quando uma conclusão é desfeita. O XP de subtarefas e o do pai são processados separadamente. Leia [gamificação](/docs/gamificacao).

## As preferências acompanham minha conta em outro computador?

Tema e visualização padrão ficam no armazenamento local do navegador. O histórico do chat também é guardado localmente por usuário, com limite de mensagens; não é uma sincronização entre dispositivos.

## O tema claro já está completo?

A opção existe e é persistida, mas vários componentes ainda usam a paleta escura fixa. Consulte [configurações](/docs/configuracoes) para conhecer o estado atual.

## Esqueci minha senha. Como recupero?

Solicite uma redefinição a um administrador ou operador da sua instância. Não há recuperação por e-mail na interface atual.

## Posso usar Neon, Supabase ou PostgreSQL local?

Sim. A API usa PostgreSQL e aceita conexão por URL ou formato Npgsql. Configure a conexão, TLS e permissões de migração de acordo com o banco escolhido.

## Posso instalar como aplicativo? Funciona sem internet?

O frontend tem configuração PWA e pode oferecer instalação em navegadores compatíveis. Isso não torna o banco ou a IA locais: tarefas, autenticação e assistente continuam dependendo dos serviços da sua instância. Não há promessa de sincronização de edições offline.

## Posso hospedar tudo no GitHub Pages?

O Pages serve conteúdo estático. O sistema completo também precisa executar a API .NET e acessar PostgreSQL. O projeto inclui Dockerfiles e Compose para publicar esses componentes juntos ou separadamente.

## Não encontrei minha dúvida

Confira [solução de problemas](/docs/solucao-de-problemas). Para uma falha nova, abra uma issue no repositório com passos de reprodução e mensagens de erro sem credenciais.
