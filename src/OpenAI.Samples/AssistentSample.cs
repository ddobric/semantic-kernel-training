using OpenAI.Assistants;
using OpenAI.Files;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Samples
{
    /// <summary>
    /// Demonstrates Retrieval-Augmented Generation (RAG) using the OpenAI Assistants API.
    /// This sample uploads a JSON sales data file, creates an assistant with file search
    /// and code interpreter tools, runs a user query about the data, and displays
    /// the assistant's augmented response including any generated visualizations.
    /// </summary>
    internal class AssistentSample
    {
        /// <summary>
        /// Runs the full RAG workflow:
        /// 1. Uploads a sales data document to OpenAI file storage
        /// 2. Creates an assistant with file search and code interpreter tools
        /// 3. Creates a thread with an initial user query about sales data
        /// 4. Polls the thread run until completion
        /// 5. Retrieves and displays all messages (including generated images/charts)
        /// 6. Cleans up all created resources (thread, assistant, file)
        /// </summary>
        public static async Task RunRetrievalAugmentedGenerationAsync()
        {
            // Assistants is a beta API and subject to change; acknowledge its experimental status by suppressing the matching warning.
#pragma warning disable OPENAI001

            // Create the top-level OpenAI client and obtain specialized sub-clients
            OpenAIClient openAIClient = new(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            AssistantClient assistantClient = openAIClient.GetAssistantClient();

            // Prepare a sample JSON document containing Contoso monthly sales data.
            // In a real scenario, this would be an actual file from your data store.
            using Stream document = BinaryData.FromBytes("""
            {
                "description": "This document contains the sale history data for Contoso products.",
                "sales": [
                    {
                        "month": "January",
                        "by_product": {
                            "113043": 15,
                            "113045": 12,
                            "113049": 2
                        }
                    },
                    {
                        "month": "February",
                        "by_product": {
                            "113045": 22
                        }
                    },
                    {
                        "month": "March",
                        "by_product": {
                            "113045": 16,
                            "113055": 5
                        }
                    }
                ]
            }
            """u8.ToArray()).ToStream();

            // Upload the document to OpenAI's file storage for use by the assistant
            OpenAIFile salesFile = await fileClient.UploadFileAsync(document, "monthly_sales.json", FileUploadPurpose.Assistants);

            // Configure the assistant with:
            // - FileSearchToolDefinition: enables the assistant to search through uploaded files
            // - CodeInterpreterToolDefinition: enables the assistant to write and execute Python code
            //   (e.g., to generate charts and visualizations)
            // - A vector store is created from the uploaded file for semantic search
            AssistantCreationOptions assistantOptions = new()
            {
                Name = "Example: Contoso sales RAG",
                Instructions =
                    "You are an assistant that looks up sales data and helps visualize the information based"
                    + " on user queries. When asked to generate a graph, chart, or other visualization, use"
                    + " the code interpreter tool to do so.",
                Tools =
            {
                new FileSearchToolDefinition(),
                new CodeInterpreterToolDefinition(),
            },
                ToolResources = new()
                {
                    FileSearch = new()
                    {
                        NewVectorStores =
                    {
                        new VectorStoreCreationHelper([salesFile.Id]),
                    }
                    }
                },
            };

            // Create the assistant using GPT-4o as the underlying model
            Assistant assistant = await assistantClient.CreateAssistantAsync("gpt-4o", assistantOptions);

            // Create a new thread with an initial user message asking about sales data and a trend graph
            ThreadCreationOptions threadOptions = new()
            {
                InitialMessages = { "How well did product 113045 sell in February? Graph its trend over time." }
            };

            // Start the thread and run the assistant against the initial message
            ThreadRun threadRun = await assistantClient.CreateThreadAndRunAsync(assistant.Id, threadOptions);

            // Poll until the run reaches a terminal state (completed, failed, etc.)
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                threadRun = assistantClient.GetRun(threadRun.ThreadId, threadRun.Id);
            } while (!threadRun.Status.IsTerminal);

            // Retrieve the full conversation history in ascending order (oldest first)
            AsyncCollectionResult<ThreadMessage> messages
                = assistantClient.GetMessagesAsync(threadRun.ThreadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Ascending });

            // Display each message from the thread
            await foreach (ThreadMessage message in messages)
            {
                Console.Write($"[{message.Role.ToString().ToUpper()}]: ");
                foreach (MessageContent contentItem in message.Content)
                {
                    // Print text content
                    if (!string.IsNullOrEmpty(contentItem.Text))
                    {
                        Console.WriteLine($"{contentItem.Text}");

                        if (contentItem.TextAnnotations.Count > 0)
                        {
                            Console.WriteLine();
                        }

                        // Display any file citations or file outputs referenced in the text
                        foreach (TextAnnotation annotation in contentItem.TextAnnotations)
                        {
                            if (!string.IsNullOrEmpty(annotation.InputFileId))
                            {
                                Console.WriteLine($"* File citation, file ID: {annotation.InputFileId}");
                            }
                            if (!string.IsNullOrEmpty(annotation.OutputFileId))
                            {
                                Console.WriteLine($"* File output, new file ID: {annotation.OutputFileId}");
                            }
                        }
                    }

                    // Download and save any generated images (e.g., charts from code interpreter)
                    if (!string.IsNullOrEmpty(contentItem.ImageFileId))
                    {
                        OpenAIFile imageInfo = await fileClient.GetFileAsync(contentItem.ImageFileId);
                        BinaryData imageBytes = await fileClient.DownloadFileAsync(contentItem.ImageFileId);
                        using FileStream stream = File.OpenWrite($"{imageInfo.Filename}.png");
                        imageBytes.ToStream().CopyTo(stream);

                        Console.WriteLine($"<image: {imageInfo.Filename}.png>");
                    }
                }
                Console.WriteLine();
            }

            // Clean up: delete the thread, assistant, and uploaded file to avoid lingering resources
            await assistantClient.DeleteThreadAsync(threadRun.ThreadId);
            await assistantClient.DeleteAssistantAsync(assistant.Id);
            await fileClient.DeleteFileAsync(salesFile.Id);
        }
    }
}
