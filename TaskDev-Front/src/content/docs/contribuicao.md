## Preparar uma mudança

Configure o ambiente pelo [guia de instalação](/docs/instalacao), reproduza o problema e mantenha a alteração focada. Não inclua arquivos `.env`, chaves, credenciais de banco ou dados privados em commits e capturas.

## Verificações do projeto

Na raiz do repositório:

```sh
npm ci --prefix TaskDev-Front
npm run build:front
npm run test:back
```

O build do frontend executa `vue-tsc` e Vite. Os testes do backend cobrem, entre outros pontos, regras de gamificação, senhas, criptografia, conexão PostgreSQL e fronteiras da arquitetura.

O workflow `.github/workflows/ci.yml` verifica arquivos versionados, executa testes da API, compila o frontend e valida os modos Docker com um PostgreSQL descartável.

## Integração com contêineres

Em um ambiente com Docker, Bash, OpenSSL, curl e jq:

```sh
bash scripts/unified-smoke-test.sh
```

O teste do modo unificado verifica SPA, respostas da API, login por proxy HTTPS e persistência de sessão depois de recriar o contêiner. Existe também `scripts/compose-smoke-test.sh` para o Compose.

## Atualizar este manual

1. Edite o artigo em `TaskDev-Front/src/content/docs/`.
2. Para adicionar uma página, registre slug, título, grupo e descrição em `catalog.ts`.
3. Coloque capturas autorizadas em `TaskDev-Front/public/docs/screens/` e use nomes descritivos.
4. Referencie artigos com `/docs/assunto` e imagens com `/docs/screens/nome.png`.
5. Use títulos de nível 2 e 3; o sumário do artigo é gerado automaticamente.
6. Execute o build e confira desktop, celular, busca e links.

Evite duplicar o manual inteiro no README. Ele apresenta o projeto e aponta para os guias. Quando uma regra ou fluxo mudar, atualize o artigo correspondente no mesmo conjunto de alterações.

## Enviar uma contribuição

Ao abrir uma issue, descreva o comportamento observado, o esperado e os passos para reproduzir. Em pull requests, explique o resultado da mudança e as verificações executadas. O projeto usa a licença **MIT**, disponível no arquivo `LICENSE` da raiz.
