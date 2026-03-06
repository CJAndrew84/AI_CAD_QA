namespace QaAgent.Core;

public static class AiAdvisorFactory
{
    public static IAiAdvisor CreateDefault()
    {
        var hasAgentsConfig =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("M365_AGENTS_MESSAGES_ENDPOINT"))
            && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("M365_AGENTS_BEARER_TOKEN"));

        if (hasAgentsConfig)
        {
            return new Microsoft365AgentsSdkClient();
        }

        return new MsCopilotClient();
    }
}
