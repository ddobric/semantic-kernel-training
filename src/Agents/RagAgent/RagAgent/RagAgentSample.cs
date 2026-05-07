using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Text;
using OpenAI.Chat;
using System.ComponentModel;
using System.Net;

namespace RagAgent
{
    /// <summary>
    /// Demonstrates a Retrieval-Augmented Generation (RAG) pattern using Azure OpenAI.
    /// Text is chunked, embedded into vectors, stored in memory, and retrieved via
    /// cosine-similarity search when the AI agent invokes the QueryInfo tool.
    /// </summary>
    internal class RagAgentSample
    {
        /// <summary>
        /// Sample knowledge-base text that will be chunked and embedded.
        /// </summary>
        private const string _text = """
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

        Damir Dobric is a dancing teacher from palma. Regularly dancing on tech-stages.

        Hans Mustermann is a dancing teacher from Stockholm. Teaching Salsa.
        
        """;

        /// <summary>
        /// Shared embedding generator used to create vector representations of text.
        /// </summary>
        private static IEmbeddingGenerator<string, Embedding<float>> _gen;

        /// <summary>
        /// Configures the embedding generator, creates the AI agent with a RAG tool,
        /// populates the in-memory knowledge base, and starts the conversation loop.
        /// </summary>
        public static async Task RunAsync()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-mini";

            _gen = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
               .GetEmbeddingClient("text-embedding-3-large")
               .AsIEmbeddingGenerator();

            // The AIFunctionFactory.Create wrapper exposes GetProcessInfo as a callable tool.
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .GetChatClient(deploymentName)
                .AsAIAgent(instructions: "You are the agent that shares exact information, which user is asking for.",
                tools: [AIFunctionFactory.Create(QueryInfo)],
                name: nameof(RagAgentSample));

            await CreateMemory(_gen);

            // Start an interactive conversation loop with streaming output.
            await RunConversationLoopAsync(agent);
        }

        /// <summary>
        /// In-memory vector store holding chunked text with their embedding vectors.
        /// </summary>
        private static List<MyInMemoryVector> _memory = new List<MyInMemoryVector>();

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        /// <summary>
        /// Chunks the source text into paragraphs, generates an embedding for each chunk,
        /// and stores them in the in-memory vector list.
        /// </summary>
        /// <param name="gen">The embedding generator to use for vectorization.</param>
        public static async Task CreateMemory(IEmbeddingGenerator<string, Embedding<float>> gen)
        {
            var lines = TextChunker.SplitPlainTextLines(_text, 40);

            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 50, chunkHeader: "DOCUMENT Ref: test.txt\n\n");

            foreach (var paragraph in paragraphs)
            {
                // Create embedding vector for the text.
                var vector = await gen.GenerateVectorAsync(paragraph);
                _memory.Add(new MyInMemoryVector
                {
                    Chunk = paragraph,
                    Ref = "some reference",
                    Embedding = vector.ToArray(),
                });
            }
        }

        #region Conversation Loop with Colored Output
        /// <summary>
        /// Runs an interactive streaming conversation loop with colored output.
        /// </summary>
        public static async Task RunConversationLoopAsync(AIAgent agent)
        {
            AgentSession session = await agent.CreateSessionAsync();

            WriteLineColored(InfoColor, "Chat session started. Type 'exit' or press Enter on an empty line to quit.");

            while (true)
            {
                Console.WriteLine();
                WriteColored(PromptColor, "You > ");

                Console.ForegroundColor = UserColor;
                string? userInput = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrEmpty(userInput) || userInput == "exit")
                {
                    WriteLineColored(InfoColor, "Session ended.");
                    break;
                }

                WriteColored(AgentColor, "Agent > ");

                try
                {
                    Console.ForegroundColor = AgentColor;

                    // Show a rotating dots animation while waiting for the first token.
                    using var spinnerCts = new CancellationTokenSource();
                    bool firstTokenReceived = false;

                    var spinnerTask = Task.Run(async () =>
                    {
                        string[] frames =
                        [
                            "/ ", "- ", "\\ ", "| "
                        ];
                        int i = 0;
                        try
                        {
                            while (!spinnerCts.Token.IsCancellationRequested)
                            {
                                string frame = frames[i % frames.Length];
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.Write(frame);
                                Console.CursorLeft -= frame.Length;
                                i++;
                                await Task.Delay(200, spinnerCts.Token);
                            }
                        }
                        catch (OperationCanceledException) { }
                    });

                    await foreach (var update in agent.RunStreamingAsync(userInput, session))
                    {
                        if (!firstTokenReceived)
                        {
                            firstTokenReceived = true;
                            spinnerCts.Cancel();
                            await spinnerTask;
                            // Clear the spinner text and restore agent color.
                            Console.Write("  ");
                            Console.CursorLeft -= 2;
                            Console.ForegroundColor = AgentColor;
                        }

                        Console.Write(update);
                    }

                    Console.ResetColor();
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ResetColor();
                    WriteLineColored(ErrorColor, $"Error: {ex.Message}");
                }
            }
        }

        private const ConsoleColor UserColor = ConsoleColor.Cyan;
        private const ConsoleColor AgentColor = ConsoleColor.Green;
        private const ConsoleColor PromptColor = ConsoleColor.Yellow;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor InfoColor = ConsoleColor.DarkGray;


        private static void WriteColored(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        private static void WriteLineColored(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        #endregion

        /// <summary>
        /// RAG lookup tool. Receives a semantic query summarizing the user intent
        /// and returns matching documents from the knowledge base.
        /// </summary>
        [Description("Searches the knowledge base using a semantic query derived from the conversation context and returns relevant documents.")]
        protected static async Task<string> QueryInfo(string semanticQuery)
        {
            string bestMatches = await FindBestMatchAsync(semanticQuery);

            return bestMatches;
        }

        /// <summary>
        /// Finds the top 3 most semantically similar chunks from the knowledge base
        /// by computing cosine similarity between the user query embedding and stored embeddings.
        /// </summary>
        /// <param name="userInput">The user's natural-language query.</param>
        /// <returns>A string containing the top 3 matching chunks separated by blank lines.</returns>
        protected static async Task<string> FindBestMatchAsync(string userInput)
        {
            var userEmbedding = await _gen.GenerateVectorAsync(userInput);

            var topMatches = _memory
                .Select(entry => new { Entry = entry, Similarity = CalculateSimilarity(userEmbedding.ToArray(), entry.Embedding) })
                .OrderByDescending(x => x.Similarity)
                .Take(3)
                .Select(x => x.Entry.Chunk);

            return string.Join("\n\n", topMatches);
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
