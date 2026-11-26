## Requisitos

Para desenvolvimento separado, use **Node.js 24**, npm, **.NET SDK 10** e PostgreSQL. Para executar com contêineres, use Docker com Compose v2 e um banco externo ou o PostgreSQL opcional do Compose.

## Opção A: Docker Compose

Na raiz do repositório, copie o exemplo de ambiente:

```sh
cp .env.example .env
```

No PowerShell, o equivalente é `Copy-Item .env.example .env`. Preencha o arquivo local com a conexão do seu banco e uma chave de criptografia estável.

| Variável | Finalidade |
| --- | --- |
| `DATABASE_URL` | Conexão PostgreSQL, por URL ou string Npgsql |
| `DATABASE_URL_UNPOOLED` | Conexão direta opcional para migrações |
| `AGENT_ENCRYPTION_KEY` | 32 bytes em Base64 para cifrar credenciais de IA |
| `POSTGRES_PASSWORD` | Senha do banco quando usar o Compose com PostgreSQL local |
| `Security__SecureCookies` | `false` no HTTP local; `true` na publicação HTTPS |
| `Admin__BootstrapEnabled` | Habilitação temporária da criação do primeiro administrador |

Gere uma chave com `openssl rand -base64 32`. Em PowerShell moderno:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Com banco externo:

```sh
docker compose up --build -d
```

Com banco no próprio Compose, configure também `POSTGRES_PASSWORD` com uma senha aleatória hexadecimal:

```sh
docker compose -f compose.yaml -f compose.postgres.yaml up --build -d
```

Abra `http://localhost:8080`. Na inicialização, o DbUp aplica as migrações versionadas; a conta do banco precisa ter permissão para criar os objetos do schema.

## Opção B: desenvolvimento separado

Configure `DATABASE_URL` e `AGENT_ENCRYPTION_KEY` no `.env` da raiz, no ambiente da API ou via User Secrets. O comando `npm run dev:back` carrega automaticamente os valores não vazios do `.env`; variáveis já definidas no terminal têm prioridade. O Compose também usa esse arquivo. Se executar `dotnet run` diretamente, use o ambiente ou User Secrets, pois esse comando não passa pelo carregador do projeto.

Terminal da API, na raiz:

```sh
npm run dev:back
```

Terminal do frontend:

```sh
npm ci --prefix TaskDev-Front
npm run dev:front
```

A interface abre em `http://localhost:5173` e o proxy de desenvolvimento encaminha `/api` para `http://localhost:5092`. Os scripts da API usam `.taskdev-tools/dotnet10` se houver um SDK local; caso contrário, usam `dotnet` do PATH.

## Consultar apenas a documentação

O `/docs` não depende de API ou banco. Instale e execute somente o frontend e abra `http://localhost:5173/docs`. O exemplo interativo também funciona sem backend.

## Endereços do frontend

Consulte `TaskDev-Front/.env.example`. `VITE_API_TARGET` configura o proxy de desenvolvimento; `VITE_API_BASE_URL` define a URL pública usada pelo navegador no build. Variáveis `VITE_*` são públicas e nunca devem conter senhas, chaves de IA ou conexão do banco.

Continue com [deploy e operação](/docs/deploy) para publicar sua instância.
