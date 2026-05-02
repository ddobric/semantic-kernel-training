using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Samples
{
    /// <summary>
    /// Demonstrates document classification using embedding-based similarity.
    /// This sample loads text documents from the Docs/ folder, generates embeddings
    /// for each document using both large and small embedding models, and then
    /// classifies user input by computing cosine similarity against all stored documents.
    /// The document with the highest similarity score is the best category match.
    /// </summary>
    internal class ClassificationSample
    {
        /// <summary>
        /// Runs the classification loop:
        /// 1. Builds an in-memory "knowledge base" by embedding all documents in Docs/
        /// 2. Prompts the user for text input
        /// 3. Generates embeddings for the input using both large and small models
        /// 4. Compares the input embedding against each document embedding via cosine similarity
        /// 5. Displays similarity scores so the user can see which document category best matches
        /// </summary>
        public static async Task RunAsync()
        {
            // In-memory store for document embeddings
            List<Entry> entries = new List<Entry>();

            // Create two embedding clients to compare model performance:
            // - text-embedding-3-large: higher dimensional, more accurate embeddings
            // - text-embedding-3-small: lower dimensional, faster and cheaper
            EmbeddingClient clientLarge = new("text-embedding-3-large",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            EmbeddingClient clientSmall = new("text-embedding-3-small",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Pre-compute embeddings for all documents in the Docs/ folder
            await CreateMemory(entries, clientLarge, clientSmall);

            // Interactive classification loop
            while (true)
            {
                Console.WriteLine("Enter text for classification: ");

                var inp1 = Console.ReadLine();

                List<string> inputs = [inp1];

                // Generate embeddings for the user's input text using both models
                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(inputs);
                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(inputs);

                // Compare input embedding against each stored document embedding
                var results = entries.Select(entry => new
                {
                    Document = new FileInfo(entry.DocName).Name,
                    SimilarityLarge = Program.CalculateSimilarity(eL[0].ToFloats().ToArray(), entry.EmbeddingLarge),
                    SimilaritySmall = Program.CalculateSimilarity(eS[0].ToFloats().ToArray(), entry.EmbeddingSmall)
                }).ToList();

                // Render a formatted console table
                const string headerDoc = "Document";
                const string headerLarge = "Large Model";
                const string headerSmall = "Small Model";

                int docWidth = Math.Max(headerDoc.Length, results.Max(r => r.Document.Length));
                int largeWidth = Math.Max(headerLarge.Length, 10);
                int smallWidth = Math.Max(headerSmall.Length, 10);

                string separator = $"+-{new string('-', docWidth)}-+-{new string('-', largeWidth)}-+-{new string('-', smallWidth)}-+";

                Console.WriteLine(separator);
                Console.WriteLine($"| {headerDoc.PadRight(docWidth)} | {headerLarge.PadRight(largeWidth)} | {headerSmall.PadRight(smallWidth)} |");
                Console.WriteLine(separator);

                var bestLarge = results.Max(r => r.SimilarityLarge);
                var bestSmall = results.Max(r => r.SimilaritySmall);

                foreach (var r in results)
                {
                    bool isBest = r.SimilarityLarge == bestLarge || r.SimilaritySmall == bestSmall;
                    if (isBest)
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"| {r.Document.PadRight(docWidth)} | {r.SimilarityLarge.ToString("F6").PadLeft(largeWidth)} | {r.SimilaritySmall.ToString("F6").PadLeft(smallWidth)} |");

                    if (isBest)
                        Console.ResetColor();
                }

                Console.WriteLine(separator);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Builds the in-memory knowledge base by reading each document from Docs/
        /// and generating embeddings with both the large and small embedding models.
        /// </summary>
        private static async Task CreateMemory(List<Entry> entries, EmbeddingClient clientLarge, EmbeddingClient clientSmall)
        {
            // Process each text file in the Docs directory
            foreach (var file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs")))
            {
                Entry entry = new Entry();
                var txt = File.ReadAllText(file);
                entry.DocName = file;

                // Generate embedding using the large model (higher accuracy)
                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingLarge = eL[0].ToFloats().ToArray();

                // Generate embedding using the small model (faster, cheaper)
                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingSmall = eS[0].ToFloats().ToArray();

                entries.Add(entry);
            }
        }
    }

    /// <summary>
    /// Represents a document entry with its name and precomputed embeddings
    /// from both the large and small embedding models.
    /// </summary>
    internal class Entry
    {
        public string DocName { get; set; }
        public float[] EmbeddingLarge { get; set; }
        public float[] EmbeddingSmall { get; set; }
    }
}
