using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpServer.Console
{
    internal static class HostExtension
    {
        public static IMcpServerBuilder AddTools(this IMcpServerBuilder builder, KernelPluginCollection plugins)
        {
            foreach (var plugin in plugins)
            {
                foreach (var function in plugin)
                {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    builder.Services.AddSingleton(services => McpServerTool.Create(function.AsAIFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
            }

            return builder;
        }
    }
}
