# Task.dev · Frontend

Interface Vue 3, TypeScript, Pinia, Vue Router e Tailwind CSS. A documentação pública é integrada ao frontend em `/docs`, com artigos em Markdown, busca local e uma demonstração interativa independente da API.

## Executar

Requer Node.js 24 e npm. Na raiz do repositório:

```sh
npm ci --prefix TaskDev-Front
npm run dev:front
```

- Aplicação: [localhost:5173](http://localhost:5173).
- Documentação: [localhost:5173/docs](http://localhost:5173/docs), sem necessidade de backend.
- API de desenvolvimento: proxy de `/api` para `http://localhost:5092`.

Consulte [.env.example](.env.example) para os endereços configuráveis e o [README principal](../README.md) para banco, API e Docker.

## Compilar e verificar

```sh
npm run build:front
```

Executa a checagem TypeScript e gera `dist/`. Para pré-visualizar o build, use `npm --prefix TaskDev-Front run preview`. A hospedagem precisa de fallback para `index.html` nas rotas do frontend, inclusive `/docs/assunto`.

## Organização

| Caminho | Conteúdo |
| --- | --- |
| `src/views` | Telas da aplicação e `DocsView.vue` |
| `src/components` | Componentes reutilizáveis por recurso |
| `src/content/docs` | Artigos Markdown e catálogo de navegação |
| `src/components/docs` | Demonstração local dos controles |
| `src/styles/docs.css` | Layout e estilos da documentação |
| `public/docs/screens` | Capturas ilustrativas dos guias |
| `src/api` | Cliente HTTP e módulos da API |
| `src/stores` | Estado Pinia |
| `src/router` | Rotas e proteção de acesso |

## Editar a documentação

Edite os arquivos `.md` em `src/content/docs`. Para novos artigos, registre o slug e os metadados em `catalog.ts`. Use links `/docs/assunto` e imagens `/docs/screens/arquivo.png`. Os títulos de nível 2 e 3 alimentam o sumário. Verifique também os links e as capturas após cada mudança.

As imagens são carregadas sob demanda e ficam fora do precache de instalação da PWA. A demonstração usa estado local e componentes visuais existentes; não envia requisições nem grava tarefas.
