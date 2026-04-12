using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI.Samples
{
    internal class ClassificationSample
    {
        public static async Task RunAsync()
        {
            // List to store document entries with embeddings
            List<Entry> entries = new List<Entry>();

            // Create embedding clients for large and small models
            EmbeddingClient clientLarge = new("text-embedding-3-large",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            EmbeddingClient clientSmall = new("text-embedding-3-small",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            await CreateVectorMemory(entries, clientLarge, clientSmall);

            // Infinite loop for user input and classification
            while (true)
            {
                Console.WriteLine("Enter text for classification: ");

                var inp1 = Console.ReadLine(); // Get user input

                List<string> inputs = [inp1];

                // Generate embeddings for user input
                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(inputs);
                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(inputs);

                // Compare input embedding with each document embedding
                foreach (var entry in entries)
                {
                    var similarityL = Program.CalculateSimilarity(eL[0].ToFloats().ToArray(), entry.EmbeddingLarge);
                    var similarityS = Program.CalculateSimilarity(eS[0].ToFloats().ToArray(), entry.EmbeddingSmall);

                    // Output similarity scores for each document
                    Console.WriteLine($"Document: {new FileInfo(entry.DocName).Name}\t SimilarityL: {similarityL}, SimilarityS: {similarityS}");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private static async Task CreateVectorMemory(List<Entry> entries, EmbeddingClient clientLarge, EmbeddingClient clientSmall)
        {
            // Iterate over all files in the Docs directory
            foreach (var file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs")))
            {
                Entry entry = new Entry();
                var txt = File.ReadAllText(file); // Read document text
                entry.DocName = file;

                // Generate large model embedding for the document
                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingLarge = eL[0].ToFloats().ToArray();

                // Generate small model embedding for the document
                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingSmall = eS[0].ToFloats().ToArray();

                entries.Add(entry); // Add entry to the list
            }
        }
    }

    internal class Entry
    {
        public string DocName { get; set; }
        public float[] EmbeddingLarge { get; set; }
        public float[] EmbeddingSmall { get; set; }
    }
}
