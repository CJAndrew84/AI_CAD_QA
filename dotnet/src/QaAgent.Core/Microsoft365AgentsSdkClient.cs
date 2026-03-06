using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QaAgent.Core;

/// <summary>
/// Lightweight Microsoft 365 Agents-compatible client.
/// This class is designed so you can switch to the official SDK objects used in
/// microsoft/Agents samples without changing orchestrator/add-in code.
/// </summary>
public sealed class Microsoft365AgentsSdkClient : IAiAdvisor
{
    private readonly HttpClient _httpClient;
    private readonly string? _messagesEndpoint;
    private readonly string? _bearerToken;

    public Microsoft365AgentsSdkClient(HttpClient? httpClient = null, string? messagesEndpoint = null, string? bearerToken = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _messagesEndpoint = messagesEndpoint ?? Environment.GetEnvironmentVariable("M365_AGENTS_MESSAGES_ENDPOINT");
        _bearerToken = bearerToken ?? Environment.GetEnvironmentVariable("M365_AGENTS_BEARER_TOKEN");
    }

    public async Task<CopilotAdvice> SuggestAsync(
        DocumentSnapshot snapshot,
        IReadOnlyList<RuleEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        var failed = evaluations.Where(e => !e.Passed).ToList();
        if (string.IsNullOrWhiteSpace(_messagesEndpoint) || string.IsNullOrWhiteSpace(_bearerToken))
        {
            return OfflineAdvice(failed);
        }

        var prompt = JsonSerializer.Serialize(new
        {
            platform = snapshot.Platform,
            file = snapshot.FilePath,
            failed_rules = failed.Select(r => new
            {
                rule_id = r.RuleId,
                severity = r.Severity.ToString().ToLowerInvariant(),
                message = r.Message,
            })
        });

        // Keep request schema simple and adapter-friendly; swap with SDK calls when wiring the sample.
        var payload = new
        {
            input = prompt,
            context = "CAD QA"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _messagesEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        // Accept a few common response shapes.
        var content = TryGetText(document.RootElement) ?? "Review failed checks.";

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-', ' '))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return new CopilotAdvice(lines.FirstOrDefault() ?? "Review failed checks.", lines.Skip(1).Take(3).ToList());
    }

    private static string? TryGetText(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.String)
            {
                return output.GetString();
            }

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            if (root.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var choiceMessage)
                    && choiceMessage.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString();
                }
            }
        }

        return null;
    }

    private static CopilotAdvice OfflineAdvice(IReadOnlyList<RuleEvaluation> failed)
    {
        if (failed.Count == 0)
        {
            return new CopilotAdvice("All QA checks passed.", ["Proceed to publish."]);
        }

        return new CopilotAdvice(
            "Fix the failed QA rules before publish.",
            failed.Take(3).Select(f => $"{f.RuleId}: {f.Message}").ToList()
        );
    }
}
