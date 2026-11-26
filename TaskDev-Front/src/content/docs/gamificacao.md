## Cada conclusão conta

A barra lateral mostra seu XP acumulado, nível e patente. O servidor calcula as recompensas e registra os eventos no banco. O feedback visual informa quando há ganho de XP.

![Subtarefas concluídas, contador atualizado e aviso de ganho de XP](/docs/screens/xp.png)

## XP por tarefa

| Prioridade | Valor-base da tarefa |
| --- | ---: |
| Baixa | 15 XP |
| Média | 30 XP |
| Alta | 50 XP |
| Urgente | 80 XP |

Ao concluir o pai, a API soma os eventos de XP da tarefa e concede apenas o restante até o valor-base, com mínimo de zero. Também marca todas as subtarefas como concluídas. Concluir novamente uma tarefa que já recebeu a recompensa não duplica esse prêmio. Reabrir reverte o evento de conclusão do pai, mas mantém os eventos das subtarefas e não desmarca o checklist.

## XP por subtarefa

O valor-base da tarefa é dividido entre as subtarefas existentes. A divisão usa inteiros; o último item recebe o saldo restante, com mínimo de 1 XP por item. Por isso, em casos com mais subtarefas do que o valor-base, a soma pode ultrapassar o valor do pai.

**Exemplo atual:** prioridade média e seis subtarefas resultam em **5 XP por subtarefa**. Marcar duas concede 10 XP. Concluir o pai concede os **20 XP restantes**, totalizando 30 XP para esse fluxo, e marca todos os itens. As capturas fornecidas mostram uma versão anterior com indicadores diferentes; os valores do código atual são os descritos aqui.

Desmarcar um item reverte o XP desse item. Alterações posteriores na prioridade ou na quantidade de subtarefas influenciam os próximos cálculos; eventos já registrados mantêm seu próprio histórico.

## Níveis e patentes

O nível é calculado por `floor(XP total / 100) + 1`, com nível mínimo 1. Cada novo bloco de 100 XP avança um nível.

| Patente | Nível a partir de |
| --- | ---: |
| E | 1 |
| D | 3 |
| C | 6 |
| B | 12 |
| A | 21 |
| S | 35 |

As regras ficam em `Domain/UseCases/GamificationRules.cs`. O [exemplo interativo](/docs/demonstracao) permite observar a combinação entre checklist, conclusão e XP.
