using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using OpenAI.Files;
using OpenAI.Responses;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FoundryAgentDemo
{
    /// <summary>
    /// Demonstrates running an AI agent using the Responses API without creating a persistent agent in Azure Foundry.
    /// The agent is created in-memory via <see cref="AIProjectClientExtensions.AsAIAgent"/> and executes a single prompt
    /// with a custom tool (<see cref="Tools.GetProcessInfo"/>) for listing running processes.
    /// </summary>
    internal class FoundryResponsesAgentSample
    {
        /// <summary>
        /// Creates an in-memory AI agent with a process-info tool and runs a single prompt.
        /// </summary>
        public async Task RunAsync()
        {
            Helper.GetAzureEndpointAndModelDeployment(out var projectEndpoint, out var deploymentName);

            AIAgent agent = new AIProjectClient(
                 new Uri(projectEndpoint),
                         new DefaultAzureCredential())
                         .AsAIAgent(
                           model: deploymentName,
                           name: nameof(FoundryResponsesAgentSample),
                           instructions: "You are good at creating analytics.",
                           tools: [AIFunctionFactory.Create(Tools.GetProcessInfo)]);

            Console.WriteLine(await agent.RunAsync("List running processes and create some intersting analytics."));
        }

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public async Task RunWithFileAsync()
        {
            OpenAIFile? uploadedFile = null;
            OpenAIFileClient? fileClient = null;
            bool fileWasUploaded = false; // Track if we uploaded the file

            try
            {
                Helper.GetAzureEndpointAndModelDeployment(out var endpoint, out var deploymentName);

                const string agentName = "MyAgent";

                // Load PDF from disk
                string fileName = "Daenet_Semantic_Testing_Framework.pdf";
                string instructions = "Extract from this document the Author, the year and the title.";

                string filePath = $@"Files\{fileName}";

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Error: File not found at path: {filePath}");
                    return;
                }

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                // Create AIProjectClient
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
                AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: credential);

                // 1. Get the file client
                fileClient = projectClient.ProjectOpenAIClient.GetOpenAIFileClient();

                // Check if file already exists
                Console.WriteLine("Checking if file already exists...");
                var existingFilesResult = fileClient.GetFiles();
                var existingFiles = existingFilesResult.Value;

                foreach (var file in existingFiles)
                {
                    // Check filename matches and purpose is Assistants
                    if (file.Filename == fileName && file.Purpose.ToString().ToLower() == "assistants")
                    {
                        uploadedFile = file;
                        Console.WriteLine($"File '{fileName}' already exists with ID: {uploadedFile.Id}");
                        break;
                    }
                }

                // Upload file only if it doesn't exist
                if (uploadedFile == null)
                {
                    Console.WriteLine($"File '{fileName}' not found. Uploading...");
                    uploadedFile = await fileClient.UploadFileAsync(
                        BinaryData.FromBytes(fileBytes),
                        fileName,
                        FileUploadPurpose.Assistants
                    );

                    fileWasUploaded = true; // Mark that we uploaded this file
                    Console.WriteLine($"File uploaded successfully: {uploadedFile.Id}");
                }
                else
                {
                    Console.WriteLine($"Using existing file: {uploadedFile.Id}");
                }

                // 2. Check if the agent already exists in Foundry
                ProjectsAgentVersion? agentVersion = null;
                var agentAdminClient = projectClient.AgentAdministrationClient;

                try
                {
                    // Try to get existing agent versions
                    var existingVersions = agentAdminClient.GetAgentVersions(agentName);
                    agentVersion = existingVersions.FirstOrDefault();

                    if (agentVersion != null)
                    {
                        Console.WriteLine($"Agent '{agentName}' already exists in Foundry (Version: {agentVersion.Version})");
                    }
                }
                catch (Azure.RequestFailedException ex) when (ex.Status == 404)
                {
                    // Agent doesn't exist, which is fine - we'll create it below
                    Console.WriteLine($"Agent '{agentName}' not found in Foundry. Creating new agent...");
                }

                // 3. Create agent if it doesn't exist
                if (agentVersion == null)
                {
                    agentVersion = await agentAdminClient.CreateAgentVersionAsync(
                        agentName,
                        new ProjectsAgentVersionCreationOptions(
                            new DeclarativeAgentDefinition(model: deploymentName)
                            {
                                Instructions = "You are an expert document analyzer. Carefully read and analyze the provided documents to extract requested information."
                            }));

                    Console.WriteLine($"Agent '{agentName}' created successfully (Version: {agentVersion.Version})");
                }

                // 4. Use the Responses API to send a message with the file attachment
                AgentReference agentReference = new(name: agentName, version: agentVersion.Version);
                ProjectResponsesClient responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agentReference);

                // 5. Create a user message with both text and file attachment
                ResponseItem userMessage = ResponseItem.CreateUserMessageItem(
                [
                    ResponseContentPart.CreateInputTextPart(instructions),
                    ResponseContentPart.CreateInputFilePart(uploadedFile.Id)
                ]);

                Console.WriteLine("\n=== Sending request to agent ===");

                // 6. Create the response
                ResponseResult response = responseClient.CreateResponse([userMessage]);

                Console.WriteLine("\n=== Agent Response ===");
                Console.WriteLine(response.GetOutputText());

                // 7. Clean up the response
                responseClient.DeleteResponse(response.Id);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied: {ex.Message}");
            }
            catch (Azure.Identity.AuthenticationFailedException ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
            }
            catch (Azure.RequestFailedException ex)
            {
                Console.WriteLine($"Azure API request failed: {ex.Message} (Status: {ex.Status})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // 8. Clean up - delete the uploaded file only if we uploaded it in this session
                if (uploadedFile != null && fileClient != null && fileWasUploaded)
                {
                    try
                    {
                        await fileClient.DeleteFileAsync(uploadedFile.Id);
                        Console.WriteLine("\nFile cleanup completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nWarning: Failed to delete uploaded file: {ex.Message}");
                    }
                }
                else if (uploadedFile != null && !fileWasUploaded)
                {
                    Console.WriteLine("\nSkipped file cleanup (using pre-existing file).");
                }
            }
        }
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


    }
}
