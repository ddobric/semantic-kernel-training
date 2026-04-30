using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using OpenAI.Files;
using static Azure.Core.HttpHeader;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure.AI.OpenAI;
namespace OpenAIInterpreterAgent;

public static class Program
{
    /// <summary>
    /// Demonstrates Assistent with interpreter and File Tool.
    /// Try using these suggested inputs:
    /// -Compare the files to determine the number of countries do not have a state or province defined compared to the total count
    /// -Create a table for countries with state or province defined.Include the count of states or provinces and the total population
    /// -Provide a bar chart for countries whose names start with the same letter and sort the x axis by highest count to lowest (include all countries)
    /// </summary>
    /// <returns></returns>
    public static async Task Main()
    {  // Load configuration from environment variables or user secrets.
        Settings settings = new();

        // Initialize the clients
        AzureOpenAIClient client = OpenAIAssistantAgent.CreateAzureOpenAIClient(new AzureCliCredential(), new Uri(settings.AzureOpenAI.Endpoint));
        //OpenAIClient client = OpenAIAssistantAgent.CreateOpenAIClient(new ApiKeyCredential(settings.OpenAI.ApiKey)));
        AssistantClient assistantClient = client.GetAssistantClient();
        OpenAIFileClient fileClient = client.GetOpenAIFileClient();

        // Upload files
        Console.WriteLine("Uploading files...");
        OpenAIFile fileDataCountryDetail = await fileClient.UploadFileAsync("PopulationByAdmin1.csv", FileUploadPurpose.Assistants);
        OpenAIFile fileDataCountryList = await fileClient.UploadFileAsync("PopulationByCountry.csv", FileUploadPurpose.Assistants);

        // Define assistant
        Console.WriteLine("Defining assistant...");
        Assistant assistant =
            await assistantClient.CreateAssistantAsync(
                settings.AzureOpenAI.ChatModelDeployment,
                name: "SampleAssistantAgent",
                instructions:
                        """
                        Analyze the available data to provide an answer to the user's question.
                        Always format response using markdown.
                        Always include a numerical index that starts at 1 for any lists or tables.
                        Always sort lists in ascending order.
                        """,
                enableCodeInterpreter: true,
                codeInterpreterFileIds: [fileDataCountryList.Id, fileDataCountryDetail.Id]);

        // Create agent
        OpenAIAssistantAgent agent = new(assistant, assistantClient);

        // Create the conversation thread
        Console.WriteLine("Creating thread...");
        AssistantAgentThread agentThread = new();

        Console.WriteLine("Ready!");

        try
        {
            bool isComplete = false;
            List<string> fileIds = [];
            do
            {
                Console.WriteLine();
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                {
                    isComplete = true;
                    break;
                }

                var message = new ChatMessageContent(AuthorRole.User, input);

                Console.WriteLine();

                bool isCode = false;
                await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
                {
                    if (isCode != (response.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false))
                    {
                        Console.WriteLine();
                        isCode = !isCode;
                    }

                    // Display response.
                    Console.Write($"{response.Content}");

                    // Capture file IDs for downloading.
                    fileIds.AddRange(response.Items.OfType<StreamingFileReferenceContent>().Select(item => item.FileId));
                }
                Console.WriteLine();

                // Download any files referenced in the response.
                await DownloadResponseImageAsync(fileClient, fileIds);
                fileIds.Clear();

            } while (!isComplete);
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("Cleaning-up...");
            await Task.WhenAll(
                [
                    agentThread.DeleteAsync(),
                    assistantClient.DeleteAssistantAsync(assistant.Id),
                    fileClient.DeleteFileAsync(fileDataCountryList.Id),
                    fileClient.DeleteFileAsync(fileDataCountryDetail.Id),
                ]);
        }
    }


    public static async Task Main2()
    {
        // Load configuration from environment variables or user secrets.
        Settings settings = GetSettings();

        OpenAIClientProvider clientProvider =
            OpenAIClientProvider.ForOpenAI(new ApiKeyCredential(settings.Key));

        Console.WriteLine("Uploading files...");
        OpenAIFileClient fileClient = clientProvider.Client.GetOpenAIFileClient();
        OpenAIFile fileDataCountryDetail = await fileClient.UploadFileAsync("Data/PopulationByAdmin1.csv", FileUploadPurpose.Assistants);
        OpenAIFile fileDataCountryList = await fileClient.UploadFileAsync("Data/PopulationByCountry.csv", FileUploadPurpose.Assistants);
        
        AssistantClient assistantClient = new AssistantClient(new ApiKeyCredential(settings.Key));

        Console.WriteLine("Defining agent...");
        OpenAIAssistantAgent agent =
            await OpenAIAssistantAgent.CreateAsync(
                clientProvider,
                new OpenAIAssistantDefinition(settings.Model)
                {
                    Name = "SampleAssistantAgent",
                    Instructions =
                        """
                        Analyze the available data to provide an answer to the user's question.
                        Always format response using markdown.
                        Always include a numerical index that starts at 1 for any lists or tables.
                        Always sort lists in ascending order.
                        """,
                    EnableCodeInterpreter = true,
                    CodeInterpreterFileIds = [fileDataCountryList.Id, fileDataCountryDetail.Id],
                },
                new Kernel());

        Console.WriteLine("Creating thread...");

        var thread = await assistantClient.CreateThreadAsync();
        
        //string threadId = await agent.CreateThreadAsync();

        Console.WriteLine("Ready!");

        try
        {
            bool isComplete = false;
            List<string> fileIds = [];
            do
            {
                Console.WriteLine();
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                {
                    isComplete = true;
                    break;
                }

                await agent.AddChatMessageAsync(thread, new ChatMessageContent(AuthorRole.User, input));

                Console.WriteLine();

                bool isCode = false;
                await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(threadId))
                {
                    if (isCode != (response.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false))
                    {
                        Console.WriteLine();
                        isCode = !isCode;
                    }

                    // Display response.
                    Console.Write($"{response.Content}");

                    // Capture file IDs for downloading.
                    fileIds.AddRange(response.Items.OfType<StreamingFileReferenceContent>().Select(item => item.FileId));
                }
                Console.WriteLine();

                // Download any files referenced in the response.
                await DownloadResponseImageAsync(fileClient, fileIds);
                fileIds.Clear();

            } while (!isComplete);
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("Cleaning-up...");
            await Task.WhenAll(
                [
                    agent.DeleteThreadAsync(threadId),
                    agent.DeleteAsync(),
                    fileClient.DeleteFileAsync(fileDataCountryList.Id),
                    fileClient.DeleteFileAsync(fileDataCountryDetail.Id),
                ]);
        }
    }

    private static async Task DownloadResponseImageAsync(OpenAIFileClient client, ICollection<string> fileIds)
    {
        if (fileIds.Count > 0)
        {
            Console.WriteLine();
            foreach (string fileId in fileIds)
            {
                await DownloadFileContentAsync(client, fileId, launchViewer: true);
            }
        }
    }

    private static async Task DownloadFileContentAsync(OpenAIFileClient client, string fileId, bool launchViewer = false)
    {
        OpenAIFile fileInfo = client.GetFile(fileId);
        if (fileInfo.Purpose == FilePurpose.AssistantsOutput)
        {
            string filePath =
                Path.Combine(
                    Path.GetTempPath(),
                    Path.GetFileName(Path.ChangeExtension(fileInfo.Filename, ".png")));

            BinaryData content = await client.DownloadFileAsync(fileId);
            await using FileStream fileStream = new(filePath, FileMode.Create);
            await content.ToStream().CopyToAsync(fileStream);
            Console.WriteLine($"File saved to: {filePath}.");

            if (launchViewer)
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C start {filePath}"
                    });
            }
        }
    }

    private static Settings GetSettings()
    {
        var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())

        //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

        var config = builder.Build();
        
        return new Settings { Key = config["OPENAI_API_KEY"]!, Model = config["OPENAI_CHATCOMPLETION_DEPLOYMENT"]! };
    }
}
