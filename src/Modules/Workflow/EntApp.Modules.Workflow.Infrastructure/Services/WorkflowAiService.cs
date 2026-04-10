using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.Workflow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Services;

/// <summary>
/// AI destekli workflow oluşturma ve tarif etme servisi.
/// ILlmService (Semantic Kernel) kullanarak doğal dil ↔ Elsa Flowchart JSON dönüşümü yapar.
/// Elsa API üzerinden workflow definition CRUD işlemleri gerçekleştirir.
/// </summary>
public sealed partial class WorkflowAiService : IWorkflowAiService
{
    private readonly ILlmService _llmService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkflowAiService> _logger;

    public WorkflowAiService(
        ILlmService llmService,
        IHttpClientFactory httpClientFactory,
        ILogger<WorkflowAiService> logger)
    {
        _llmService = llmService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // GenerateFromPromptAsync
    // ═══════════════════════════════════════════════════════════

    public async Task<WorkflowGenerationResult> GenerateFromPromptAsync(
        string prompt, string? name = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        _logger.LogInformation("[Workflow:AI] Generating workflow from prompt: {Prompt}", prompt[..Math.Min(prompt.Length, 100)]);

        // 1. LLM'den Elsa Flowchart JSON üret
        var chatRequest = new ChatRequest
        {
            SystemPrompt = GenerationSystemPrompt,
            Messages = [ChatMessage.User(prompt)],
            Temperature = 0.3f, // Düşük temperature → deterministik JSON çıktı
            ModuleName = "Workflow",
        };

        var chatResponse = await _llmService.ChatAsync(chatRequest, ct);

        // 2. JSON'u parse et (LLM bazen ```json ... ``` içine sarar)
        var rawJson = StripMarkdownCodeBlock(chatResponse.Content);

        JsonDocument parsedDoc;
        try
        {
            parsedDoc = JsonDocument.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Workflow:AI] LLM returned invalid JSON. Raw: {Raw}", rawJson[..Math.Min(rawJson.Length, 500)]);
            throw new InvalidOperationException(
                "AI geçerli bir workflow JSON'u üretemedi. Lütfen tarifini daha net ifade et.", ex);
        }

        // 3. Workflow adı ve açıklaması — JSON'dan çıkar veya kullanıcının verdiğini al
        var root = parsedDoc.RootElement;
        var workflowName = name
            ?? TryGetString(root, "name")
            ?? $"AI Workflow {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        var workflowDescription = TryGetString(root, "description") ?? "";

        // 4. Activity sayısını hesapla
        var activityCount = CountActivities(root);

        // 5. Elsa API'ye gönder
        var elsaPayload = BuildElsaSavePayload(root, workflowName, workflowDescription);
        var definitionId = await CreateElsaWorkflowAsync(elsaPayload, ct);

        _logger.LogInformation(
            "[Workflow:AI] Workflow created — Id: {Id}, Name: {Name}, Activities: {Count}, Tokens: {In}+{Out}",
            definitionId, workflowName, activityCount, chatResponse.InputTokens, chatResponse.OutputTokens);

        return new WorkflowGenerationResult(
            definitionId, workflowName, workflowDescription, activityCount,
            $"Workflow başarıyla oluşturuldu. {activityCount} activity içeriyor.");
    }

    // ═══════════════════════════════════════════════════════════
    // DescribeWorkflowAsync
    // ═══════════════════════════════════════════════════════════

    public async Task<WorkflowDescriptionResult> DescribeWorkflowAsync(
        string definitionId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);

        _logger.LogInformation("[Workflow:AI] Describing workflow: {DefinitionId}", definitionId);

        // 1. Elsa API'den workflow JSON'unu çek
        var workflowJson = await GetElsaWorkflowJsonAsync(definitionId, ct);
        if (workflowJson is null)
            throw new KeyNotFoundException($"Workflow '{definitionId}' bulunamadı.");

        // Name bilgisini JSON'dan çıkar
        using var doc = JsonDocument.Parse(workflowJson);
        var rootElement = doc.RootElement;
        var workflowName = TryGetString(rootElement, "name")
            ?? TryGetString(rootElement, "materializedName")
            ?? "İsimsiz Workflow";

        // 2. LLM'den tarif iste
        var chatRequest = new ChatRequest
        {
            SystemPrompt = DescriptionSystemPrompt,
            Messages = [ChatMessage.User($"Aşağıdaki Elsa workflow JSON'unu Türkçe olarak detaylı tarif et:\n\n{workflowJson}")],
            Temperature = 0.5f,
            ModuleName = "Workflow",
        };

        var chatResponse = await _llmService.ChatAsync(chatRequest, ct);

        // 3. Activity listesini JSON'dan çıkar
        var activities = ExtractActivitySummaries(rootElement);

        _logger.LogInformation(
            "[Workflow:AI] Described workflow {Id} — {ActivityCount} activities, Tokens: {In}+{Out}",
            definitionId, activities.Count, chatResponse.InputTokens, chatResponse.OutputTokens);

        return new WorkflowDescriptionResult(
            definitionId, workflowName, chatResponse.Content, activities);
    }

    // ═══════════════════════════════════════════════════════════
    // Elsa API Helpers
    // ═══════════════════════════════════════════════════════════

    private async Task<string> CreateElsaWorkflowAsync(string jsonPayload, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ElsaApi");
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("workflow-definitions", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[Workflow:AI] Elsa API error: {Status} — {Body}",
                response.StatusCode, responseBody[..Math.Min(responseBody.Length, 500)]);
            throw new InvalidOperationException(
                $"Elsa API hatası: {response.StatusCode}. Workflow oluşturulamadı.");
        }

        using var doc = JsonDocument.Parse(responseBody);
        // Elsa response: { workflowDefinition: { definitionId: "..." } } veya { definitionId: "..." }
        if (doc.RootElement.TryGetProperty("workflowDefinition", out var wfDef) &&
            wfDef.TryGetProperty("definitionId", out var defId1))
        {
            return defId1.GetString()!;
        }
        if (doc.RootElement.TryGetProperty("definitionId", out var defId2))
        {
            return defId2.GetString()!;
        }

        throw new InvalidOperationException("Elsa API yanıtından definitionId okunamadı.");
    }

    private async Task<string?> GetElsaWorkflowJsonAsync(string definitionId, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ElsaApi");
        var response = await client.GetAsync($"workflow-definitions/{definitionId}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════
    // JSON Helpers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// LLM'in döndürdüğü JSON'u Elsa SaveWorkflowDefinitionRequest formatına sarar.
    /// LLM sadece root activity (Flowchart) döner, biz model wrapper'ını ekliyoruz.
    /// </summary>
    private static string BuildElsaSavePayload(JsonElement llmOutput, string name, string description)
    {
        // LLM'in root activity'si (Flowchart) direkt root ise onu al
        JsonElement rootActivity;
        if (llmOutput.TryGetProperty("root", out var existingRoot))
        {
            rootActivity = existingRoot;
        }
        else if (TryGetString(llmOutput, "type")?.Contains("Flowchart", StringComparison.OrdinalIgnoreCase) == true)
        {
            // LLM direkt Flowchart objesini döndü
            rootActivity = llmOutput;
        }
        else
        {
            // Tüm output'u root olarak kullan
            rootActivity = llmOutput;
        }

        var payload = new
        {
            model = new
            {
                name,
                description,
                root = rootActivity
            },
            publish = false
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(property, out var val) &&
            val.ValueKind == JsonValueKind.String)
        {
            return val.GetString();
        }
        return null;
    }

    private static int CountActivities(JsonElement root)
    {
        var count = 0;
        CountActivitiesRecursive(root, ref count);
        return count;
    }

    private static void CountActivitiesRecursive(JsonElement element, ref int count)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out _))
                count++;

            if (element.TryGetProperty("activities", out var activities) &&
                activities.ValueKind == JsonValueKind.Array)
            {
                foreach (var activity in activities.EnumerateArray())
                    CountActivitiesRecursive(activity, ref count);
            }
        }
    }

    private static List<ActivitySummary> ExtractActivitySummaries(JsonElement root)
    {
        var summaries = new List<ActivitySummary>();
        ExtractActivitiesRecursive(root, summaries);
        return summaries;
    }

    private static void ExtractActivitiesRecursive(JsonElement element, List<ActivitySummary> summaries)
    {
        if (element.ValueKind != JsonValueKind.Object) return;

        // "root" property varsa ona bak
        if (element.TryGetProperty("root", out var rootProp))
            ExtractActivitiesRecursive(rootProp, summaries);

        if (element.TryGetProperty("activities", out var activities) &&
            activities.ValueKind == JsonValueKind.Array)
        {
            foreach (var activity in activities.EnumerateArray())
            {
                var type = TryGetString(activity, "type") ?? "Unknown";
                var displayName = TryGetString(activity, "name")
                    ?? TryGetString(activity, "displayName")
                    ?? type;
                var desc = TryGetString(activity, "description") ?? "";
                summaries.Add(new ActivitySummary(type, displayName, desc));

                // Nested activities (composite activities)
                ExtractActivitiesRecursive(activity, summaries);
            }
        }
    }

    /// <summary>
    /// LLM çıktısından markdown code block'u temizler.
    /// Örn: ```json\n{...}\n``` → {...}
    /// </summary>
    private static string StripMarkdownCodeBlock(string content)
    {
        content = content.Trim();
        // ```json ... ``` veya ``` ... ```
        var match = CodeBlockRegex().Match(content);
        return match.Success ? match.Groups[1].Value.Trim() : content;
    }

    [GeneratedRegex(@"```(?:json)?\s*\n?(.*?)\n?\s*```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    // ═══════════════════════════════════════════════════════════
    // System Prompts
    // ═══════════════════════════════════════════════════════════

    private const string GenerationSystemPrompt = """
        Sen bir Elsa Workflows v3 tasarımcısısın. Kullanıcının doğal dilde tarif ettiği iş akışlarını
        Elsa Flowchart JSON formatında üretiyorsun.

        ## Kurallar
        1. SADECE aşağıdaki custom activity'leri ve Elsa built-in activity'leri kullanabilirsin.
        2. Her zaman geçerli JSON döndür — markdown, açıklama veya başka metin EKLEME.
        3. Root activity tipi mutlaka "Elsa.Flowchart" olmalı.
        4. Her activity'nin benzersiz bir "id" alanı olmalı (ör: "activity-1", "activity-2" vb.).
        5. Bağlantılar "connections" dizisinde tanımlanmalı.
        6. Türkçe name/description kullan.
        7. Her activity'nin doğru input property'lerini doldur.

        ## Custom Activity Katalogu (EntApp — Ticket Management)

        ### 1. Change Ticket Status
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.ChangeTicketStatusActivity"
        - Inputs:
          - TicketId (Guid): Ticket ID — genelde workflow'un başlangıç parametresinden gelir
          - NewStatus (string): "New" | "Open" | "InProgress" | "WaitingForInfo" | "Escalated" | "Resolved" | "Closed" | "Cancelled" | "Reopened"
          - Reason (string, optional): Durum değişikliği nedeni

        ### 2. Route to Queue
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.RouteToQueueActivity"
        - Inputs:
          - TicketId (Guid)
          - QueueId (Guid): Hedef kuyruk ID'si
        - Outputs: RoutedQueueId

        ### 3. Assign Ticket
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.AssignTicketActivity"
        - Inputs:
          - TicketId (Guid)
          - AssigneeUserId (Guid)
        - Outputs: AssignedTo

        ### 4. Wait for Approval (Blocking)
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.WaitForApprovalActivity"
        - Inputs:
          - TicketId (Guid)
          - ApprovalLabel (string): "Talep Üzerine Alma", "Bütçe Onayı" vb.
          - ApproverUserId (Guid?, optional)
          - TimeoutHours (int): 0 = timeout yok
        - Outcomes: "Approved", "Rejected"
        - Outputs: Decision, Comment
        - NOT: Bu activity workflow'u duraklatır (blocking). Frontend bookmark sorgulayarak buton gösterir.

        ### 5. Send Notification
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.SendNotificationActivity"
        - Inputs:
          - TicketId (Guid)
          - RecipientUserId (Guid)
          - Template (string): Bildirim mesajı
        - Outputs: NotificationId

        ### 6. Add Comment
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.AddCommentActivity"
        - Inputs:
          - TicketId (Guid)
          - Content (string): Yorum içeriği
          - IsInternal (bool): true = dahili not
        - Outputs: CommentId

        ### 7. Get Ticket Details
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.GetTicketDetailsActivity"
        - Inputs:
          - TicketId (Guid)
        - Outputs: Status, Priority, Title, CategoryName, QueueName, AssigneeUserId, ReporterUserId
        - Outcomes: "Done", "NotFound"

        ### 8. Check SLA
        - Type: "EntApp.Modules.Workflow.Infrastructure.Activities.CheckSlaActivity"
        - Inputs:
          - TicketId (Guid)
        - Outputs: ResponseBreached, ResolutionBreached
        - Outcomes: "OK", "ResponseBreached", "ResolutionBreached"

        ## Elsa Built-in Activity'ler (sık kullanılanlar)
        - "Elsa.HttpEndpoint" — HTTP trigger (Method, Path, Content)
        - "Elsa.If" — Koşul (Condition expression)
        - "Elsa.Switch" — Çoklu koşul dallanma
        - "Elsa.SetVariable" — Değişken ata
        - "Elsa.Delay" — Bekleme süresi
        - "Elsa.Fork" — Paralel dallanma
        - "Elsa.Join" — Paralel dalları birleştir

        ## Elsa Flowchart JSON Formatı

        ```json
        {
          "name": "Workflow Adı",
          "description": "Kısa açıklama",
          "root": {
            "type": "Elsa.Flowchart",
            "activities": [
              {
                "id": "activity-1",
                "type": "Elsa.HttpEndpoint",
                "name": "TicketCreatedTrigger",
                "metadata": { "displayName": "Talep Oluşturuldu" },
                "customProperties": {
                  "path": { "expression": { "type": "Literal", "value": "/workflows/ticket-created" } },
                  "method": { "expression": { "type": "Literal", "value": "POST" } },
                  "readContent": { "expression": { "type": "Literal", "value": "true" } }
                }
              },
              {
                "id": "activity-2",
                "type": "EntApp.Modules.Workflow.Infrastructure.Activities.ChangeTicketStatusActivity",
                "name": "OpenTicket",
                "metadata": { "displayName": "Durumu Open Yap" },
                "customProperties": {
                  "ticketId": { "expression": { "type": "JavaScript", "value": "Activities.TicketCreatedTrigger.ParsedContent.ticketId" } },
                  "newStatus": { "expression": { "type": "Literal", "value": "Open" } },
                  "reason": { "expression": { "type": "Literal", "value": "Workflow tarafından otomatik açıldı" } }
                }
              }
            ],
            "connections": [
              { "source": { "activity": "activity-1", "port": "Done" }, "target": { "activity": "activity-2", "port": "In" } }
            ]
          }
        }
        ```

        ## TicketId Referansı
        Workflow genelde bir HTTP trigger ile başlar. TicketId'yi almak için:
        - JavaScript expression: `Activities.TicketCreatedTrigger.ParsedContent.ticketId`
        - Veya bir SetVariable ile workflow değişkeni olarak kaydet

        ## Wait for Approval Dallanma
        WaitForApproval activity'si "Approved" ve "Rejected" outcome'ları verir.
        Connection'larda port olarak "Approved" ve "Rejected" kullan:
        ```json
        { "source": { "activity": "wait-1", "port": "Approved" }, "target": { "activity": "next-on-approve" } }
        { "source": { "activity": "wait-1", "port": "Rejected" }, "target": { "activity": "next-on-reject" } }
        ```

        SADECE JSON döndür, başka metin yazma!
        """;

    private const string DescriptionSystemPrompt = """
        Sen bir iş süreçleri analisti ve Elsa Workflows v3 uzmanısın.
        Sana verilen Elsa workflow JSON'unu Türkçe doğal dilde, anlaşılır bir şekilde tarif edeceksin.

        ## Kurallar
        1. Workflow'un amacını ve genel akışını ilk paragrafta özetle.
        2. Her adımı sırasıyla listele ve ne yaptığını açıkla.
        3. Dallanma noktalarını (If, Switch, Wait for Approval outcomes) net belirt.
        4. Blocking activity'leri (WaitForApproval) vurgula — nerede durak noktası var, ne bekliyor.
        5. Teknik detail (JSON, ID) yazma — sadece iş mantığını açıkla.
        6. Yanıtını düz metin olarak ver, markdown formatında.
        7. Sonunda "Bu workflow'u değiştirmek istersen, yukarıdaki tarifi düzenleyip bana gönder" de.

        ## Custom Activity Referansı
        - ChangeTicketStatusActivity → Ticket durumunu değiştirir
        - RouteToQueueActivity → Ticket'ı bir hizmet kuyruğuna yönlendirir
        - AssignTicketActivity → Ticket'ı bir kullanıcıya atar
        - WaitForApprovalActivity → Workflow duraklar, kullanıcı onayı/reddi bekler
        - SendNotificationActivity → Bildirim gönderir
        - AddCommentActivity → Ticket'a otomatik yorum ekler
        - GetTicketDetailsActivity → Ticket bilgilerini getirir
        - CheckSlaActivity → SLA ihlali kontrol eder (OK/ResponseBreached/ResolutionBreached)
        """;
}
