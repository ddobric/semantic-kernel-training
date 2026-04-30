using System;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Plugins;

namespace GithubAgent
{

    /// <summary>
    /// This examples demonstrates how to use SK to consume the GitHub agent.
    /// </summary>

    public static class Program
    {
        public static async Task Main()
        {          
            Console.WriteLine("Initialize plugins...");
            Settings settings = GetSettings();
            GitHubPlugin githubPlugin = new(settings.GitHubSettings);

            Console.WriteLine("Creating kernel...");
            IKernelBuilder builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                settings.Model,
                settings.Key);

            builder.Plugins.AddFromObject(githubPlugin);

            Kernel kernel = builder.Build();

            Console.WriteLine("Defining agent...");
            ChatCompletionAgent agent =
                new()
                {
                    Name = "SampleAssistantAgent",
                    Instructions =
                            """
                        You are an agent designed to query and retrieve information from a single GitHub repository in a read-only manner.
                        You are also able to access the profile of the active user.

                        Use the current date and time to provide up-to-date details or time-sensitive responses.

                        The repository you are querying is a private repository with the following name: {{$repository}} inside the organization {{$organization}}

                        The current date and time is: {{$now}}. 
                        """,
                    Kernel = kernel,
                    Arguments =
                        new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
                        {
                        { "repository", "se-cloud-2024-2025" },
                            {"organization", "UniversityOfAppliedSciencesFrankfurt" }
                        }
                };

            Console.WriteLine("Ready!");

            ChatHistory history = [];
            bool isComplete = false;
            do
            {
                Console.WriteLine();
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                else if (input.Trim().ToLower().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    isComplete = true;
                    break;
                }

                history.Add(new ChatMessageContent(AuthorRole.User, input));

                Console.WriteLine();

                DateTime now = DateTime.Now;

                KernelArguments arguments =
                    new()
                    {
                    { "now", $"{now.ToShortDateString()} {now.ToShortTimeString()}" }
                    };

                await foreach (ChatMessageContent response in agent.InvokeAsync(history, arguments))
                {
                    // Display response.
                    Console.WriteLine($"{response.Content}");
                }

            } while (!isComplete);
        }

        private static Settings GetSettings()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())

            //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

            var config = builder.Build();

            var gitHubSettings = new GitHubSettings();

            config.GetSection("GitHubSettings").Bind(gitHubSettings);
      
            return new Settings {  GitHubSettings = gitHubSettings, Key = config["OPENAI_API_KEY"]!, Model = config["OPENAI_CHATCOMPLETION_DEPLOYMENT"]! } ;
        }
    }
}