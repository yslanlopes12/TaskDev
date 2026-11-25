using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;

public partial class AgentHttpClient : IAgentHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentHttpClient> _logger;

    public AgentHttpClient(HttpClient httpClient, ILogger<AgentHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<bool> TestConnectionAsync(
        string endpointUrl,
        string? apiKey,
        string providerType,
        string? model,
        CancellationToken cancellationToken = default)
    {
        ValidateUrlSsrf(endpointUrl);

        if (providerType == EnumAgentProviderType.Anthropic)
        {
            var url = NormalizeAnthropicUrl(endpointUrl);
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model = !string.IsNullOrWhiteSpace(model) ? model : "claude-3-5-sonnet-latest",
                max_tokens = 10,
                messages = new[] { new { role = "user", content = "ping" } }
            };
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var res = await _httpClient.SendAsync(req, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);
            return true;
        }

        if (providerType == EnumAgentProviderType.Gemini)
        {
            var resolvedModel = !string.IsNullOrWhiteSpace(model) ? model : "gemini-2.0-flash";
            var url = NormalizeGeminiUrl(endpointUrl, resolvedModel);
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.Add("x-goog-api-key", apiKey);

            var body = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = "ping" } } }
                },
                generationConfig = new { maxOutputTokens = 10 }
            };
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var res = await _httpClient.SendAsync(req, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);
            return true;
        }

        if (providerType == EnumAgentProviderType.OpenAiCompatible)
        {
            var url = NormalizeOpenAiUrl(endpointUrl);
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var testBody = new
            {
                model = !string.IsNullOrWhiteSpace(model) ? model : "gpt-4o-mini",
                messages = new[] { new { role = "user", content = "ping" } },
                max_tokens = 10
            };
            req.Content = new StringContent(JsonSerializer.Serialize(testBody), Encoding.UTF8, "application/json");
            var res = await _httpClient.SendAsync(req, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);
            return true;
        }

        using var webhookReq = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
            webhookReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        webhookReq.Content = new StringContent(
            JsonSerializer.Serialize(new { action = "ping", timestamp = DateTimeOffset.UtcNow }),
            Encoding.UTF8,
            "application/json");
        var webhookRes = await _httpClient.SendAsync(webhookReq, cancellationToken);
        await EnsureSuccessOrThrowDetailedAsync(webhookRes, cancellationToken);
        return true;
    }

    public async Task<AgentRawCompletion> ChatAsync(
        string endpointUrl,
        string? apiKey,
        string providerType,
        string? model,
        string systemPrompt,
        IReadOnlyList<AgentChatMessageModel> messages,
        CancellationToken cancellationToken = default)
    {
        ValidateUrlSsrf(endpointUrl);

        if (providerType == EnumAgentProviderType.Anthropic)
        {
            var url = NormalizeAnthropicUrl(endpointUrl);
            var formattedMessages = messages
                .Where(m => m.Role is "user" or "assistant")
                .Select(m => new { role = m.Role, content = m.Content })
                .ToList();

            var body = new
            {
                model = !string.IsNullOrWhiteSpace(model) ? model : "claude-3-5-sonnet-latest",
                max_tokens = 4096,
                system = systemPrompt,
                messages = formattedMessages,
                tools = BuildAnthropicTools()
            };
            var bodyJson = JsonSerializer.Serialize(body);
            var res = await SendWithTransientRetryAsync(() =>
            {
                var r = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    r.Headers.Add("x-api-key", apiKey);
                r.Headers.Add("anthropic-version", "2023-06-01");
                r.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                return r;
            }, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);

            var rawJson = await res.Content.ReadAsStringAsync(cancellationToken);
            return ExtractAnthropicCompletion(rawJson);
        }

        if (providerType == EnumAgentProviderType.Gemini)
        {
            var resolvedModel = !string.IsNullOrWhiteSpace(model) ? model : "gemini-2.0-flash";
            var url = NormalizeGeminiUrl(endpointUrl, resolvedModel);

            var contents = messages
                .Where(m => m.Role is "user" or "assistant")
                .Select(m => new
                {
                    role = m.Role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = m.Content } }
                })
                .ToList();

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                tools = BuildGeminiTools(),
                generationConfig = new { temperature = 0.4, maxOutputTokens = 4096 }
            };
            var bodyJson = JsonSerializer.Serialize(body);
            var res = await SendWithTransientRetryAsync(() =>
            {
                var r = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    r.Headers.Add("x-goog-api-key", apiKey);
                r.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                return r;
            }, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);

            var rawJson = await res.Content.ReadAsStringAsync(cancellationToken);
            return ExtractGeminiCompletion(rawJson);
        }

        if (providerType == EnumAgentProviderType.OpenAiCompatible)
        {
            var url = NormalizeOpenAiUrl(endpointUrl);

            var allMessages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };
            foreach (var m in messages)
            {
                var role = m.Role is "user" or "assistant" ? m.Role : "user";
                allMessages.Add(new { role, content = m.Content });
            }

            var resolvedModel = !string.IsNullOrWhiteSpace(model) ? model : "gpt-4o-mini";

            var bodyWithTools = new
            {
                model = resolvedModel,
                messages = allMessages,
                tools = BuildOpenAiTools(),
                tool_choice = "auto",
                temperature = 0.4,
                max_tokens = 4096
            };

            var bodyJson = JsonSerializer.Serialize(bodyWithTools);
            var res = await SendWithTransientRetryAsync(() =>
            {
                var r = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                r.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                return r;
            }, cancellationToken);

            // Se o modelo/provedor não suportar native tools (400), faz fallback automático sem tools
            if (res.StatusCode == HttpStatusCode.BadRequest)
            {
                var errSnippet = await res.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Provider returned 400 with tools, retrying without tools: {Err}", errSnippet);
                res.Dispose();

                var fallbackBody = JsonSerializer.Serialize(new
                {
                    model = resolvedModel,
                    messages = allMessages,
                    temperature = 0.4,
                    max_tokens = 4096
                });

                res = await SendWithTransientRetryAsync(() =>
                {
                    var r = new HttpRequestMessage(HttpMethod.Post, url);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    r.Content = new StringContent(fallbackBody, Encoding.UTF8, "application/json");
                    return r;
                }, cancellationToken);
            }

            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);

            var rawJson = await res.Content.ReadAsStringAsync(cancellationToken);
            return ExtractOpenAiCompletion(rawJson);
        }

        // Webhook
        using var webhookReq = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
            webhookReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        webhookReq.Content = new StringContent(
            JsonSerializer.Serialize(new { systemPrompt, messages, timestamp = DateTimeOffset.UtcNow }),
            Encoding.UTF8,
            "application/json");
        var webhookRes = await _httpClient.SendAsync(webhookReq, cancellationToken);
        await EnsureSuccessOrThrowDetailedAsync(webhookRes, cancellationToken);
        var webhookRaw = await webhookRes.Content.ReadAsStringAsync(cancellationToken);
        return new AgentRawCompletion(ExtractWebhookText(webhookRaw));
    }

    public async Task<IReadOnlyList<SuggestedTaskModel>> PreviewTasksAsync(
        string endpointUrl,
        string? apiKey,
        string providerType,
        string? model,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var systemInstructions = """
            Você é um assistente do Task.dev que converte linguagem natural em tarefas estruturadas.
            Sua resposta DEVE ser estritamente um objeto JSON no formato:
            {
              "tasks": [
                {
                  "title": "Título claro e objetivo da tarefa",
                  "description": "Detalhes opcionais ou null",
                  "priority": "LOW" ou "MEDIUM" ou "HIGH" ou "URGENT",
                  "dueDate": "AAAA-MM-DD" ou null,
                  "subtasks": ["subtarefa 1", "subtarefa 2"]
                }
              ]
            }
            Retorne apenas o JSON puro, sem marcações markdown como ```json.
        """;

        if (providerType == EnumAgentProviderType.Webhook)
        {
            ValidateUrlSsrf(endpointUrl);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { prompt, timestamp = DateTimeOffset.UtcNow }),
                Encoding.UTF8,
                "application/json");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(response, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseTasksJson(rawJson.Trim());
        }

        var completion = await ChatAsync(
            endpointUrl,
            apiKey,
            providerType,
            model,
            systemInstructions,
            [new AgentChatMessageModel("user", prompt)],
            cancellationToken);

        return ParseTasksJson(StripMarkdownFence(completion.Text));
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(
        string endpointUrl,
        string? apiKey,
        string providerType,
        CancellationToken cancellationToken = default)
    {
        ValidateUrlSsrf(endpointUrl);

        if (providerType == EnumAgentProviderType.Webhook)
        {
            return [];
        }

        if (providerType == EnumAgentProviderType.Gemini)
        {
            var trimmed = endpointUrl.Trim().TrimEnd('/');
            var modelsUrl = trimmed.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? $"{trimmed}/models"
                : $"{trimmed}/v1beta/models";

            using var req = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.Add("x-goog-api-key", apiKey);

            var res = await _httpClient.SendAsync(req, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var list = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var supportsGenerateContent = false;
                    if (item.TryGetProperty("supportedGenerationMethods", out var methods) && methods.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in methods.EnumerateArray())
                        {
                            if (string.Equals(m.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase))
                            {
                                supportsGenerateContent = true;
                                break;
                            }
                        }
                    }

                    if (!supportsGenerateContent) continue;

                    var clean = name.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
                        ? name["models/".Length..]
                        : name;

                    if (IsChatCapableModelId(clean))
                        list.Add(clean);
                }
            }
            return list.Distinct().OrderBy(x => x).ToList();
        }

        if (providerType == EnumAgentProviderType.Anthropic)
        {
            var trimmed = endpointUrl.Trim().TrimEnd('/');
            var modelsUrl = trimmed.EndsWith("/messages", StringComparison.OrdinalIgnoreCase)
                ? trimmed[..^"/messages".Length] + "/models"
                : trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                    ? $"{trimmed}/models"
                    : $"{trimmed}/v1/models";

            using var req = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");

            var res = await _httpClient.SendAsync(req, cancellationToken);
            await EnsureSuccessOrThrowDetailedAsync(res, cancellationToken);
            var json = await res.Content.ReadAsStringAsync(cancellationToken);
            return ParseOpenAiModelsArray(json);
        }

        var chatUrl = NormalizeOpenAiUrl(endpointUrl);
        var openAiModelsUrl = chatUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? chatUrl[..^"/chat/completions".Length] + "/models"
            : chatUrl.TrimEnd('/') + "/models";

        using var openAiReq = new HttpRequestMessage(HttpMethod.Get, openAiModelsUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
            openAiReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var openAiRes = await _httpClient.SendAsync(openAiReq, cancellationToken);
        await EnsureSuccessOrThrowDetailedAsync(openAiRes, cancellationToken);
        var openAiJson = await openAiRes.Content.ReadAsStringAsync(cancellationToken);
        return ParseOpenAiModelsArray(openAiJson);
    }

    private static object[] BuildOpenAiTools() =>
    [
        new
        {
            type = "function",
            function = new
            {
                name = "ask_options",
                description = "Apresenta uma enquete interativa com botões clicáveis para o usuário escolher espaço, prioridade, prazo ou opções sem precisar digitar.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        question = new { type = "string", description = "Pergunta curta e clara em português" },
                        multi_select = new { type = "boolean", description = "true se o usuário puder marcar várias opções; false para escolha única" },
                        options = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Lista de opções curtas e claras para o usuário clicar"
                        }
                    },
                    required = new[] { "question", "multi_select", "options" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "create_task",
                description = "Propõe a criação estruturada de uma tarefa com título, descrição, prioridade, prazo e subtarefas no Task.dev.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        space_id = new { type = "string", description = "UUID do espaço de destino (ou vazio se indefinido)" },
                        title = new { type = "string", description = "Título claro e objetivo da tarefa" },
                        description = new { type = "string", description = "Descrição detalhada da tarefa" },
                        priority = new { type = "string", @enum = new[] { "LOW", "MEDIUM", "HIGH", "URGENT" } },
                        due_date = new { type = "string", description = "Prazo no formato AAAA-MM-DD ou vazio se não houver prazo" },
                        subtasks = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Lista de subtarefas práticas para concluir a tarefa"
                        }
                    },
                    required = new[] { "title", "priority", "subtasks" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "update_task",
                description = "Edita uma tarefa existente (título, descrição, status, prioridade ou prazo) pelo task_id.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        task_id = new { type = "string", description = "UUID da tarefa existente a ser editada" },
                        title = new { type = "string", description = "Novo título da tarefa (opcional)" },
                        description = new { type = "string", description = "Nova descrição da tarefa (opcional)" },
                        status = new { type = "string", @enum = new[] { "PENDING", "IN_PROGRESS", "DONE" } },
                        priority = new { type = "string", @enum = new[] { "LOW", "MEDIUM", "HIGH", "URGENT" } },
                        due_date = new { type = "string", description = "Novo prazo AAAA-MM-DD ou vazio para remover" }
                    },
                    required = new[] { "task_id" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "delete_task",
                description = "Propõe a exclusão de uma tarefa existente pelo task_id.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        task_id = new { type = "string", description = "UUID da tarefa existente a ser excluída" },
                        title = new { type = "string", description = "Título da tarefa para confirmação" }
                    },
                    required = new[] { "task_id" }
                }
            }
        }
    ];

    private static object[] BuildAnthropicTools() =>
    [
        new
        {
            name = "ask_options",
            description = "Apresenta uma enquete interativa com botões clicáveis para o usuário escolher espaço, prioridade, prazo ou opções.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    question = new { type = "string" },
                    multi_select = new { type = "boolean" },
                    options = new { type = "array", items = new { type = "string" } }
                },
                required = new[] { "question", "multi_select", "options" }
            }
        },
        new
        {
            name = "create_task",
            description = "Propõe a criação estruturada de uma tarefa com título, descrição, prioridade, prazo e subtarefas.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    space_id = new { type = "string" },
                    title = new { type = "string" },
                    description = new { type = "string" },
                    priority = new { type = "string", @enum = new[] { "LOW", "MEDIUM", "HIGH", "URGENT" } },
                    due_date = new { type = "string" },
                    subtasks = new { type = "array", items = new { type = "string" } }
                },
                required = new[] { "title", "priority", "subtasks" }
            }
        },
        new
        {
            name = "update_task",
            description = "Edita uma tarefa existente (título, descrição, status, prioridade ou prazo) pelo task_id.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    task_id = new { type = "string" },
                    title = new { type = "string" },
                    description = new { type = "string" },
                    status = new { type = "string", @enum = new[] { "PENDING", "IN_PROGRESS", "DONE" } },
                    priority = new { type = "string", @enum = new[] { "LOW", "MEDIUM", "HIGH", "URGENT" } },
                    due_date = new { type = "string" }
                },
                required = new[] { "task_id" }
            }
        },
        new
        {
            name = "delete_task",
            description = "Propõe a exclusão de uma tarefa existente pelo task_id.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    task_id = new { type = "string" },
                    title = new { type = "string" }
                },
                required = new[] { "task_id" }
            }
        }
    ];

    private static object[] BuildGeminiTools() =>
    [
        new
        {
            functionDeclarations = new object[]
            {
                new
                {
                    name = "ask_options",
                    description = "Apresenta uma enquete interativa com botões clicáveis para o usuário escolher espaço, prioridade ou prazo.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            question = new { type = "string" },
                            multi_select = new { type = "boolean" },
                            options = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "question", "multi_select", "options" }
                    }
                },
                new
                {
                    name = "create_task",
                    description = "Propõe a criação estruturada de uma tarefa com título, descrição, prioridade, prazo e subtarefas.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            space_id = new { type = "string" },
                            title = new { type = "string" },
                            description = new { type = "string" },
                            priority = new { type = "string" },
                            due_date = new { type = "string" },
                            subtasks = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "title", "priority", "subtasks" }
                    }
                },
                new
                {
                    name = "update_task",
                    description = "Edita uma tarefa existente pelo task_id.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new { type = "string" },
                            title = new { type = "string" },
                            description = new { type = "string" },
                            status = new { type = "string" },
                            priority = new { type = "string" },
                            due_date = new { type = "string" }
                        },
                        required = new[] { "task_id" }
                    }
                },
                new
                {
                    name = "delete_task",
                    description = "Propõe a exclusão de uma tarefa existente pelo task_id.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new { type = "string" },
                            title = new { type = "string" }
                        },
                        required = new[] { "task_id" }
                    }
                }
            }
        }
    ];

    private AgentRawCompletion ExtractOpenAiCompletion(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return new AgentRawCompletion(string.Empty);

        var choice = choices[0];
        var finishReason = choice.TryGetProperty("finish_reason", out var frProp) && frProp.ValueKind == JsonValueKind.String
            ? frProp.GetString()
            : null;

        if (!choice.TryGetProperty("message", out var msg))
            return new AgentRawCompletion(string.Empty, FinishReason: finishReason);

        var content = msg.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String
            ? StripThinkBlocks(contentProp.GetString() ?? string.Empty)
            : string.Empty;

        string? toolName = null;
        string? toolArgs = null;

        if (msg.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0)
        {
            var firstCall = toolCalls[0];
            if (firstCall.TryGetProperty("function", out var fn))
            {
                toolName = fn.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                toolArgs = fn.TryGetProperty("arguments", out var aProp) ? aProp.GetString() : null;
            }
        }

        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(toolName))
        {
            _logger.LogWarning("OpenAI-compatible provider returned empty content and no tool_calls. finish_reason={FinishReason}", finishReason);
        }

        return new AgentRawCompletion(content, toolName, toolArgs, finishReason);
    }

    private static AgentRawCompletion ExtractAnthropicCompletion(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var sb = new StringBuilder();
        string? toolName = null;
        string? toolArgs = null;

        if (root.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArr.EnumerateArray())
            {
                var type = block.TryGetProperty("type", out var tProp) ? tProp.GetString() : null;
                if (type == "tool_use")
                {
                    toolName = block.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                    if (block.TryGetProperty("input", out var inputProp))
                        toolArgs = inputProp.GetRawText();
                }
                else if (block.TryGetProperty("text", out var textProp))
                {
                    sb.Append(textProp.GetString());
                }
            }
        }

        return new AgentRawCompletion(StripThinkBlocks(sb.ToString()), toolName, toolArgs);
    }

    private static AgentRawCompletion ExtractGeminiCompletion(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var sb = new StringBuilder();
        string? toolName = null;
        string? toolArgs = null;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("functionCall", out var fc))
                {
                    toolName = fc.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                    if (fc.TryGetProperty("args", out var argsProp))
                        toolArgs = argsProp.GetRawText();
                }
                else if (part.TryGetProperty("text", out var textProp))
                {
                    sb.Append(textProp.GetString());
                }
            }
        }

        return new AgentRawCompletion(StripThinkBlocks(sb.ToString()), toolName, toolArgs);
    }

    private static string StripThinkBlocks(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return ThinkBlockRegex().Replace(text, string.Empty).Trim();
    }

    private static IReadOnlyList<string> ParseOpenAiModelsArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<string>();
        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                if (!string.IsNullOrWhiteSpace(id) && IsChatCapableModelId(id))
                    list.Add(id);
            }
        }
        return list.Distinct().OrderBy(x => x).ToList();
    }

    private static readonly string[] NonChatModelKeywords =
    [
        "embed", "rerank", "whisper", "tts", "dall-e", "moderation",
        "imagen", "veo", "stable-diffusion", "sdxl", "flux", "guard",
        "shield", "parse", "ocr", "asr", "retriever", "bge-", "e5-",
        "nvclip", "audio", "speech", "transcribe", "realtime", "sora"
    ];

    private static bool IsChatCapableModelId(string modelId)
    {
        var lower = modelId.ToLowerInvariant();
        foreach (var kw in NonChatModelKeywords)
        {
            if (lower.Contains(kw, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string NormalizeOpenAiUrl(string endpointUrl)
    {
        var trimmed = endpointUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
            return $"{trimmed}/chat/completions";
        if (trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            return $"{trimmed}/v1/chat/completions";
        return $"{trimmed}/v1/chat/completions";
    }

    private static string NormalizeAnthropicUrl(string endpointUrl)
    {
        var trimmed = endpointUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            return $"{trimmed}/messages";
        return $"{trimmed}/v1/messages";
    }

    private static string NormalizeGeminiUrl(string endpointUrl, string model)
    {
        var trimmed = endpointUrl.Trim().TrimEnd('/');
        if (trimmed.Contains(":generateContent", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        var cleanModel = model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? model["models/".Length..]
            : model;
        if (trimmed.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            return $"{trimmed}/models/{cleanModel}:generateContent";
        return $"{trimmed}/v1beta/models/{cleanModel}:generateContent";
    }

    private async Task<HttpResponseMessage> SendWithTransientRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        using var firstReq = requestFactory();
        var response = await _httpClient.SendAsync(firstReq, ct);

        if (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable)
        {
            response.Dispose();
            await Task.Delay(800, ct);
            using var retryReq = requestFactory();
            return await _httpClient.SendAsync(retryReq, ct);
        }

        return response;
    }

    private async Task EnsureSuccessOrThrowDetailedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        var snippet = body.Length > 300 ? body[..300] + "..." : body;
        _logger.LogWarning("AI Provider returned {StatusCode}: {Body}", (int)response.StatusCode, snippet);
        throw new HttpRequestException($"Provedor retornou {(int)response.StatusCode} ({response.ReasonPhrase}): {snippet}");
    }

    private static string ExtractWebhookText(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("reply", out var replyProp))
                return replyProp.GetString() ?? rawJson;
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString() ?? rawJson;
        }
        catch { }
        return rawJson;
    }

    private static string StripMarkdownFence(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            content = content[7..];
        else if (content.StartsWith("```"))
            content = content[3..];
        if (content.EndsWith("```"))
            content = content[..^3];
        return content.Trim();
    }

    public static void ValidateUrlSsrf(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("URL inválida.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Apenas protocolos HTTP e HTTPS são permitidos.");
        }

        var host = uri.DnsSafeHost;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Port == 11434)
        {
            return;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Endereços de loopback (localhost) não são permitidos.");
        }

        try
        {
            var ipAddresses = Dns.GetHostAddresses(host);
            foreach (var ip in ipAddresses)
            {
                if (IPAddress.IsLoopback(ip))
                {
                    throw new InvalidOperationException("Endereços IP de loopback são bloqueados.");
                }

                if (IsPrivateOrRestrictedIp(ip))
                {
                    throw new InvalidOperationException("Endereços IP privados ou de metadados são bloqueados.");
                }
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Não foi possível resolver o host '{host}': {ex.Message}");
        }
    }

    private static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            if (bytes[0] == 127) return true;
            if (bytes[0] == 0) return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(IPAddress.IPv6Loopback)) return true;
            if ((bytes[0] == 0xfe) && ((bytes[1] & 0xc0) == 0x80)) return true;
            if ((bytes[0] & 0xfe) == 0xfc) return true;
        }

        return false;
    }

    private static IReadOnlyList<SuggestedTaskModel> ParseTasksJson(string json)
    {
        var list = new List<SuggestedTaskModel>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            JsonElement tasksArray;

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                tasksArray = doc.RootElement;
            }
            else if (doc.RootElement.TryGetProperty("tasks", out var tProp) && tProp.ValueKind == JsonValueKind.Array)
            {
                tasksArray = tProp;
            }
            else
            {
                return list;
            }

            foreach (var item in tasksArray.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var desc = item.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                var priority = item.TryGetProperty("priority", out var prioProp) ? prioProp.GetString() ?? "MEDIUM" : "MEDIUM";
                var dueDate = item.TryGetProperty("dueDate", out var dueProp) ? dueProp.GetString() : null;

                var subtasks = new List<string>();
                if (item.TryGetProperty("subtasks", out var subProp) && subProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in subProp.EnumerateArray())
                    {
                        if (s.ValueKind == JsonValueKind.String)
                        {
                            var sStr = s.GetString();
                            if (!string.IsNullOrWhiteSpace(sStr)) subtasks.Add(sStr);
                        }
                        else if (s.ValueKind == JsonValueKind.Object && s.TryGetProperty("title", out var sTitle))
                        {
                            var sStr = sTitle.GetString();
                            if (!string.IsNullOrWhiteSpace(sStr)) subtasks.Add(sStr);
                        }
                    }
                }

                list.Add(new SuggestedTaskModel
                {
                    Title = title.Trim(),
                    Description = desc,
                    Priority = priority.ToUpperInvariant(),
                    DueDate = dueDate,
                    Subtasks = subtasks
                });
            }
        }
        catch { }

        return list;
    }

    [GeneratedRegex(@"<think>[\s\S]*?</think>", RegexOptions.IgnoreCase)]
    private static partial Regex ThinkBlockRegex();
}
