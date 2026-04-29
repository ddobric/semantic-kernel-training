using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentFramework_Samples.Providers.FoundryLocal
{
    public static class FoundryLocalExtensions
    {
        /// <summary>
        /// Creates a <see cref="ChatClientAgent"/> from a <see cref="FoundryLocalChatClient"/>.
        /// The client already implements <see cref="IChatClient"/>, so it is used directly
        /// (with an optional pipeline transformation) and wrapped in a <see cref="ChatClientAgent"/>.
        /// </summary>
        public static ChatClientAgent AsAIAgent(
            this FoundryLocalChatClient client,
            string? instructions = null,
            string? name = null,
            string? description = null,
            IList<AITool>? tools = null,
            int? defaultMaxTokens = null,
            Func<IChatClient, IChatClient>? clientFactory = null,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? services = null)
        {

            IChatClient chatClient = client;// client.GetChatClient(model: "gpt-4o");

            // Apply an optional client pipeline transformation (e.g. adding middleware).
            if (clientFactory is not null)
                chatClient = clientFactory(chatClient);

            ChatClientAgentOptions agentOptions = new()
            {
                Name = name,
                Description = description,
                ChatOptions = new()
                {
                    Instructions = instructions,
                    Tools = tools,
                }
            };

            if (defaultMaxTokens.HasValue)
                agentOptions.ChatOptions.MaxOutputTokens = defaultMaxTokens.Value;

            return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services);
        }
    }
}
