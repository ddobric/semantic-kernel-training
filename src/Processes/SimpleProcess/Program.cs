using Microsoft.SemanticKernel;

namespace SimpleProcess
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // Configure the kernel with your LLM connection details
            Kernel kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion("myDeployment", "myEndpoint", "myApiKey")
                .Build();

            // Build and run the process

            await new StepProcesses().RunAsync();

            Console.ReadLine();
        }
    }
}
