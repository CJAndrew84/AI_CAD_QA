using System.Text;
using System.Text.Json;
using QaAgent.Core;

var app = new McpServerApp();
await app.RunAsync();

internal sealed class McpServerApp
{
    private readonly Stream _input = Console.OpenStandardInput();
    private readonly Stream _output = Console.OpenStandardOutput();

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var request = await ReadMessageAsync(cancellationToken);
            if (request is null)
            {
                break;
            }

            await HandleRequestAsync(request.Value, cancellationToken);
        }
    }

    private async Task HandleRequestAsync(JsonElement request, CancellationToken cancellationToken)
    {
        var id = request.TryGetProperty("id", out var idElement) ? idElement : default;
        var method = request.GetProperty("method").GetString();
        var @params = request.TryGetProperty("params", out var p) ? p : default;

        try
        {
            object result = method switch
            {
                "initialize" => HandleInitialize(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCallAsync(@params, cancellationToken),
                "ping" => new { },
                _ => throw new InvalidOperationException($"Method not supported: {method}")
            };

            await WriteMessageAsync(new
            {
                jsonrpc = "2.0",
                id = JsonElementToObject(id),
                result
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteMessageAsync(new
            {
                jsonrpc = "2.0",
                id = JsonElementToObject(id),
                error = new
                {
                    code = -32603,
                    message = ex.Message
                }
            }, cancellationToken);
        }
    }

    private static object HandleInitialize() => new
    {
        protocolVersion = "2024-11-05",
        serverInfo = new
        {
            name = "qaagent-mcp",
            version = "0.1.0"
        },
        capabilities = new
        {
            tools = new { }
        }
    };

    private static object HandleToolsList() => new
    {
        tools = new object[]
        {
            new
            {
                name = "qa.evaluate_file",
                description = "Evaluate QA XML rules against a CAD file and return pass/fail with advice.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        platform = new { type = "string", @enum = new[] { "AutoCAD", "MicroStation" } },
                        filePath = new { type = "string" },
                        fileFormat = new { type = "string" },
                        masterXml = new { type = "string" },
                        rulesDir = new { type = "string" }
                    },
                    required = new[] { "platform", "filePath", "fileFormat" }
                }
            },
            new
            {
                name = "qa.list_rule_files",
                description = "List rule files referenced by the configured Master.xml.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        masterXml = new { type = "string" },
                        rulesDir = new { type = "string" }
                    },
                    required = Array.Empty<string>()
                }
            }
        }
    };

    private static async Task<object> HandleToolsCallAsync(JsonElement @params, CancellationToken cancellationToken)
    {
        var name = @params.GetProperty("name").GetString() ?? string.Empty;
        var args = @params.TryGetProperty("arguments", out var argElement)
            ? argElement
            : JsonDocument.Parse("{}").RootElement;

        return name switch
        {
            "qa.evaluate_file" => await EvaluateFileAsync(args, cancellationToken),
            "qa.list_rule_files" => ListRuleFiles(args),
            _ => throw new InvalidOperationException($"Unknown tool: {name}")
        };
    }

    private static async Task<object> EvaluateFileAsync(JsonElement args, CancellationToken cancellationToken)
    {
        var platform = args.GetProperty("platform").GetString() ?? "AutoCAD";
        var filePath = args.GetProperty("filePath").GetString() ?? string.Empty;
        var fileFormat = args.GetProperty("fileFormat").GetString() ?? "dwg";

        var masterXml = args.TryGetProperty("masterXml", out var m)
            ? m.GetString()
            : Environment.GetEnvironmentVariable("QA_MASTER_XML");
        var rulesDir = args.TryGetProperty("rulesDir", out var r)
            ? r.GetString()
            : Environment.GetEnvironmentVariable("QA_RULES_DIR");

        masterXml ??= Path.Combine(Environment.CurrentDirectory, "Rule XML Masters", "Master.xml");
        rulesDir ??= Path.Combine(Environment.CurrentDirectory, "Rule XML Files");

        var orchestrator = new QaCopilotOrchestrator(masterXml, rulesDir);
        var snapshot = new DocumentSnapshot(platform, filePath, fileFormat);
        var result = await orchestrator.RunAsync(snapshot, cancellationToken);

        var failed = result.Evaluations.Where(e => !e.Passed).ToList();
        var payload = new
        {
            platform,
            filePath,
            totalRules = result.Evaluations.Count,
            failedRules = failed.Count,
            advice = new
            {
                summary = result.Advice.Summary,
                actions = result.Advice.Actions
            },
            failures = failed.Select(f => new
            {
                f.RuleId,
                severity = f.Severity.ToString(),
                f.Message,
                f.SourceFile
            }).ToList()
        };

        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
                }
            }
        };
    }

    private static object ListRuleFiles(JsonElement args)
    {
        var masterXml = args.TryGetProperty("masterXml", out var m)
            ? m.GetString()
            : Environment.GetEnvironmentVariable("QA_MASTER_XML");
        var rulesDir = args.TryGetProperty("rulesDir", out var r)
            ? r.GetString()
            : Environment.GetEnvironmentVariable("QA_RULES_DIR");

        masterXml ??= Path.Combine(Environment.CurrentDirectory, "Rule XML Masters", "Master.xml");
        rulesDir ??= Path.Combine(Environment.CurrentDirectory, "Rule XML Files");

        var loader = new XmlRuleLoader(masterXml, rulesDir);
        var rules = loader.Load();
        var files = rules.Select(rf => rf.SourceFile).Distinct().OrderBy(x => x).ToList();

        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(new { masterXml, rulesDir, files }, new JsonSerializerOptions { WriteIndented = true })
                }
            }
        };
    }

    private async Task<JsonElement?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await ReadLineAsync(_input, cancellationToken);
            if (line is null)
            {
                return null;
            }

            if (line.Length == 0)
            {
                break;
            }

            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                headers[key] = value;
            }
        }

        if (!headers.TryGetValue("Content-Length", out var lenText) || !int.TryParse(lenText, out var length))
        {
            throw new InvalidOperationException("Missing Content-Length header.");
        }

        var buffer = new byte[length];
        var read = 0;
        while (read < length)
        {
            var n = await _input.ReadAsync(buffer.AsMemory(read, length - read), cancellationToken);
            if (n == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading MCP payload.");
            }
            read += n;
        }

        using var doc = JsonDocument.Parse(buffer);
        return doc.RootElement.Clone();
    }

    private async Task WriteMessageAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

        await _output.WriteAsync(header, cancellationToken);
        await _output.WriteAsync(body, cancellationToken);
        await _output.FlushAsync(cancellationToken);
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var b = new byte[1];
            var n = await stream.ReadAsync(b, cancellationToken);
            if (n == 0)
            {
                return bytes.Count == 0 ? null : Encoding.ASCII.GetString(bytes.ToArray()).TrimEnd('\r');
            }

            if (b[0] == '\n')
            {
                return Encoding.ASCII.GetString(bytes.ToArray()).TrimEnd('\r');
            }

            bytes.Add(b[0]);
        }
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => JsonSerializer.Deserialize<object>(element.GetRawText())
        };
    }
}
