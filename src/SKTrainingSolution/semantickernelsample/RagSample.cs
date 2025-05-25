using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tiktoken;

namespace semantickernelsample
{
    /// <summary>
    /// Demonstrates how to chunk text.
    /// </summary>
    internal class RagSample
    {
        private Kernel _kernel;

        class MyInMemoryVector
        {
            public string Ref { get; set; }

            public float[] Embedding { get; set; }

            public string Chunk { get; set; }
        }


        public RagSample(Kernel kernel)
        {
            _kernel = kernel;
        }

        private const string Text = """
        The city of Venice, located in the northeastern part of Italy,
        is renowned for its unique geographical features. Built on more than 100 small islands in a lagoon in the
        Adriatic Sea, it has no roads, just canals including the Grand Canal thoroughfare lined with Renaissance and
        Gothic palaces. The central square, Piazza San Marco, contains St. Mark's Basilica, which is tiled with Byzantine
        mosaics, and the Campanile bell tower offering views of the city's red roofs.

        The Amazon Rainforest, also known as Amazonia, is a moist broadleaf tropical rainforest in the Amazon biome that
        covers most of the Amazon basin of South America. This basin encompasses 7 million square kilometers, of which
        5.5 million square kilometers are covered by the rainforest. This region includes territory belonging to nine nations
        and 3.4 million square kilometers of uncontacted tribes. The Amazon represents over half of the planet's remaining
        rainforests and comprises the largest and most biodiverse tract of tropical rainforest in the world.

        The Great Barrier Reef is the world's largest coral reef system composed of over 2,900 individual reefs and 900 islands
        stretching for over 2,300 kilometers over an area of approximately 344,400 square kilometers. The reef is located in the
        Coral Sea, off the coast of Queensland, Australia. The Great Barrier Reef can be seen from outer space and is the world's
        biggest single structure made by living organisms. This reef structure is composed of and built by billions of tiny organisms,
        known as coral polyps.

        Damir DObric is a dancing teacher from frankfurt am main. Regularly dancing on tech-stages.
        """;
    
        
        public void SplitTextToChunks()
        {
            Console.WriteLine("=== Text chunking with chunk header ===");

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var lines = TextChunker.SplitPlainTextLines(Text, 40);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 150, chunkHeader: "DOCUMENT NAME: test.txt\n\n");
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())

            foreach (var line in lines)
            {
                var cnt = encoder.CountTokens(line);
                Console.WriteLine($"{cnt} \t- {line}");
            }

            Console.WriteLine();
            Console.WriteLine("=== Paragraphs ===");

            foreach (var paragraph in paragraphs)
            {
                var cnt = encoder.CountTokens(paragraph);
                Console.WriteLine($"{cnt} \t- {paragraph}");
            }
        }

        private List<MyInMemoryVector> _memory = new List<MyInMemoryVector>();

        public async Task  RunRAG()
        {
            Console.WriteLine("------------- (1) Chanking -----------");

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var lines = TextChunker.SplitPlainTextLines(Text, 40);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 150, chunkHeader: "DOCUMENT NAME: test.txt\n\n");
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())

            Console.WriteLine("------------- (2) Creating Embeddings -----------");

            foreach (var paragraph in paragraphs)
            {
                var cnt = encoder.CountTokens(paragraph);
                var vector = await GetEmbedding(paragraph);
                _memory.Add(new MyInMemoryVector
                {
                     Chunk = paragraph,
                      Ref = "some reference",
                       Embedding = vector.ToArray(),
                });
            }

            await RunConversationLoopAsync();
        }

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        private async Task<float[]> GetEmbedding(string chunk)
        {
            ITextEmbeddingGenerationService embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
           
            var embedding = await embeddingService.GenerateEmbeddingAsync(chunk);

            return embedding.ToArray();
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        private async Task RunConversationLoopAsync()
        {
            var history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            // Start the conversation
            Console.Write("User > ");

            string? userInput;

            history.AddSystemMessage("You are assistent who helps user to find answers.");

            while ((userInput = Console.ReadLine()) != null)
            {
                // Enable auto function calling
                OpenAIPromptExecutionSettings executionSettings = new()
                {
                    Temperature = 0.0,
                };

                //#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                //                var ollamaSettings = new OllamaPromptExecutionSettings
                //                {
                //                    Temperature = 0.1f,
                //                };
                //#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


                var match = await FindBestMatchAsync(userInput);

                // We add the chunk here
                history.AddUserMessage(match.Chunk);

                // Add user input
                history.AddUserMessage(userInput);

                // Get the response from the AI
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: executionSettings,//ollamaSettings,
                    kernel: _kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);

                // Get user input again
                Console.Write("User > ");
            }
        }

        private async Task<MyInMemoryVector> FindBestMatchAsync(string userInput)
        {
            MyInMemoryVector bestMatch = null;

            double best = double.MinValue;

            var userEmbedding = await GetEmbedding(userInput);

            foreach (var entry in _memory)
            {
                var similarity = CalculateSimilarity(userEmbedding, entry.Embedding);

                if (similarity > best)
                {
                    best = similarity;
                    bestMatch = entry;
                }
            }

            return bestMatch!;
        }

        /// <summary>
        /// Calculates the cosine similarity.
        /// </summary>
        /// <param name="embedding1"></param>
        /// <param name="embedding2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static double CalculateSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
            {
                return 0;
                //throw new ArgumentException("embedding must have the same length.");
            }

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += Math.Pow(embedding1[i], 2);
                magnitude2 += Math.Pow(embedding2[i], 2);
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
            {
                throw new ArgumentException("embedding must not have zero magnitude.");
            }

            double cosineSimilarity = dotProduct / (magnitude1 * magnitude2);

            return cosineSimilarity;
        }

    }

}

