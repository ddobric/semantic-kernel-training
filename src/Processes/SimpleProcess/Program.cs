using Microsoft.SemanticKernel;
using ProzessFrameworkSamples;
using ProzessFrameworkSamples.Plugins;

namespace SimpleProcess
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // Configure the kernel with your LLM connection details
            Kernel kernel = GetKernel();
            
            // DEmonstrates how model internally orchstrates tasks.
            await new ModelOrchestratorSample().RunAsync(kernel);

            //await new StepProcesses().RunAsync();

            await new TechContentProcess().RunAsync(kernel);

            Console.ReadLine();
        }

        /// <summary>
        /// Gets the kernel from environment settings.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Kernel GetKernel()
        {
            Kernel? kernel = TryGetAzureKernel();
            if (kernel == null)
                kernel = TryGetOpenAIKernel();

            if (kernel == null)
                throw new Exception("No valid kernel found.To initialize the kernel, please see documentation. Requred environment variables must be set.");

            return kernel;
        }


        private static Kernel? TryGetOpenAIKernel()
        {
            Kernel? kernel = null;

            if (Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null &&
             Environment.GetEnvironmentVariable("OPENAI_ORGID") != null)
            {
                if (Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT") != null &&
                     Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT") == null)
                {
                    kernel = Kernel.CreateBuilder()
                     .AddOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
                    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
                    Environment.GetEnvironmentVariable("OPENAI_ORGID")!)
                .Build();
                }
                else if (Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT") != null &&
                     Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT") != null)
                {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernel = Kernel.CreateBuilder()
                     .AddOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
                    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
                    Environment.GetEnvironmentVariable("OPENAI_ORGID")!)
                     .AddOpenAITextEmbeddingGeneration(
                     Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT")!,
                     Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
                     Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!
                )
                .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
                else
                {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernel = Kernel.CreateBuilder()
                     .AddOpenAITextEmbeddingGeneration(
                    Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
                    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
                    Environment.GetEnvironmentVariable("OPENAI_ORGID")!)
                .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
            }
            return kernel;
        }

        private static Kernel? TryGetAzureKernel()
        {
            Kernel? kernel = null;

            if (Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") != null &&
         Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") != null)
            {
                if (Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT") != null &&
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") == null)
                {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
                )
                .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
                else if (Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT") != null &&
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") != null)
                {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
                )
                .AddAzureOpenAITextEmbeddingGeneration(
                     Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")!,
                     Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                     Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
                )
                .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
                else
                {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernel = Kernel.CreateBuilder()
                                     .AddAzureOpenAITextEmbeddingGeneration(
                        Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")!,
                        Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                        Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
                   )
                   .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }
            }

            return kernel;
        }
    }
}
