using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AgentFramework_Samples.GettingStarted
{
    public class AgentWithMemory
    {
        public static async Task RunAsync()
        {
            Helpers.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            ChatClient chatClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName);

            // Create the agent and provide a factory to add our custom memory component to
            // all sessions created by the agent. Here each new memory component will have its own
            // user info object, so each session will have its own memory.
            // In real world applications/services, where the user info would be persisted in a database,
            // and preferably shared between multiple sessions used by the same user, ensure that the
            // factory reads the user id from the current context and scopes the memory component
            // and its storage to that user id.
            AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new() { Instructions = "You are a friendly assistant. Always address the user by their name." },
                AIContextProviders = [new CustomMemory<UserInfo>(chatClient.AsIChatClient())]
            });

            // Create a new session for the conversation.
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(">> Use session with blank memory\n");

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Hello, what is the square root of 9?", session));
            Console.WriteLine(await agent.RunAsync("My name is Ruaidhrí", session));
            Console.WriteLine(await agent.RunAsync("I am 20 years old", session));

            // We can serialize the session. The serialized state will include the state of the memory component.
            JsonElement sesionElement = await agent.SerializeSessionAsync(session);

            Console.WriteLine("\n>> Use deserialized session with previously created memories\n");

            // Later we can deserialize the session and continue the conversation with the previous memory component state.
            var deserializedSession = await agent.DeserializeSessionAsync(sesionElement);
            Console.WriteLine(await agent.RunAsync("What is my name and age?", deserializedSession));

            Console.WriteLine("\n>> Read memories using memory component\n");

            // It's possible to access the memory component via the agent's GetService method.
            var userInfo = agent.GetService<CustomMemory<UserInfo>>()?.GetValue(deserializedSession);

            // Output the user info that was captured by the memory component.
            Console.WriteLine($"MEMORY - User Name: {userInfo?.UserName}");
            Console.WriteLine($"MEMORY - User Age: {userInfo?.UserAge}");

            Console.WriteLine("\n>> Use new session with previously created memories\n");

            // It is also possible to set the memories using a memory component on an individual session.
            // This is useful if we want to start a new session, but have it share the same memories as a previous session.
            var newSession = await agent.CreateSessionAsync();
            if (userInfo is not null && agent.GetService<CustomMemory<UserInfo>>() is CustomMemory<UserInfo> newSessionMemory)
            {
                newSessionMemory.SetValue(newSession, userInfo);
            }

            // Invoke the agent and output the text result.
            // This time the agent should remember the user's name and use it in the response.
            Console.WriteLine(await agent.RunAsync("What is my name and age?", newSession));
        }
    }



    /// <summary>
    /// Sample memory component = ContextProvider that can remember a user's name and age.
    /// </summary>
    internal sealed class CustomMemory<T> : AIContextProvider
        where T : class, IContextValue, new()
    {
        private readonly ProviderSessionState<T> _sessionState;
        private IReadOnlyList<string>? _stateKeys;
        private readonly IChatClient _chatClient;

        public CustomMemory(IChatClient chatClient, Func<AgentSession?, T>? stateInitializer = null)
        {
            this._sessionState = new ProviderSessionState<T>(
               stateInitializer ?? (_ => new T()),
               this.GetType().Name);
            this._chatClient = chatClient;
        }

        public override IReadOnlyList<string> StateKeys
        {
            get
            {
                return this._stateKeys ??= [this._sessionState.StateKey];
            }
        }

        public T? GetValue(AgentSession session)
            => this._sessionState.GetOrInitializeState(session);

        public void SetValue(AgentSession session, T val)
            => this._sessionState.SaveState(session, val);

        protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
        {
            var val = this._sessionState.GetOrInitializeState(context.Session);

            // Try and extract the state.
            if ((!val.IsPopulated) && context.RequestMessages.Any(x => x.Role == ChatRole.User))
            {
                var response = await this._chatClient.GetResponseAsync<T>(
                    context.RequestMessages,
                    new ChatOptions()
                    {
                        Instructions = "Extract the state values from the message if present. If not present return nulls."
                    },
                    cancellationToken: cancellationToken);

                val = response.Result;
            }

            this._sessionState.SaveState(context.Session, val!);
        }

        protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
        {
            var val = this._sessionState.GetOrInitializeState(context.Session);

            StringBuilder instructions = new();

            if (val.IsPopulated == false)
            {
                // If we don't already know the user's name and age, add instructions to ask for them, otherwise just provide what we have to the context.
                instructions
                    .AppendLine(
                        val.GetInstructions());
            }

            return new ValueTask<AIContext>(new AIContext
            {
                Instructions = instructions.ToString()
            });
        }
    }

    public interface IContextValue
    {
        string GetInstructions();

        public bool IsPopulated { get; }
    }

    internal sealed class UserInfo : IContextValue
    {
        public string? UserName { get; set; }
        public int? UserAge { get; set; }

        public bool IsPopulated
        {
            get
            {
                return this.UserName is not null && this.UserAge is not null;
            }
        }

        public string GetInstructions()
        {
            if (this.UserName is null)
                return "Ask the user to provide his name and politely decline to answer any questions until the state is provided.";
            if (this.UserAge is null)
                return "Ask user politly to provide his age.";

            return String.Empty;
        }

        public override string ToString()
        {
            return $"User: {UserName}, Age: {UserAge}";
        }
    }
}
