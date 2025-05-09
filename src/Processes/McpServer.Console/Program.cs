using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace McpServer.Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Create a kernel builder and add plugins
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Plugins.AddFromType<Tools>();
     
            // Build the kernel
            Kernel kernel = kernelBuilder.Build();

            var builder = Host.CreateEmptyApplicationBuilder(settings: null);
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                // Add all functions from the kernel plugins to the MCP server as tools
                .AddTools(kernel.Plugins);
            await builder.Build().RunAsync();
        }

    }
}
