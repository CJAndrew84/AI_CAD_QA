using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QaAgent.Core;

public sealed class MsCopilotClient : IAiAdvisor
{
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint;
    private readonly string? _apiKey;

    public MsCopilotClient(HttpClient? httpClient = null, string? endpoint = null, string? apiKey = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _endpoint = endpoint ?? Environment.GetEnvironmentVariable("MS_COPILOT_ENDPOINT");
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("MS_COPILOT_API_KEY");
    }

    public async Task<CopilotAdvice> SuggestAsync(DocumentSnapshot snapshot, IReadOnlyList<RuleEvaluation> evaluations, CancellationToken cancellationToken = default)
    {
        var failed = evaluations.Where(e => !e.Passed).ToList();
        if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return OfflineAdvice(failed);
        }

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = "You are an engineering CAD QA copilot. Give concise, actionable fixes." },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        platform = snapshot.Platform,
                        file = snapshot.FilePath,
                        failed_rules = failed.Select(r => new
                        {
                            rule_id = r.RuleId,
                            severity = r.Severity.ToString().ToLowerInvariant(),
                            message = r.Message
                        })
                    })
                }
            },
            temperature = 0.2
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return OfflineAdvice(failed);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Review failed checks.";

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.TrimStart('-', ' '))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            return new CopilotAdvice(lines.FirstOrDefault() ?? "Review failed checks.", lines.Skip(1).Take(3).ToList());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return OfflineAdvice(failed);
        }
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
