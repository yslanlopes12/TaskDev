# Task.dev

Gerenciador de tarefas de código aberto para desenvolvedores e pequenas equipes. O projeto inclui espaços compartilhados, Kanban, listas de tarefas, subtarefas e um assistente de IA com revisão antes de criar, editar ou excluir tarefas.

A interface usa Vue, TypeScript e Vite. A API usa ASP.NET Core 10, Dapper e PostgreSQL. Você fornece o seu próprio banco e, se quiser usar o chat, a sua chave de um provedor de IA.

## Configuração do banco

Crie um banco PostgreSQL para o Task.dev. Na primeira inicialização, a API aplica as migrações versionadas com DbUp. Use uma conta que tenha acesso ao banco e permissão para criar os objetos do schema.

| Variável da API | Uso |
| --- | --- |
| `DATABASE_URL` | URL `postgresql://` do seu banco. Também aceita uma string de conexão Npgsql. |
| `DATABASE_URL_UNPOOLED` | Conexão opcional para migrações. Quando vazia, a API usa a conexão normal. |
| `AGENT_ENCRYPTION_KEY` | Chave estável de 32 bytes em Base64, usada para proteger as chaves dos provedores de IA no banco. |
| `Admin__BootstrapEnabled` | Habilita temporariamente a criação do primeiro administrador fora de Development. |
| `Security__SecureCookies` | Configure como `true` ao publicar a interface e a API por HTTPS. |

- **Neon**. Copie a conexão do painel **Connect**. Você pode usar a conexão com pool em `DATABASE_URL` e a direta em `DATABASE_URL_UNPOOLED`. [Documentação do Neon](https://neon.com/docs/connect/connect-from-any-app).
- **Supabase**. Copie a conexão PostgreSQL direta no painel **Connect**. Em uma rede somente IPv4, use o **Session pooler** quando a conexão direta não estiver disponível. [Documentação do Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres).
- **PostgreSQL local ou outro provedor**. Informe host, porta, banco, usuário e senha. Siga os requisitos TLS do provedor. Para uma instância local sem TLS, informe `sslmode=disable` na URL.

## Executar em um único contêiner

O `Dockerfile` da raiz compila o Vue com Node.js 24, publica a API com o SDK .NET 10 e reúne ambos em uma imagem compacta do ASP.NET Runtime 10 que preserva as bibliotecas de idiomas e fusos horários. O Kestrel roda sem privilégios de root e serve a interface em `/` e a API em `/api` pela porta HTTP `8080`. Use um banco PostgreSQL externo, como Neon ou Supabase.

Com `DATABASE_URL` e `AGENT_ENCRYPTION_KEY` no `.env` da raiz, execute:

```bash
docker build -t taskdev:unified .
docker run --rm --env-file .env -e Security__SecureCookies=false -p 127.0.0.1:8080:8080 taskdev:unified
```

Abra `http://localhost:8080`. O comando desativa o atributo `Secure` para o teste HTTP local. A imagem ativa cookies seguros e desativa o Swagger por padrão. O frontend da imagem usa `/api` na mesma origem. As configurações do Vite para desenvolvimento e publicação separada continuam disponíveis pelos comandos correspondentes.

### Publicar no Northflank

1. Crie um **Combined Service** vinculado ao repositório. Selecione Dockerfile, use `Dockerfile` e contexto `.` e mantenha uma instância. Confira a franquia do plano Sandbox e o consumo do serviço no painel. [Guia de deploy](https://northflank.com/docs/v1/application/getting-started/build-and-deploy-your-code) · [Planos do Northflank](https://northflank.com/pricing).
2. Configure uma porta pública **HTTP** em `8080`. Use o endereço HTTPS fornecido pelo Northflank e configure a verificação de saúde HTTP em `/api/health`. O proxy da plataforma termina o TLS e fala HTTP com o contêiner. [Rede do Northflank](https://northflank.com/docs/v1/application/network/networking-on-northflank).
3. Em **Runtime variables** ou em um **Secret Group**, defina `DATABASE_URL` e `AGENT_ENCRYPTION_KEY`. Se tiver uma conexão direta para o DbUp, defina também `DATABASE_URL_UNPOOLED`. Use `Security__SecureCookies=true`, `Swagger__Enabled=false` e mantenha `Admin__BootstrapEnabled=false` após criar o primeiro administrador por um acesso restrito. Quando o provedor oferecer verificação de CA e hostname, use `sslmode=verify-full` na conexão PostgreSQL.
4. Configure `ForwardedHeaders__KnownProxies__0` com o IP do proxy que se conecta diretamente ao Kestrel ou `ForwardedHeaders__KnownNetworks__0` com um CIDR exclusivo dos proxies confiáveis. Confirme esse endereço ou intervalo com o operador do ambiente e use índices `__1`, `__2` e assim por diante se houver mais entradas. A aplicação só habilita esse encaminhamento com uma lista explícita e aceita um salto de `X-Forwarded-For` e `X-Forwarded-Proto`. Mantenha `ASPNETCORE_FORWARDEDHEADERS_ENABLED` desativada. [Cabeçalhos encaminhados no ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0).
5. Acesse `/api/health` por HTTPS e confira os atributos `Secure`, `HttpOnly` e `SameSite=Lax` no cookie `taskdev-session` após o login. Reimplante o serviço e confirme que `/api/auth/me` ainda reconhece a sessão no mesmo domínio.

No deploy unificado, o DbUp cria `taskdev_security.data_protection_keys` no PostgreSQL. O ASP.NET usa esse repositório para manter as sessões após a recriação do contêiner, sem volume. As chaves são criptografadas com uma chave derivada de `AGENT_ENCRYPTION_KEY`. Preserve esse segredo, o banco e as chaves armazenadas entre deploys. Se a conexão de migração usa outro usuário PostgreSQL, conceda ao usuário da aplicação `USAGE` no schema `taskdev_security` e `SELECT, INSERT` na tabela. Se `Database__MigrateOnStartup=false`, aplique a migração antes do deploy. A primeira migração do armazenamento de chaves em arquivo para o PostgreSQL exige um novo login.

## Executar com Docker Compose

Requer Docker Engine ou Docker Desktop com Compose v2. Na raiz do repositório:

```bash
cp .env.example .env
```

Para gerar a chave da API, use `openssl rand -base64 32`. No PowerShell, use `[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`. Salve a chave em `AGENT_ENCRYPTION_KEY` no seu `.env`.

**Com Neon, Supabase ou outro banco externo**, preencha `DATABASE_URL` e execute:

```bash
docker compose up --build -d
```

**Com um PostgreSQL no próprio Compose**, preencha `POSTGRES_PASSWORD` com uma senha aleatória em hexadecimal, por exemplo, gerada por `openssl rand -hex 24`. Execute:

```bash
docker compose -f compose.yaml -f compose.postgres.yaml up --build -d
```

Abra [http://localhost:8080](http://localhost:8080). O Compose publica a interface no loopback. O Nginx encaminha `/api` para a API. O banco local e as chaves de sessão usam volumes.

### Primeiro administrador

Em um ambiente privado, habilite `Admin__BootstrapEnabled=true` e reaplique o mesmo comando do Compose. Crie o primeiro administrador pela API:

```bash
curl --fail-with-body --json '{"firstName":"Seu","lastName":"Nome"}' \
  http://localhost:8080/api/admin/bootstrap
```

Guarde os campos `username` e `generatedPassword` da resposta para entrar na interface. Depois, volte a opção para `false` e reaplique o Compose antes de disponibilizar a instância publicamente. Com a API rodando localmente em Development, o endpoint também fica disponível na porta `5092`.

### Imagens independentes

Os Dockerfiles também podem ser usados individualmente:

```bash
docker build -t taskdev-api TaskDev-Back/TaskDev-Back
docker build -t taskdev-web TaskDev-Front
```

A API escuta em `8080` no contêiner. A interface escuta em `80`. Em redes ou plataformas separadas, defina `TASKDEV_API_UPSTREAM` no contêiner web com a URL interna da API, sem acrescentar `/api`. Injete as variáveis privadas somente no contêiner da API.

Para usar as duas imagens com um PostgreSQL externo já configurado no seu `.env`:

```bash
docker network create taskdev
docker run -d --name api --network taskdev --env-file .env \
  -v taskdev-session-keys:/home/app/.aspnet/DataProtection-Keys taskdev-api
docker run -d --name taskdev-web --network taskdev \
  -p 127.0.0.1:8080:80 taskdev-web
```

## Executar frontend e API separadamente

Requer .NET SDK 10, Node.js 24, npm e um PostgreSQL próprio. Configure `DATABASE_URL` e `AGENT_ENCRYPTION_KEY` no ambiente da API ou em [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets). O `.env` da raiz é lido pelo Compose.

Em um terminal com as credenciais configuradas, execute:

```bash
npm run dev:back
```

Os scripts npm da API usam o SDK local em `.taskdev-tools/dotnet10`, quando ele está disponível. Em outros ambientes, usam o `dotnet` do PATH.

Em outro terminal:

```bash
npm ci --prefix TaskDev-Front
npm run dev:front
```

Acesse `http://localhost:5173`. O Vite encaminha `/api` para `http://localhost:5092`. Para outro endereço da API em desenvolvimento, ajuste `VITE_API_TARGET` no arquivo local do frontend, usando [o exemplo](TaskDev-Front/.env.example).

Para publicar separadamente:

```bash
dotnet publish TaskDev-Back/TaskDev-Back/TaskDev-Back.csproj -c Release -o artifacts/api
npm run build:front
```

Sirva `TaskDev-Front/dist` com fallback de SPA para `index.html` e execute a API com `dotnet artifacts/api/TaskDev-Back.dll`. Prefira HTTPS e um proxy para `/api` no mesmo domínio da interface. Se a API ficar em outro subdomínio do mesmo site, configure `VITE_API_BASE_URL` antes do build e `FrontendUrl` na API com a origem HTTPS exata da interface.

## Segredos

Copie os arquivos `.env.example` e preencha as cópias locais ou use o gerenciador de segredos da sua plataforma. Mantenha `.env`, `app.json`, `appsettings*.json`, certificados privados e a configuração local do Neon fora do Git. Variáveis `VITE_*` são visíveis no navegador. Reserve-as para URLs e opções públicas.

Mantenha a mesma chave de criptografia ao atualizar a API. Se uma instalação antiga usava a chave de desenvolvimento padrão, gere uma chave nova e salve novamente as credenciais dos provedores de IA no painel de configurações.

## Testes e contribuições

```bash
npm run test:back
npm ci --prefix TaskDev-Front
npm run build:front
```

O [workflow de CI](.github/workflows/ci.yml) roda em pushes e pull requests. Ele verifica os arquivos versionados, executa os testes da API, compila a interface e valida os dois modos Docker com PostgreSQL descartável. Para rodar a integração do contêiner unificado na raiz, use `bash scripts/unified-smoke-test.sh` em um ambiente com Docker, OpenSSL, curl e jq. O script verifica o fallback da SPA, os códigos da API, o login atrás de um proxy HTTPS e a persistência da sessão após a recriação do contêiner. As credenciais são geradas na própria execução.

Contribuições são bem-vindas por issues e pull requests. Inclua passos para reproduzir problemas e o resultado dos testes quando fizer mudanças.

## Licença

[MIT](LICENSE).
