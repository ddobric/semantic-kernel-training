#pragma warning disable OPENAI001

using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Containers;
using OpenAI.Responses;
namespace AgentFramework_Samples.OpenAIAgents
{
    internal class OpenAICodeInterpreter
    {

        public static async Task RunAsync()
        {
            Helpers.GetModelAndKey(out var apiKey, out var model);

            var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey));

            // Create an agent with Code Interpreter tool enabled
            AIAgent agent = openAIClient
                .GetResponsesClient()
                .AsAIAgent(
                    model: model,
                    instructions: "You are a helpful assistant that can generate files using code.",
                    name: "CodeInterpreterAgent",
                    tools: [new HostedCodeInterpreterTool()]);

            // Ask the agent to generate a file
            //AgentResponse response = await agent.RunAsync(
            //    "Create a CSV file with the multiplication times tables from 1 to 12. Include headers.");

            AgentResponse response = await agent.RunAsync(
               "Create the simple line diagram from following values: 1,2,3,4,5,4,3,2,1,2,3,4,5,6,5,4,3,2,1,2,3,4,5,6");

            // Display the text response
            foreach (TextContent textContent in response.Messages.SelectMany(x => x.Contents).OfType<TextContent>())
            {
                Console.WriteLine(textContent.Text);
            }

            // Extract container file citations from response annotations and download
            ContainerClient containerClient = openAIClient.GetContainerClient();

            HashSet<string> downloadedFiles = [];
            bool foundContainerFiles = false;

            foreach (AIContent content in response.Messages.SelectMany(x => x.Contents))
            {
                if (content.Annotations is null)
                {
                    continue;
                }

                foreach (AIAnnotation annotation in content.Annotations)
                {
                    // Container files from Code Interpreter have ContainerFileCitationMessageAnnotation as raw representation
                    if (annotation is CitationAnnotation citation
                        && citation.RawRepresentation is ContainerFileCitationMessageAnnotation containerCitation)
                    {
                        foundContainerFiles = true;

                        // Deduplicate by container+file ID in case the same file is cited multiple times
                        string key = $"{containerCitation.ContainerId}/{containerCitation.FileId}";
                        if (!downloadedFiles.Add(key))
                        {
                            continue;
                        }

                        Console.WriteLine($"\nDownloading container file: {containerCitation.Filename}");
                        Console.WriteLine($"  Container ID: {containerCitation.ContainerId}");
                        Console.WriteLine($"  File ID:      {containerCitation.FileId}");

                        BinaryData fileData = await containerClient.DownloadContainerFileAsync(
                            containerCitation.ContainerId,
                            containerCitation.FileId);

                        // Sanitize filename to prevent path traversal
                        string safeFilename = Path.GetFileName(containerCitation.Filename);
                        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), safeFilename);
                        await File.WriteAllBytesAsync(outputPath, fileData.ToArray());
                        Console.WriteLine($"  Saved to:     {outputPath}");
                    }
                }
            }

            if (!foundContainerFiles)
            {
                Console.WriteLine("\nNo container file citations found in the response.");
                Console.WriteLine("The model may not have generated a downloadable file for this prompt.");
            }
        }
    }
}
