## Primeiro, configure uma conexão

O Task.dev usa uma conexão de IA configurada por usuário. Você fornece sua própria chave quando o provedor exige uma. O uso de modelos pode ter custos e limites definidos pelo provedor; a organização manual de tarefas não depende dessa conexão.

1. Abra **Configurações → Integração de IA**.
2. Dê um nome à conexão e selecione o provedor.
3. Informe a chave quando necessária e escolha um modelo.
4. Use **Buscar da API** para consultar os modelos disponíveis quando a integração permitir.
5. Para um endpoint próprio, confira as configurações avançadas.
6. Selecione **Testar Conexão**, mantenha **Ativo** marcado e salve.

![Configuração do assistente com provedor, modelo, chave mascarada e teste de conexão](/docs/screens/ia-configuracao.png)

## Integrações presentes no projeto

A interface inclui opções para NVIDIA NIM, DeepSeek, OpenAI, Anthropic, Gemini, OpenRouter, Ollama e conexão personalizada. Os modelos sugeridos na interface são predefinições do código; a disponibilidade efetiva depende da sua conta e da resposta do provedor.

Endpoints externos precisam usar **HTTPS**. A API bloqueia destinos privados, reservados e redirecionamentos. O acesso ao Ollama em `localhost:11434` exige `Agent__AllowLocalOllama=true` no servidor e é destinado a instalações confiáveis. Em Docker, `localhost` aponta para o próprio contêiner, não para o computador que hospeda o Ollama; endereços privados arbitrários não são liberados por essa opção.

## Peça com contexto

Abra **Assistente IA** na barra superior. Exemplos de pedidos:

> Crie uma tarefa “Estudar Java” no espaço Geral, com subtarefas de sintaxe, lógica, POO e um projeto prático.

> Altere a prioridade da tarefa “Revisar API” para alta.

> Organize as etapas para preparar uma apresentação do meu projeto.

Informe o espaço e o título exato quando houver tarefas parecidas. A resposta de um modelo pode precisar de ajustes; revise títulos, datas, prioridade e destino.

## Revise antes de aplicar

O chat prepara uma proposta. Para criação, o cartão mostra título, descrição, subtarefas e espaço de destino. Ajuste os campos disponíveis e clique em **Criar tarefa** para aplicar. Se a proposta estiver errada, cancele e explique a correção.

![Proposta da IA aguardando revisão e confirmação da criação](/docs/screens/ia-revisao.png)

Após confirmar, o cartão informa a criação e a tarefa aparece no espaço. Edições e exclusões propostas também passam pelo fluxo de revisão. A aplicação executa as mudanças por suas APIs de tarefas, respeitando o acesso do usuário.

## Dados e credenciais

A chave do provedor é armazenada cifrada no servidor com AES-GCM. As senhas de conta usam hash Argon2id, um mecanismo diferente. O conteúdo enviado ao assistente e o contexto anexado à requisição saem da sua instância em direção ao endpoint configurado. Considere essa comunicação ao escolher o provedor e ao escrever seus pedidos.

Se o teste falhar, veja [problemas com IA](/docs/solucao-de-problemas#assistente-de-ia-nao-responde).
