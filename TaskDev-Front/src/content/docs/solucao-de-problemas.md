## Frontend abre, mas tarefas não carregam

Confirme que a API está rodando e acessível. Em desenvolvimento, o proxy padrão aponta para `http://localhost:5092`; confira `VITE_API_TARGET` se a porta for outra. Confira também a conexão do PostgreSQL e os logs da API. Reinicie o Vite após mudar o arquivo de ambiente.

## O build diz que run-p ou vite não foi encontrado

Instale as dependências antes de compilar:

```sh
npm ci --prefix TaskDev-Front
npm run build:front
```

Confira se sua versão do Node atende ao projeto. Erros `ENOTFOUND` indicam falha de resolução de rede; valide acesso ao registro npm e a configuração de proxy do seu ambiente.

## Login falha ou a sessão não permanece

Use o nome de usuário devolvido no cadastro, não um e-mail presumido. Confirme que a conta está ativa. Em HTTP local, `Security__SecureCookies=true` impede o envio do cookie seguro; use `false` apenas para esse ambiente. Em produção, prefira HTTPS na mesma origem da interface.

Se a sessão desaparece depois de recriar o contêiner, revise a persistência das chaves de Data Protection. Caso tenha perdido a senha, procure um administrador ou operador.

## Erro de CSRF ao salvar

Confirme que o navegador está enviando cookies e o token CSRF. Em integrações próprias, obtenha o token em `/api/auth/csrf` e envie o cabeçalho esperado. Revise origem, HTTPS e proxy antes de desativar proteções.

## Espaço ausente ou permissão negada

Peça ao proprietário para conferir seu nome de usuário e o vínculo de membro. Editar configurações, excluir espaços ou gerenciar participantes exige propriedade. Atribuir uma tarefa exige que o responsável tenha acesso ao espaço.

## Não consigo arrastar as tarefas

Selecione **Todas**, limpe a busca e os filtros de prioridade e volte para **Ordem manual**. As outras ordenações e os filtros restringem a reorganização manual.

## Assistente de IA não responde

1. Confira se a conexão está ativa e salva.
2. Execute **Testar Conexão** nas configurações.
3. Confira chave, modelo, limites e disponibilidade da sua conta no provedor.
4. Verifique se o endpoint é acessível a partir do servidor da API.
5. Endpoints externos devem usar HTTPS e não podem redirecionar. Para Ollama, habilite `Agent__AllowLocalOllama=true` em uma instalação confiável e use `localhost:11434` a partir da API; em Docker, `localhost` refere-se ao contêiner.
6. Se a chave de criptografia da instância mudou, salve novamente a credencial de IA com a configuração correta.

Uma chave mascarada indica uma credencial salva; o texto mascarado não deve ser enviado como chave em uma integração própria.

## Banco não inicia ou as migrações falham

Se aparecer **Configure DATABASE_URL ou ConnectionStrings__DefaultConnection**, a API ainda não recebeu uma conexão. Copie `.env.example` para `.env` na raiz, preencha `DATABASE_URL` e `AGENT_ENCRYPTION_KEY` e reinicie com `npm run dev:back`. O script carrega esse arquivo automaticamente. Para `dotnet run` direto, configure as variáveis no terminal ou em User Secrets.

Valide host, porta, usuário, senha e banco em `DATABASE_URL`. A conta precisa conseguir criar os objetos do schema. Quando usar pool de conexões, considere `DATABASE_URL_UNPOOLED` para migrações e confira as exigências de TLS do provedor.

## Artigo de documentação dá 404 ao recarregar

O servidor precisa de fallback de SPA para `index.html` em rotas como `/docs/instalacao`. Preserve o atendimento de arquivos estáticos em `/docs/screens/` e reserve `/api` para a API. O Nginx e o modo unificado do projeto já contemplam esse comportamento.

## Como relatar uma falha

Informe a página, o resultado esperado, os passos e o erro observado. Inclua a versão ou commit da instância quando possível. Capture somente os dados necessários e oculte senhas, tokens, conexão do banco e informações privadas da equipe.
