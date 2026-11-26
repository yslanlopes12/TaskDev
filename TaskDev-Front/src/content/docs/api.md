## Base, contratos e autenticação

A base padrão é **`/api`**. As rotas abaixo são um mapa da implementação atual. Os contratos completos de entrada ficam em `Adapter/InBound/AdapterHttp/DTOs/Request/RequestDTOs.cs` e os de saída em `DTOs/Response/ResponseDTOs.cs`.

Quando habilitado na instância, o Swagger está em **`/swagger`**, com OpenAPI em **`/swagger/v1/swagger.json`**. A imagem unificada o desativa por padrão. A documentação pública não precisa expor nem habilitar o Swagger em produção.

O login cria o cookie `taskdev-session`. Clientes de navegador enviam `credentials: 'include'`. Para escritas, obtenha o token em `GET /api/auth/csrf` e envie o cabeçalho `X-CSRF-TOKEN`. O cliente do frontend também aceita o cookie de token e envia `X-XSRF-TOKEN` como compatibilidade.

## Conta e sessão

| Método | Rota | Uso |
| --- | --- | --- |
| POST | `/auth/register` | Cadastro com nome, sobrenome, fuso e senha opcional |
| POST | `/auth/login` | Início de sessão com usuário e senha |
| POST | `/auth/logout` | Encerramento da sessão |
| GET | `/auth/me` | Usuário atual |
| GET | `/auth/csrf` | Token para operações de escrita |

## Espaços e membros

| Método | Rota | Uso |
| --- | --- | --- |
| GET / POST | `/spaces/` | Listar espaços acessíveis / criar |
| GET / PUT / DELETE | `/spaces/{id}` | Consultar / editar / excluir |
| GET / POST | `/spaces/{id}/members` | Listar / adicionar membro por usuário |
| DELETE | `/spaces/{id}/members/{userId}` | Remover membro |

Editar e excluir o espaço ou gerenciar membros exige propriedade. O acesso às tarefas depende do vínculo com o espaço.

## Tarefas e subtarefas

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/tasks/?spaceId={id}` | Listar tarefas acessíveis; filtro de espaço opcional |
| POST | `/tasks/` | Criar tarefa e subtarefas iniciais |
| GET / PUT / DELETE | `/tasks/{id}` | Consultar / editar / excluir |
| PATCH | `/tasks/{id}/status` | Alterar status |
| PATCH | `/tasks/{id}/move` | Alterar status e posição |
| PATCH | `/tasks/{id}/complete` | Concluir e processar XP |
| PATCH | `/tasks/{id}/uncomplete` | Reabrir e reverter XP do pai |
| GET / POST | `/tasks/{taskId}/subtasks/` | Listar / criar subtarefa |
| PATCH | `/tasks/{taskId}/subtasks/reorder` | Reordenar subtarefas |
| PATCH / PUT | `/subtasks/{id}` | Editar subtarefa |
| PATCH | `/subtasks/{id}/toggle` | Alternar conclusão e ajustar XP |
| DELETE | `/subtasks/{id}` | Excluir subtarefa |

### Exemplo de corpo de criação

Substitua `spaceId` pelo identificador real de um espaço ao qual você tem acesso:

```json
{
  "spaceId": "00000000-0000-0000-0000-000000000000",
  "title": "Estudar Java",
  "description": "Revisar os fundamentos e praticar.",
  "priority": "MEDIUM",
  "dueDate": null,
  "assigneeUserId": null,
  "subtasks": ["Estudar sintaxe", "Praticar lógica"]
}
```

Os status são `PENDING`, `IN_PROGRESS` e `DONE`; as prioridades são `LOW`, `MEDIUM`, `HIGH` e `URGENT`. Datas usam `yyyy-MM-dd`. Não use `PUT` como uma atualização parcial: envie os campos que deseja preservar conforme o contrato.

## Dashboard e progresso

| Método | Rota | Uso |
| --- | --- | --- |
| GET | `/dashboard` | Resumo do trabalho |
| GET | `/progress` | XP, nível e patente |

## Assistente de IA

| Método | Rota | Uso |
| --- | --- | --- |
| GET / PUT / DELETE | `/agent/config` | Consultar / salvar / excluir conexão |
| POST | `/agent/test` | Testar conexão |
| POST | `/agent/models` | Buscar modelos |
| POST | `/agent/preview` | Gerar proposta de tarefas |
| POST | `/agent/chat` | Conversar e obter propostas de ações |

O chat propõe ações; a confirmação na interface executa a alteração através das rotas de tarefas.

## Administração

| Método | Rota | Uso |
| --- | --- | --- |
| POST | `/admin/bootstrap` | Criar primeiro administrador, quando habilitado |
| GET | `/admin/users` | Listar usuários |
| POST | `/admin/users/{id}/reset-password` | Redefinir senha |
| PATCH | `/admin/users/{id}/role` | Alterar papel |
| PATCH | `/admin/users/{id}/active` | Ativar ou desativar conta |

## Respostas e erros

As rotas usam códigos HTTP conforme a operação: `200`/`201` para sucesso, `400` para validação, `401` para ausência de sessão, `403` para acesso recusado, `404` para recurso ausente e `409` para conflitos. Consulte o corpo retornado e o contrato da rota; não presuma um envelope único para todas as respostas.

Para investigar falhas, consulte [solução de problemas](/docs/solucao-de-problemas).
