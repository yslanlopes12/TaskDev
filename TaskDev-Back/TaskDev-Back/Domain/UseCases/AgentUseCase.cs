using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed partial class AgentUseCase : IAgentUseCase
{
    private const string EditSpaceQuestion = "Em qual espaço está a tarefa que você quer editar?";
    private const string EditTargetQuestion = "Qual tarefa desse espaço você quer editar?";
    private const string EditChangesQuestion = "O que você quer alterar nessa tarefa?";
    private const int ChatContextMessageLimit = 60;

    private readonly IAgentConfigRepository _agentConfigRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IAgentHttpClient _agentHttpClient;
    private readonly ISecretEncryptor _secretEncryptor;

    public AgentUseCase(
        IAgentConfigRepository agentConfigRepository,
        ISpaceRepository spaceRepository,
        ITaskRepository taskRepository,
        IAgentHttpClient agentHttpClient,
        ISecretEncryptor secretEncryptor)
    {
        _agentConfigRepository = agentConfigRepository;
        _spaceRepository = spaceRepository;
        _taskRepository = taskRepository;
        _agentHttpClient = agentHttpClient;
        _secretEncryptor = secretEncryptor;
    }

    public async Task<BaseReturn<AgentConfig?>> GetConfigAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var config = await _agentConfigRepository.GetByUserIdAsync(userId);
        return BaseReturn<AgentConfig?>.Success(config);
    }

    public async Task<BaseReturn<AgentConfig>> SaveConfigAsync(SaveAgentConfigCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.EndpointUrl))
        {
            return BaseReturn<AgentConfig>.ValidationError("Nome da conexão e URL do endpoint são obrigatórios.");
        }

        var existing = await _agentConfigRepository.GetByUserIdAsync(command.UserId);

        string? encryptedKey = null;
        if (!string.IsNullOrWhiteSpace(command.ApiKey))
        {
            encryptedKey = _secretEncryptor.Encrypt(command.ApiKey.Trim());
        }
        else if (existing != null)
        {
            encryptedKey = existing.EncryptedApiKey;
        }

        var config = new AgentConfig
        {
            UserId = command.UserId,
            Name = command.Name.Trim(),
            EndpointUrl = command.EndpointUrl.Trim(),
            ProviderType = EnumAgentProviderType.IsValid(command.ProviderType) ? command.ProviderType : EnumAgentProviderType.OpenAiCompatible,
            Model = command.Model?.Trim(),
            EncryptedApiKey = encryptedKey,
            IsEnabled = command.IsEnabled,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _agentConfigRepository.UpsertAsync(config);
        return BaseReturn<AgentConfig>.Success(config);
    }

    public async Task<BaseReturn<bool>> DeleteConfigAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _agentConfigRepository.DeleteAsync(userId);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> TestConnectionAsync(Guid userId, string? endpointUrl, string? apiKey, string? providerType, string? model, CancellationToken cancellationToken = default)
    {
        string finalUrl;
        string? finalKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        string finalType = providerType ?? EnumAgentProviderType.OpenAiCompatible;
        string? finalModel = model;

        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            var config = await _agentConfigRepository.GetByUserIdAsync(userId);
            if (config == null)
            {
                return BaseReturn<bool>.ValidationError("Nenhum agente configurado para teste.");
            }

            finalUrl = config.EndpointUrl;
            finalType = config.ProviderType;
            finalModel = config.Model;
            if (finalKey == null && !string.IsNullOrWhiteSpace(config.EncryptedApiKey))
            {
                finalKey = _secretEncryptor.Decrypt(config.EncryptedApiKey);
            }
        }
        else
        {
            finalUrl = endpointUrl.Trim();
            if (finalKey == null)
            {
                var existing = await _agentConfigRepository.GetByUserIdAsync(userId);
                if (existing != null && !string.IsNullOrWhiteSpace(existing.EncryptedApiKey))
                {
                    finalKey = _secretEncryptor.Decrypt(existing.EncryptedApiKey);
                }
            }
        }

        try
        {
            var isOk = await _agentHttpClient.TestConnectionAsync(finalUrl, finalKey, finalType, finalModel, cancellationToken);
            return isOk
                ? BaseReturn<bool>.Success(true)
                : BaseReturn<bool>.BusinessError("Falha na validação de resposta do agente.");
        }
        catch (Exception ex)
        {
            return BaseReturn<bool>.BusinessError($"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<BaseReturn<IReadOnlyList<SuggestedTaskModel>>> PreviewTasksAsync(Guid userId, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BaseReturn<IReadOnlyList<SuggestedTaskModel>>.ValidationError("Descreva o que precisa fazer.");
        }

        var config = await _agentConfigRepository.GetByUserIdAsync(userId);
        if (config == null || !config.IsEnabled)
        {
            return BaseReturn<IReadOnlyList<SuggestedTaskModel>>.BusinessError("Nenhum agente de IA ativo configurado.");
        }

        string? decryptedKey = null;
        if (!string.IsNullOrWhiteSpace(config.EncryptedApiKey))
        {
            decryptedKey = _secretEncryptor.Decrypt(config.EncryptedApiKey);
        }

        try
        {
            var suggestions = await _agentHttpClient.PreviewTasksAsync(config.EndpointUrl, decryptedKey, config.ProviderType, config.Model, prompt, cancellationToken);
            return BaseReturn<IReadOnlyList<SuggestedTaskModel>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            return BaseReturn<IReadOnlyList<SuggestedTaskModel>>.BusinessError($"Falha ao obter sugestões do agente: {ex.Message}");
        }
    }

    public async Task<BaseReturn<AgentChatResponseModel>> ChatAsync(ChatAgentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Messages == null || command.Messages.Count == 0)
        {
            return BaseReturn<AgentChatResponseModel>.ValidationError("Envie uma mensagem para conversar com o assistente.");
        }

        var configTask = _agentConfigRepository.GetByUserIdAsync(command.UserId);
        var spacesTask = _spaceRepository.GetAccessibleSpacesByUserIdAsync(command.UserId);
        var tasksTask = _taskRepository.GetAccessibleTasksByUserIdAsync(command.UserId);
        await Task.WhenAll(configTask, spacesTask, tasksTask);

        var config = configTask.Result;
        if (config == null || !config.IsEnabled)
        {
            return BaseReturn<AgentChatResponseModel>.BusinessError("Nenhum agente de IA ativo configurado. Configure sua chave nas Configurações.");
        }

        string? decryptedKey = null;
        if (!string.IsNullOrWhiteSpace(config.EncryptedApiKey))
        {
            decryptedKey = _secretEncryptor.Decrypt(config.EncryptedApiKey);
        }

        var spaces = spacesTask.Result;
        var tasks = tasksTask.Result;
        var recentMessages = command.Messages.Count > ChatContextMessageLimit
            ? command.Messages.Skip(command.Messages.Count - ChatContextMessageLimit).ToList()
            : command.Messages;

        var editSpace = ResolveEditSpace(recentMessages, spaces);
        var editTasks = editSpace == null ? tasks : tasks.Where(t => t.SpaceId == editSpace.Id).ToArray();
        var editClarification = ResolveEditClarification(recentMessages, spaces, editSpace, editTasks);
        if (editClarification != null)
        {
            return BaseReturn<AgentChatResponseModel>.Success(editClarification);
        }

        var deletion = ResolveDeletionRequest(recentMessages, tasks);
        if (deletion != null)
        {
            return BaseReturn<AgentChatResponseModel>.Success(deletion);
        }

        var latestRequest = recentMessages[^1];
        AgentChatResponseModel? creationFallback = null;
        if (latestRequest.Role == "user")
        {
            creationFallback = DraftTaskFromRequest(latestRequest.Content, command.SpaceId, spaces);
            if (creationFallback?.SuggestedTask?.Subtasks.Count > 1)
            {
                return BaseReturn<AgentChatResponseModel>.Success(creationFallback);
            }
        }

        var answeringEditQuestion = latestRequest.Role == "user" &&
            recentMessages.Count > 1 && recentMessages[^2].Role == "assistant" &&
            recentMessages[^2].Content.EndsWith(EditChangesQuestion, StringComparison.Ordinal);
        var isEditRequest = latestRequest.Role == "user" &&
            (answeringEditQuestion || EditTaskRequestRegex().IsMatch(latestRequest.Content.Trim()) ||
             (recentMessages.Count > 1 && IsEditQuestion(recentMessages[^2], EditTargetQuestion)));
        var selectedEditTask = answeringEditQuestion && recentMessages.Count >= 4 &&
            recentMessages[^3].Role == "user" && IsEditQuestion(recentMessages[^4], EditTargetQuestion)
            && FindTasks(ReadTaskReference(recentMessages[^3].Content), editTasks) is [var selectedTask]
            ? selectedTask
            : null;

        var taskContext = string.Join('\n', recentMessages.Where(m => m.Role == "user").Select(m => m.Content));
        var relevantTasks = editTasks
            .OrderByDescending(t => t.Id == selectedEditTask?.Id)
            .ThenByDescending(t => taskContext.Contains(t.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => taskContext.Contains(t.Title, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => t.SpaceId == command.SpaceId)
            .Take(40)
            .ToList();
        var systemPrompt = BuildAgentSystemPrompt(spaces, relevantTasks, command.SpaceId);
        if (selectedEditTask != null)
        {
            systemPrompt += $"\nA tarefa selecionada para a edição em andamento tem ID {selectedEditTask.Id}.";
        }

        try
        {
            var completion = await _agentHttpClient.ChatAsync(
                config.EndpointUrl,
                decryptedKey,
                config.ProviderType,
                config.Model,
                systemPrompt,
                recentMessages,
                cancellationToken);

            var parsed = ParseCompletionResponse(completion, command.SpaceId, spaces, editTasks);
            if (creationFallback != null && parsed.ActionType != "CREATE_TASK")
            {
                return BaseReturn<AgentChatResponseModel>.Success(creationFallback);
            }
            if (selectedEditTask != null && parsed.ActionType == "UPDATE_TASK" &&
                parsed.SuggestedTask?.TaskId != selectedEditTask.Id)
            {
                return BaseReturn<AgentChatResponseModel>.Success(
                    BuildEditPoll(EditTargetQuestion, editTasks.Select(task => (task.Id, task.Title))));
            }

            if ((isEditRequest && parsed.ActionType is "CREATE_TASK" or "DELETE_TASK") ||
                (parsed.ActionType == "CREATE_TASK" && latestRequest.Role == "user" &&
                 NamedDeleteRequestRegex().IsMatch(latestRequest.Content.Trim())))
            {
                return BaseReturn<AgentChatResponseModel>.Success(new AgentChatResponseModel
                {
                    Reply = "Não consegui preparar essa alteração. Informe o título completo ou o ID da tarefa."
                });
            }

            if (string.IsNullOrWhiteSpace(parsed.Reply) && !parsed.IsAgentAction && parsed.Poll == null)
            {
                return BaseReturn<AgentChatResponseModel>.Success(new AgentChatResponseModel
                {
                    Reply = isEditRequest
                        ? "Não consegui preparar a edição. Envie o título da tarefa e a mudança desejada na mesma mensagem."
                        : "O assistente retornou uma resposta vazia. Envie a mensagem novamente."
                });
            }
            return BaseReturn<AgentChatResponseModel>.Success(parsed);
        }
        catch (Exception) when (creationFallback != null && !cancellationToken.IsCancellationRequested)
        {
            return BaseReturn<AgentChatResponseModel>.Success(creationFallback);
        }
        catch (Exception ex)
        {
            return BaseReturn<AgentChatResponseModel>.BusinessError($"Erro ao conversar com o agente: {ex.Message}");
        }
    }

    private static AgentChatResponseModel? DraftTaskFromRequest(
        string request,
        Guid? currentSpaceId,
        IReadOnlyList<Space> spaces)
    {
        var prefix = CreateTaskRequestRegex().Match(request.Trim().Trim('*').Trim());
        if (!prefix.Success) return null;

        var body = prefix.Groups["body"].Value.Trim();
        if (string.IsNullOrWhiteSpace(body)) return null;

        var sections = TaskListMarkerRegex().Split(body);
        var header = sections[0].Trim().TrimEnd(':').Trim();
        var items = sections.Skip(1)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (items.Length > 0)
        {
            (string Tag, string Label)[] tags = [("[BUG]", "correções"), ("[MELHORIA]", "melhorias"), ("[REGRA]", "regras")];
            var categories = tags.Where(tag => body.Contains(tag.Tag, StringComparison.OrdinalIgnoreCase))
                .Select(tag => tag.Label)
                .ToArray();
            var title = "Plano de trabalho do projeto";
            if (header.Length is > 0 and <= 100)
            {
                title = header;
            }
            else if (categories.Length > 0)
            {
                var subject = categories.Length == 1
                    ? categories[0]
                    : string.Join(", ", categories[..^1]) + " e " + categories[^1];
                title = char.ToUpperInvariant(subject[0]) + subject[1..] + " do projeto";
            }
            return BuildCreateProposal(title, header, null, null, currentSpaceId, spaces, items);
        }

        var firstLine = body.Split('\n', 2)[0].Trim();
        return BuildCreateProposal(ShortenTitle(firstLine, 80), body, null, null, currentSpaceId, spaces, []);
    }

    private static AgentChatResponseModel BuildCreateProposal(
        string title,
        string? description,
        string? priority,
        string? dueDate,
        Guid? spaceId,
        IReadOnlyList<Space> spaces,
        IReadOnlyList<string> subtasks)
    {
        if (!spaces.Any(space => space.Id == spaceId))
        {
            spaceId = spaces.Count == 1 ? spaces[0].Id : null;
        }

        var longItems = subtasks.Where(item => item.Length > 200).ToArray();
        if (longItems.Length > 0)
        {
            var fullNotes = "Detalhes das subtarefas:\n- " + string.Join("\n- ", longItems);
            description = string.IsNullOrWhiteSpace(description) ? fullNotes : $"{description}\n\n{fullNotes}";
        }

        return new AgentChatResponseModel
        {
            Reply = "Preparei a tarefa abaixo. Confirme a criação ou altere o título no cartão.",
            IsAgentAction = true,
            ActionType = "CREATE_TASK",
            NeedsSpace = !spaceId.HasValue,
            SuggestedTask = new SuggestedTaskModel
            {
                SpaceId = spaceId,
                Title = ShortenTitle(title.Trim(), 200),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                Priority = priority != null && EnumTaskPriority.IsValid(priority) ? priority : EnumTaskPriority.Medium,
                DueDate = DateOnly.TryParseExact(dueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : null,
                Subtasks = subtasks.Select(item => ShortenTitle(item, 200)).ToArray()
            }
        };
    }

    private static string ShortenTitle(string title, int maxLength)
    {
        if (title.Length <= maxLength) return title;
        var prefix = title[..(maxLength - 1)];
        if (char.IsHighSurrogate(prefix[^1])) prefix = prefix[..^1];
        var lastSpace = prefix.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? prefix[..lastSpace] : prefix).TrimEnd() + "…";
    }

    private static AgentChatResponseModel? ResolveEditClarification(
        IReadOnlyList<AgentChatMessageModel> messages,
        IReadOnlyList<Space> spaces,
        Space? editSpace,
        IReadOnlyList<TaskItem> tasks)
    {
        if (messages[^1].Role != "user") return null;

        var request = ReadTaskReference(messages[^1].Content);
        var repeatsEditRequest = request.Length > 0 && request.TrimEnd('?').Length == 0 &&
            messages.Count >= 3 && messages[^2].Role == "assistant" && messages[^3].Role == "user" &&
            BareEditRequestRegex().IsMatch(ReadTaskReference(messages[^3].Content));
        if (BareEditRequestRegex().IsMatch(request) || repeatsEditRequest)
        {
            return spaces.Count == 0
                ? new AgentChatResponseModel { Reply = "Você ainda não tem espaços disponíveis." }
                : BuildEditPoll(EditSpaceQuestion, spaces.Select(space => (space.Id, space.Name)));
        }

        if (messages.Count < 2) return null;
        var previous = messages[^2];
        if (IsEditQuestion(previous, EditSpaceQuestion))
        {
            if (editSpace == null)
            {
                return BuildEditPoll(EditSpaceQuestion, spaces.Select(space => (space.Id, space.Name)), "Escolha um dos espaços disponíveis.");
            }
            if (tasks.Count == 0)
            {
                return BuildEditPoll(EditSpaceQuestion, spaces.Select(space => (space.Id, space.Name)), $"O espaço “{editSpace.Name}” está sem tarefas.");
            }
            return BuildEditPoll(EditTargetQuestion, tasks.Select(task => (task.Id, task.Title)), $"Espaço “{editSpace.Name}” selecionado.");
        }

        if (!IsEditQuestion(previous, EditTargetQuestion))
        {
            return null;
        }

        if (editSpace == null)
        {
            return BuildEditPoll(EditSpaceQuestion, spaces.Select(space => (space.Id, space.Name)));
        }

        var candidates = FindTasks(request, tasks);
        return candidates.Length switch
        {
            1 => new AgentChatResponseModel
            {
                Reply = $"Encontrei a tarefa “{candidates[0].Title}”. {EditChangesQuestion}"
            },
            > 1 => BuildEditPoll(EditTargetQuestion, candidates.Select(task => (task.Id, task.Title)), "Encontrei mais de uma tarefa com esse título."),
            _ => BuildEditPoll(EditTargetQuestion, tasks.Select(task => (task.Id, task.Title)), "Não encontrei essa tarefa no espaço selecionado.")
        };
    }

    private static Space? ResolveEditSpace(IReadOnlyList<AgentChatMessageModel> messages, IReadOnlyList<Space> spaces)
    {
        if (messages.Count < 2 || messages[^1].Role != "user" || messages[^2].Role != "assistant" ||
            (!IsEditQuestion(messages[^2], EditSpaceQuestion) && !IsEditQuestion(messages[^2], EditTargetQuestion) &&
             !IsEditQuestion(messages[^2], EditChangesQuestion)))
        {
            return null;
        }

        for (var index = messages.Count - 2; index >= 0; index--)
        {
            if (!IsEditQuestion(messages[index], EditSpaceQuestion)) continue;
            if (messages[index + 1].Role != "user") return null;

            var reference = ReadTaskReference(messages[index + 1].Content);
            if (Guid.TryParse(reference, out var spaceId)) return spaces.FirstOrDefault(space => space.Id == spaceId);
            return spaces.Where(space => string.Equals(space.Name, reference, StringComparison.OrdinalIgnoreCase))
                .Take(2).ToArray() is [var chosenSpace] ? chosenSpace : null;
        }

        return null;
    }

    private static AgentChatResponseModel BuildEditPoll(
        string question,
        IEnumerable<(Guid Id, string Label)> entries,
        string? context = null)
    {
        var options = entries.GroupBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group.Count() == 1
                ? new[] { group.First().Label }
                : group.Select(entry => $"{entry.Label} ({entry.Id})"))
            .ToArray();
        return new AgentChatResponseModel
        {
            Reply = context == null ? question : $"{context} {question}",
            Poll = new AgentChatPollModel(question, false, options, AllowCustomOption: false)
        };
    }

    private static bool IsEditQuestion(AgentChatMessageModel message, string question) =>
        message.Role == "assistant" && message.Content.EndsWith(question, StringComparison.Ordinal);

    private static string ReadTaskReference(string content)
    {
        var reference = content.Trim().Trim('*', '_').Trim().Trim('"', '“', '”').Trim();
        return reference.Length > 37 && reference[^38] == '(' && reference[^1] == ')' &&
            Guid.TryParse(reference[^37..^1], out var id) ? id.ToString() : reference;
    }

    public async Task<BaseReturn<IReadOnlyList<string>>> ListModelsAsync(Guid userId, string? endpointUrl, string? apiKey, string? providerType, CancellationToken cancellationToken = default)
    {
        string finalUrl;
        string? finalKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        string finalType = providerType ?? EnumAgentProviderType.OpenAiCompatible;

        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            var config = await _agentConfigRepository.GetByUserIdAsync(userId);
            if (config == null)
            {
                return BaseReturn<IReadOnlyList<string>>.ValidationError("Informe o endpoint ou salve uma configuração antes de buscar modelos.");
            }
            finalUrl = config.EndpointUrl;
            finalType = config.ProviderType;
            if (finalKey == null && !string.IsNullOrWhiteSpace(config.EncryptedApiKey))
            {
                finalKey = _secretEncryptor.Decrypt(config.EncryptedApiKey);
            }
        }
        else
        {
            finalUrl = endpointUrl.Trim();
            if (finalKey == null)
            {
                var existing = await _agentConfigRepository.GetByUserIdAsync(userId);
                if (existing != null && !string.IsNullOrWhiteSpace(existing.EncryptedApiKey))
                {
                    finalKey = _secretEncryptor.Decrypt(existing.EncryptedApiKey);
                }
            }
        }

        try
        {
            var models = await _agentHttpClient.ListModelsAsync(finalUrl, finalKey, finalType, cancellationToken);
            return BaseReturn<IReadOnlyList<string>>.Success(models);
        }
        catch (Exception ex)
        {
            return BaseReturn<IReadOnlyList<string>>.BusinessError($"Não foi possível listar modelos do provedor: {ex.Message}");
        }
    }

    private static string BuildAgentSystemPrompt(
        IReadOnlyList<Space> spaces,
        IReadOnlyList<TaskItem> tasks,
        Guid? currentSpaceId)
    {
        var spaceNameMap = spaces.ToDictionary(s => s.Id, s => s.Name);
        var sb = new StringBuilder();
        sb.AppendLine("Você é o Assistente e Agente de Produtividade do Task.dev. Responda sempre em Português do Brasil, de forma clara, objetiva e amigável.");
        sb.AppendLine($"Data de hoje: {DateTime.UtcNow:yyyy-MM-dd}.");
        sb.AppendLine();
        sb.AppendLine("Espaços (workflows) disponíveis do usuário:");
        if (spaces.Count == 0)
        {
            sb.AppendLine("- Nenhum espaço criado ainda.");
        }
        else
        {
            foreach (var s in spaces)
            {
                var isCurrent = currentSpaceId.HasValue && s.Id == currentSpaceId.Value ? " (ESPAÇO ABERTO ATUALMENTE)" : "";
                sb.AppendLine($"- ID: {s.Id} | Nome: \"{s.Name}\"{isCurrent}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Tarefas existentes do usuário (use os IDs abaixo quando o usuário pedir para EDITAR ou EXCLUIR):");
        if (tasks.Count == 0)
        {
            sb.AppendLine("- Nenhuma tarefa cadastrada.");
        }
        else
        {
            foreach (var t in tasks)
            {
                var spName = spaceNameMap.TryGetValue(t.SpaceId, out var name) ? name : "Desconhecido";
                var due = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "sem prazo";
                sb.AppendLine($"- ID: {t.Id} | Espaço: \"{spName}\" | Título: \"{t.Title}\" | Status: {t.Status} | Prioridade: {t.Priority} | Prazo: {due}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("""
            COMO VOCÊ DEVE AGIR:
            1. Formatação e tom: Responda sempre em Markdown limpo (**negrito**, listas com `-`, `código`). NUNCA mencione "bloco JSON" ou nomes de ferramentas ao usuário.
            2. Você TEM capacidade completa para **criar**, **editar** e **excluir** tarefas através das suas ferramentas (`create_task`, `update_task`, `delete_task`) e fazer perguntas clicáveis (`ask_options`). NUNCA diga que não consegue editar ou excluir tarefas.
            3. Enquete Interativa (`ask_options`):
               - Faça **APENAS UMA pergunta por vez** quando precisar esclarecer algo (ex: qual tarefa, prioridade, prazo).
               - Use a ferramenta `ask_options` sempre que fizer uma pergunta de escolha.
            4. Criar tarefa (`create_task`):
               - Se houver um `(ESPAÇO ABERTO ATUALMENTE)` e o pedido não citar outro, use-o automaticamente.
               - Quando o usuário fornecer conteúdo suficiente, sugira um título curto com base no assunto e chame `create_task` de imediato. O próprio cartão permite alterar o título e confirmar a criação.
               - Organize textos com vários pontos em uma tarefa principal com subtarefas distintas. Preserve bugs, regras, dúvidas pendentes, decisões e itens de teste sem mudar seu significado.
               - Se o usuário não informar prioridade, use MEDIUM. Se não informar prazo para a tarefa inteira, deixe o prazo vazio. Não pergunte por título, prioridade ou prazo quando já puder montar a proposta.
            5. Editar tarefa (`update_task`):
               - Quando o usuário pedir para editar/alterar/atualizar/concluir/mudar prioridade/mudar prazo de uma tarefa existente, identifique o `task_id` na lista acima e chame `update_task` (NUNCA use `create_task` para editar!).
               - Envie somente os campos que o usuário pediu para mudar. Use due_date vazio para remover o prazo.
               - Se houver mais de uma tarefa parecida ou dúvida de qual tarefa editar, pergunte usando `ask_options` listando os títulos candidatos.
            6. Excluir tarefa (`delete_task`):
               - Quando o usuário pedir para excluir/remover/apagar uma tarefa, identifique o `task_id` na lista acima e chame `delete_task`. O cartão do aplicativo pede a confirmação, então dispense perguntas de sim ou não e não afirme que a tarefa já foi excluída.
               - Se não souber qual tarefa excluir, pergunte usando `ask_options` listando os títulos das tarefas.
            7. Se o provedor não disponibilizar ferramentas, responda com um objeto JSON contendo "action" com o nome da ferramenta e os mesmos argumentos no mesmo nível. Exemplos: {"action":"update_task","task_id":"UUID da tarefa","title":"Novo título"} e {"action":"delete_task","task_id":"UUID da tarefa"}.
            """);

        return sb.ToString();
    }

    private static AgentChatResponseModel? ResolveDeletionRequest(
        IReadOnlyList<AgentChatMessageModel> messages,
        IReadOnlyList<TaskItem> tasks)
    {
        if (messages[^1].Role != "user") return null;

        var request = messages[^1].Content.Trim();
        if (DeleteRequestRegex().IsMatch(request))
        {
            return new AgentChatResponseModel { Reply = "Qual tarefa você quer excluir? Digite o título ou o ID." };
        }

        var direct = NamedDeleteRequestRegex().Match(request);
        var previousRequest = messages.Take(messages.Count - 1).LastOrDefault(m => m.Role == "user")?.Content.Trim();
        var reference = direct.Success
            ? direct.Groups["reference"].Value
            : previousRequest != null && DeleteRequestRegex().IsMatch(previousRequest) ? request : null;
        if (reference == null) return null;

        reference = reference.Trim().TrimEnd('.', '?', '!').Trim('"', '“', '”').Trim();
        var candidates = FindTasks(reference, tasks);
        return candidates.Length switch
        {
            1 => BuildDeleteProposal(candidates[0]),
            > 1 => new AgentChatResponseModel
            {
                Reply = "Encontrei mais de uma tarefa com esse título. Informe o título completo ou o ID da tarefa."
            },
            _ => null
        };
    }

    private static TaskItem[] FindTasks(string reference, IReadOnlyList<TaskItem> tasks)
    {
        if (string.IsNullOrWhiteSpace(reference)) return [];
        if (Guid.TryParse(reference, out var id)) return tasks.Where(t => t.Id == id).ToArray();

        var exact = tasks.Where(t => string.Equals(t.Title, reference, StringComparison.OrdinalIgnoreCase)).ToArray();
        return exact.Length > 0
            ? exact
            : tasks.Where(t => t.Title.Contains(reference, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private static AgentChatResponseModel ParseCompletionResponse(
        AgentRawCompletion completion,
        Guid? currentSpaceId,
        IReadOnlyList<Space> spaces,
        IReadOnlyList<TaskItem> tasks)
    {
        var text = completion.Text.Trim();
        if (completion.ToolName != null)
        {
            var action = ParseAction(completion.ToolArgumentsJson, completion.ToolName, text, currentSpaceId, spaces, tasks);
            if (action != null) return action;
        }

        var fence = JsonFenceRegex().Match(text);
        var json = fence.Success ? fence.Groups[1].Value : text;
        if (json.TrimStart().StartsWith('{'))
        {
            var reply = fence.Success ? JsonFenceRegex().Replace(text, string.Empty).Trim() : string.Empty;
            var action = ParseAction(json, null, reply, currentSpaceId, spaces, tasks);
            if (action != null) return action;
        }

        return new AgentChatResponseModel { Reply = text, Poll = InferFallbackPoll(text, spaces) };
    }

    private static AgentChatResponseModel? ParseAction(
        string? json,
        string? toolName,
        string reply,
        Guid? currentSpaceId,
        IReadOnlyList<Space> spaces,
        IReadOnlyList<TaskItem> tasks)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var args = document.RootElement;
            if (args.ValueKind != JsonValueKind.Object) return null;

            var action = (toolName ?? ReadString(args, "action"))?.ToUpperInvariant();
            switch (action)
            {
                case "ASK_OPTIONS":
                    var options = ReadStrings(args, "options");
                    if (options.Length == 0) return null;
                    var question = ReadString(args, "question") ?? "Selecione uma opção:";
                    var multiSelect = (args.TryGetProperty("multi_select", out var multi) ||
                        args.TryGetProperty("multiSelect", out multi)) && multi.ValueKind == JsonValueKind.True;
                    return new AgentChatResponseModel
                    {
                        Reply = string.IsNullOrWhiteSpace(reply) ? question : reply,
                        Poll = new AgentChatPollModel(question, multiSelect, options)
                    };

                case "CREATE_TASK":
                    var title = ReadString(args, "title");
                    if (string.IsNullOrWhiteSpace(title)) return null;
                    var space = ReadString(args, "space_id") ?? ReadString(args, "spaceId");
                    var spaceId = Guid.TryParse(space, out var id) && spaces.Any(s => s.Id == id)
                        ? id
                        : currentSpaceId;
                    var priority = ReadString(args, "priority")?.ToUpperInvariant();
                    var dueDate = ReadString(args, "due_date") ?? ReadString(args, "dueDate");
                    return BuildCreateProposal(title, ReadString(args, "description"), priority, dueDate, spaceId, spaces, ReadStrings(args, "subtasks"));

                case "UPDATE_TASK":
                case "DELETE_TASK":
                    var taskId = ReadString(args, "task_id") ?? ReadString(args, "taskId");
                    var task = Guid.TryParse(taskId, out var targetId) ? tasks.FirstOrDefault(t => t.Id == targetId) : null;
                    if (task == null)
                    {
                        return new AgentChatResponseModel { Reply = "Não consegui identificar a tarefa. Informe o título completo ou o ID." };
                    }
                    return action == "UPDATE_TASK" ? BuildUpdateProposal(task, args) : BuildDeleteProposal(task);

                default:
                    return null;
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AgentChatResponseModel BuildUpdateProposal(TaskItem task, JsonElement args)
    {
        var title = ReadString(args, "title")?.Trim();
        var description = ReadString(args, "description");
        var status = ReadString(args, "status")?.ToUpperInvariant();
        var priority = ReadString(args, "priority")?.ToUpperInvariant();
        var changedFields = new List<string>();
        if (!string.IsNullOrEmpty(title)) changedFields.Add("title");
        if (args.TryGetProperty("description", out var desc) && desc.ValueKind is JsonValueKind.String or JsonValueKind.Null)
            changedFields.Add("description");
        if (status != null && EnumTaskStatus.IsValid(status)) changedFields.Add("status");
        if (priority != null && EnumTaskPriority.IsValid(priority)) changedFields.Add("priority");

        var dueDate = task.DueDate?.ToString("yyyy-MM-dd");
        if (args.TryGetProperty("due_date", out var due) || args.TryGetProperty("dueDate", out due))
        {
            var value = due.ValueKind == JsonValueKind.String ? due.GetString()?.Trim() : null;
            if (due.ValueKind == JsonValueKind.Null || value == "" || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                dueDate = null;
            }
            else if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return new AgentChatResponseModel { Reply = "Qual é o novo prazo? Informe uma data válida ou diga “sem prazo”." };
            }
            else
            {
                dueDate = value;
            }
            changedFields.Add("dueDate");
        }

        if (changedFields.Count == 0)
        {
            return new AgentChatResponseModel { Reply = "O que você quer alterar nessa tarefa?" };
        }

        return new AgentChatResponseModel
        {
            Reply = "Confira a edição abaixo antes de salvar.",
            IsAgentAction = true,
            ActionType = "UPDATE_TASK",
            SuggestedTask = new SuggestedTaskModel
            {
                TaskId = task.Id,
                SpaceId = task.SpaceId,
                Title = changedFields.Contains("title") ? title! : task.Title,
                Description = changedFields.Contains("description") ? description : task.Description,
                Status = changedFields.Contains("status") ? status : task.Status,
                Priority = changedFields.Contains("priority") ? priority! : task.Priority,
                DueDate = dueDate,
                ChangedFields = changedFields
            }
        };
    }

    private static AgentChatResponseModel BuildDeleteProposal(TaskItem task) => new()
    {
        Reply = "Quer excluir a tarefa abaixo?",
        IsAgentAction = true,
        ActionType = "DELETE_TASK",
        SuggestedTask = new SuggestedTaskModel
        {
            TaskId = task.Id,
            SpaceId = task.SpaceId,
            Title = task.Title
        }
    };

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string[] ReadStrings(JsonElement element, string name) =>
        element.TryGetProperty(name, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString()!.Trim())
                .Where(value => value.Length > 0)
                .ToArray()
            : [];

    [GeneratedRegex(@"^(?:quero\s+|pode\s+)?(?:excluir|exclua|remover|remova|apagar|apague)\s+(?:(?:uma|a)\s+)?tarefa[.!?]*$", RegexOptions.IgnoreCase)]
    private static partial Regex DeleteRequestRegex();

    [GeneratedRegex(@"^(?:quero\s+|pode\s+)?(?:excluir|exclua|remover|remova|apagar|apague)\s+(?:a\s+tarefa\s+|tarefa\s+|a\s+)?(?<reference>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex NamedDeleteRequestRegex();

    [GeneratedRegex(@"^(?:quero\s+|pode\s+)?(?:editar|edite|alterar|altere|atualizar|atualize|renomear|renomeie|concluir|conclua)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex EditTaskRequestRegex();

    [GeneratedRegex(@"^(?:(?:eu\s+)?(?:quero|gostaria\s+de)\s+|pode\s+)?(?:editar|edite|alterar|altere|atualizar|atualize|renomear|renomeie)(?:\s+(?:(?:uma|a|minha|essa|esta)\s+)?(?:tarefa|task))?[.!?]*$", RegexOptions.IgnoreCase)]
    private static partial Regex BareEditRequestRegex();

    [GeneratedRegex(@"^(?:(?:agora|por favor)\s*[, :]?\s*)?(?:(?:quero|pode)\s+)?(?:crie|criar|cria|adicione|adicionar|cadastre|cadastrar)\s+(?:(?:uma|um)\s+)?(?:nov[ao]s?\s+)?(?:tarefas?|tasks?)(?:[ \t]+nov[ao]s?)?(?:[ \t]*:[ \t]*|[ \t]*\r?\n|[ \t]+(?=[-*•]|\d+[.)]))(?<body>[\s\S]*)$", RegexOptions.IgnoreCase)]
    private static partial Regex CreateTaskRequestRegex();

    [GeneratedRegex(@"(?:^|\r?\n|(?<=[.!?\]])[ \t]+)[ \t]*(?:[-*•]|\d+[.)])[ \t]+")]
    private static partial Regex TaskListMarkerRegex();

    private static AgentChatPollModel? InferFallbackPoll(string reply, IReadOnlyList<Space> spaces)
    {
        if (!reply.Contains('?')) return null;

        var mentionedSpaces = spaces
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) &&
                        reply.Contains(s.Name, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name)
            .Distinct()
            .ToList();

        if (mentionedSpaces.Count >= 2 ||
            (reply.Contains("espaço", StringComparison.OrdinalIgnoreCase) && spaces.Count > 0 &&
             (reply.Contains("qual espaço", StringComparison.OrdinalIgnoreCase) ||
              reply.Contains("qual desses", StringComparison.OrdinalIgnoreCase) ||
              reply.Contains("em qual", StringComparison.OrdinalIgnoreCase) ||
              reply.Contains("escolha o espaço", StringComparison.OrdinalIgnoreCase))))
        {
            var opts = mentionedSpaces.Count >= 2 ? mentionedSpaces : spaces.Select(s => s.Name).ToList();
            return new AgentChatPollModel("Em qual espaço deseja criar a tarefa?", false, opts);
        }

        if (reply.Contains("prioridade", StringComparison.OrdinalIgnoreCase) &&
            (reply.Contains("LOW", StringComparison.OrdinalIgnoreCase) ||
             reply.Contains("MEDIUM", StringComparison.OrdinalIgnoreCase) ||
             reply.Contains("HIGH", StringComparison.OrdinalIgnoreCase) ||
             reply.Contains("URGENT", StringComparison.OrdinalIgnoreCase)))
        {
            return new AgentChatPollModel(
                "Qual prioridade você prefere?",
                false,
                ["Baixa (LOW)", "Média (MEDIUM)", "Alta (HIGH)", "Urgente (URGENT)"]);
        }

        if (reply.Contains("prazo", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("data limite", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentChatPollModel(
                "Qual prazo deseja definir?",
                false,
                ["Hoje", "Amanhã", "Em 3 dias", "Próxima semana", "Sem prazo", "Outro"]);
        }

        return null;
    }

    [GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonFenceRegex();
}
