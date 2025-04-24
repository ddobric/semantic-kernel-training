using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MyFunctionApp1
{
    public class Function2
    {
        private readonly ILogger<Function2> _logger;

        public Function2(ILogger<Function2> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function2))]
        [QueueOutput("output-queue", Connection = "AzureWebJobsStorage")]
        public Task<string> RunAsync([QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] string message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message}");

            return Task.FromResult<string>("hello");
        }
    }
}
