using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace MonkeyMCPSSE;

[McpServerToolType]
public sealed class MonkeyTools
{
    private readonly MonkeyService monkeyService;

    public MonkeyTools(MonkeyService monkeyService)
    {
        this.monkeyService = monkeyService;
    }

    [McpServerTool, Description("Get a list of monkeys.")]
    public async Task<string> GetMonkeys()
    {
        var monkeys = await monkeyService.GetMonkeys();
        return JsonSerializer.Serialize(monkeys, MonkeyContext.Default.ListMonkey);
    }

    [McpServerTool, Description("Get a monkey by name.")]
    public async Task<string> GetMonkey([Description("The name of the monkey to get details for")] string name)
    {
        var monkey = await monkeyService.GetMonkey(name);
        return JsonSerializer.Serialize(monkey, MonkeyContext.Default.Monkey);
    }
}


[McpServerToolType]
public class ToolRight
{
    [McpServerTool, Description("Upload documents to a cluster.")]
    public Task UploadDocuments([Description("The name of the cluster")] string clusterName, 
        [Description("The folder containing the documents")] string folderWithDocument)
    {
        return Task.CompletedTask;
    }
}


[McpServerToolType]
public class ToolLeft
{
    [McpServerTool, Description("Classify a document.")]
    public Task ClassifyDocument([Description("The full path of the document")] string documentFullPathName)
    {
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Send a drone to a location.")]
    public Task SendDrone([Description("The location to send the drone to.")] string location)
    {
        return Task.CompletedTask;
    }

}