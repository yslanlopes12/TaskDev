## Visão de conjunto

```text
Navegador · Vue + TypeScript + Pinia
                 ↓ HTTP /api
Entrada · Minimal APIs + DTOs + validação + sessão
                 ↓ Ports de entrada
Domínio · Casos de uso + regras de acesso e XP
                 ↓ Ports de saída
Adaptadores · Dapper / PostgreSQL e clientes de IA
```

O backend segue uma organização hexagonal: os casos de uso dependem de contratos, e os adaptadores implementam a comunicação com banco e provedores externos. A configuração de infraestrutura registra as dependências e compõe o aplicativo.

## Mapa do repositório

| Diretório | Responsabilidade |
| --- | --- |
| `TaskDev-Front/src/views` | Telas e composição das páginas |
| `TaskDev-Front/src/components` | Componentes de interface e recursos |
| `TaskDev-Front/src/stores` | Estado de sessão, tarefas, espaços, chat e progresso |
| `TaskDev-Front/src/api` | Chamadas HTTP e tratamento de CSRF |
| `TaskDev-Front/src/content/docs` | Artigos Markdown e catálogo desta documentação |
| `TaskDev-Front/public/docs/screens` | Capturas usadas pelos guias |
| `TaskDev-Back/TaskDev-Back/Domain` | Entidades, modelos, portas e casos de uso |
| `TaskDev-Back/TaskDev-Back/Adapter` | Endpoints, DTOs, repositórios e clientes |
| `TaskDev-Back/TaskDev-Back/Infra` | Configuração e composição das dependências |
| `TaskDev-Back/TaskDev-Back/database/migrations` | Migrações incorporadas à API |
| `TaskDev-Back/TaskDev.Tests` | Testes automatizados |
| `docs/architecture.md` | Detalhamento técnico e diagrama de entidades |

## Modelo de dados

- **users:** identidade, papel, estado ativo, fuso e versão de autenticação.
- **spaces:** proprietário, nome, ícone, cor e visibilidade.
- **space_members:** vínculo entre usuários e espaços.
- **tasks:** objetivo, responsável, prioridade, prazo, status e posição.
- **subtasks:** itens do checklist, ordem e conclusão.
- **xp_events:** histórico de recompensas.
- **agent_configs:** configuração individual do provedor e credenciais cifradas.

As chaves de Data Protection usadas pelas sessões podem ser persistidas no schema `taskdev_security` do PostgreSQL no modo unificado. O documento de arquitetura do repositório contém o ERD e mais detalhes de operação.

## Caminho de uma operação

1. A tela chama um módulo em `src/api`.
2. O cliente envia o cookie de sessão e, nas escritas, o token CSRF.
3. O endpoint valida o pedido e identifica o usuário.
4. O caso de uso verifica o acesso ao espaço e aplica a regra de negócio.
5. O adaptador executa o SQL parametrizado ou a chamada ao provedor.
6. A resposta atualiza a interface e os indicadores de progresso.

## Segurança e fronteiras

Senhas usam **Argon2id**; chaves de IA usam **AES-GCM**. A sessão usa um cookie HTTP-only protegido por Data Protection. A autorização é aplicada no servidor e não depende somente da visibilidade dos botões.

A documentação tem layout próprio e pula a inicialização da sessão ao ser acessada diretamente. Artigos são carregados como texto durante o build, convertidos em HTML com `marked` e sanitizados com `DOMPurify`. O exemplo interativo mantém o estado apenas em memória.

## Formas de execução

O Compose usa Nginx no frontend e a API em outro contêiner. A imagem unificada usa Kestrel para servir o build Vue e a API na mesma origem. Veja [deploy](/docs/deploy).
