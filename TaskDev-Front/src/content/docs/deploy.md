## Uma origem para aplicação e documentação

Ao publicar o frontend, o manual fica disponível em **`/docs`** e os artigos em **`/docs/{assunto}`**. O servidor deve entregar `index.html` ao acessar diretamente uma rota do frontend. Os arquivos de imagem em `/docs/screens/` devem ser servidos como arquivos estáticos.

O modo unificado e o Nginx do projeto já incluem fallback de SPA. O GitHub Pages pode servir arquivos estáticos, mas não executa a API .NET nem o PostgreSQL; o deploy completo exige esses serviços.

## Contêiner unificado

O `Dockerfile` da raiz compila Vue e API e reúne ambos numa imagem que expõe a porta 8080. Com o `.env` configurado:

```sh
docker build -t taskdev:unified .
docker run --rm --env-file .env -e Security__SecureCookies=false -p 127.0.0.1:8080:8080 taskdev:unified
```

Abra `http://localhost:8080/docs`. O comando acima usa HTTP local. Em produção, configure HTTPS no proxy público e ative `Security__SecureCookies=true`.

## Docker Compose

```sh
docker compose up --build -d
docker compose ps
docker compose logs --tail=100
```

Quando usar o PostgreSQL opcional, inclua `-f compose.yaml -f compose.postgres.yaml` em todos os comandos do Compose. Preserve os volumes ao atualizar; não use `down -v` se precisar manter os dados.

## Primeiro administrador

Em ambiente privado, habilite temporariamente `Admin__BootstrapEnabled=true`, reaplique o deploy e envie:

```sh
curl --fail-with-body --json '{"firstName":"Seu","lastName":"Nome"}' http://localhost:8080/api/admin/bootstrap
```

A resposta contém `username` e `generatedPassword`. Guarde as credenciais. Desative o bootstrap e reaplique a configuração antes de expor a instância. Em Development, o endpoint é habilitado por padrão.

## Publicação separada

```sh
dotnet publish TaskDev-Back/TaskDev-Back/TaskDev-Back.csproj -c Release -o artifacts/api
npm run build:front
```

Sirva `TaskDev-Front/dist` com fallback de SPA e execute `dotnet artifacts/api/TaskDev-Back.dll`. Prefira um proxy de `/api` no mesmo domínio. Para outro subdomínio do mesmo site, configure `VITE_API_BASE_URL` antes do build e a origem exata em `FrontendUrl` da API.

## Configurações que precisam permanecer estáveis

- **Banco:** migrações são aplicadas automaticamente; faça backup antes de atualizações importantes.
- **Chave de criptografia:** mantenha `AGENT_ENCRYPTION_KEY` para continuar lendo as credenciais de IA salvas.
- **Data Protection:** preserve as chaves de sessão conforme o modo de deploy para evitar invalidar sessões em recriações.
- **Proxy:** configure apenas os IPs ou redes confiáveis para cabeçalhos encaminhados.
- **Segredos:** injete-os no servidor; não os coloque no Git nem em variáveis `VITE_*`.

## Verificar uma publicação

1. Abra `/docs` sem login e acesse diretamente um artigo, como `/docs/instalacao`.
2. Recarregue o artigo e abra uma captura.
3. Faça login, abra um espaço e crie uma tarefa de teste.
4. Verifique se a sessão e os dados persistem após a atualização prevista.
5. Se usar IA, teste a conexão na instância publicada.
