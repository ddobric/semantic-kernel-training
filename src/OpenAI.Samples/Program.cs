using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using System.ClientModel;
using System.Text;

namespace OpenAI.Samples
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, OpenAI Samples!");

           await CreateEmbeddingsAsync();

            //await ClassifyAsync();
           
            await ChatChatCompletionsAsync();

            //await ChatStreamingAsync();

            //await TextToSpeechAsync();

            await VisionAsync();

            await ImageGenerationAsync();

            await SimpleImageEditAsync();

            await AssistentSample.RunRetrievalAugmentedGenerationAsync();

            Console.ReadLine();
        }

        public static async Task ChatChatCompletionsAsync()
        {
            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 60,
                IncludeLogProbabilities = true,
                TopLogProbabilityCount = 5,
            };

            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
               apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            List<ChatMessage> history = new List<ChatMessage>();

            history.Add(ChatMessage.CreateSystemMessage("Your only task is to complete the given prompt."));

            while (true)
            {
                var sb = new StringBuilder();

                Console.ForegroundColor = ConsoleColor.White;

                Console.Write($"[User]: ");

                string? userIntent = Console.ReadLine();

                history.Add(ChatMessage.CreateUserMessage(userIntent));

                var result = await client.CompleteChatAsync(history, options);

                Console.ForegroundColor = ConsoleColor.Cyan;

                foreach (var item in result.Value.Content)
                {
                    Console.WriteLine(item.Text);
                    sb.AppendLine(item.Text);
                }

                Console.WriteLine();

                foreach (var item in result.Value.ContentTokenLogProbabilities)
                {
                    bool isFirst = true;

                    foreach (var logProb in item.TopLogProbabilities)
                    {
                        if (isFirst)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            isFirst = false;
                        }
                        else
                            Console.ForegroundColor = ConsoleColor.Cyan;

                        Console.WriteLine($"{logProb.Token} \t\t {logProb.LogProbability}");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();

                history.Add(ChatMessage.CreateAssistantMessage(sb.ToString()));

                Console.WriteLine();
            }
        }

        public static async Task ChatStreamingAsync()
        {
            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Temperature = 0,
                MaxOutputTokenCount = 60,
            };

            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
               apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            List<ChatMessage> history = new List<ChatMessage>();

            history.Add(ChatMessage.CreateSystemMessage("You are a helpful funny assistant."));

            while (true)
            {
                Console.Write($"[User]: ");

                string? userIntent = Console.ReadLine();

                history.Add(ChatMessage.CreateUserMessage(userIntent));

                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = client.CompleteChatStreamingAsync(history, options);

                Console.Write($"[Agent]: ");

                StringBuilder sb = new StringBuilder();

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        sb.Append(ChatMessage.CreateAssistantMessage(completionUpdate.ContentUpdate[0].Text));
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                history.Add(ChatMessage.CreateAssistantMessage(sb.ToString()));

                Console.WriteLine();
            }
        }

        public static async Task VisionAsync()
        {
            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
             apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            string imageFilePath = Path.Combine("Images", "testimage.png");

            using (Stream imageStream = File.OpenRead(imageFilePath))
            {
                BinaryData imageBytes = BinaryData.FromStream(imageStream);

                List<ChatMessage> messages =
             [
              new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Please describe the following image."),
                ChatMessageContentPart.CreateImagePart(imageBytes, "image/png")),
             ];

                ChatCompletion completion = await client.CompleteChatAsync(messages);

                Console.WriteLine($"[Agent]: {completion.Content[0].Text}");
            }
        }

        public static async Task CreateEmbeddingsAsync()
        {
            EmbeddingClient client = new("text-embedding-3-small",//*text-embedding-3-large"
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            while (true)
            {
                Console.WriteLine("Enter text 1: ");
                var inp1 = Console.ReadLine();

                Console.WriteLine("Enter text 2: ");
                var inp2 = Console.ReadLine();

                List<string> inputs = [inp1, inp2];

                OpenAIEmbeddingCollection collection = await client.GenerateEmbeddingsAsync(inputs);

                //foreach (OpenAIEmbedding embedding in collection)
                //{
                //    ReadOnlyMemory<float> vector = embedding.ToFloats();

                //    //Console.WriteLine($"Dimension: {vector.Length}");
                //    //Console.WriteLine($"Floats: ");
                //    //for (int i = 0; i < vector.Length; i++)
                //    //{
                //    //    Console.WriteLine($"  [{i,4}] = {vector.Span[i]}");
                //    //}                 
                //}

                var similarity = CalculateSimilarity(collection[0].ToFloats().ToArray(), collection[1].ToFloats().ToArray());

                Console.WriteLine($"Similarity: {similarity}");

                Console.WriteLine();
                Console.WriteLine();

            }
        }

        class Entry
        {
            public string DocName { get; set; }
            public float[] EmbeddingLarge { get; set; }
            public float[] EmbeddingSmall { get; set; }
        }

        public static async Task ClassifyAsync()
        {
            List<Entry> entries = new List<Entry>();

            EmbeddingClient clientLarge = new("text-embedding-3-large"/*"text -embedding-3-small"*/,
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            EmbeddingClient clientSmall = new("text-embedding-3-small",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            foreach (var file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs")))
            {
                Entry entry = new Entry();
                var txt = File.ReadAllText(file);
                entry.DocName = file;

                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingLarge = eL[0].ToFloats().ToArray();

                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(new List<string> { txt });
                entry.EmbeddingSmall = eS[0].ToFloats().ToArray();

                entries.Add(entry);
            }

            while (true)
            {
                Console.WriteLine("Enter text for classification: ");

                var inp1 = Console.ReadLine();

                List<string> inputs = [inp1];

                OpenAIEmbeddingCollection eL = await clientLarge.GenerateEmbeddingsAsync(inputs);
                OpenAIEmbeddingCollection eS = await clientSmall.GenerateEmbeddingsAsync(inputs);

                foreach (var entry in entries)
                {
                    var similarityL = CalculateSimilarity(eL[0].ToFloats().ToArray(), entry.EmbeddingLarge);
                    var similarityS = CalculateSimilarity(eS[0].ToFloats().ToArray(), entry.EmbeddingSmall);

                    Console.WriteLine($"Document: {new FileInfo(entry.DocName).Name}\t SimilarityL: {similarityL}, SimilarityS: {similarityS}");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        public static async Task ImageGenerationAsync()
        {
            ImageClient client = new("dall-e-3", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            string prompt = "The concept for a living room that blends mediteran simplicity with Japanese minimalism for"
                + " a serene and cozy atmosphere. It's a space that invites relaxation and mindfulness, with natural light"
                + " and fresh air. Using neutral tones, including colors like white, beige, gray, and black, that create a"
                + " sense of harmony. Featuring sleek wood furniture with clean lines and subtle curves to add warmth and"
                + " elegance. Plants and flowers in ceramic pots adding color and life to a space. They can serve as focal"
                + " points, creating a connection with nature. Soft textiles and cushions in organic fabrics adding comfort"
                + " and softness to a space. They can serve as accents, adding contrast and texture. Dog sitting at the table. Tiger is laying under the table. One women is wrking on laptop and eating the chocolate.";

            prompt = "Julia Roberts is dancing in Tanzania with monkeys, elephants and Cristiano Ronaldo is working on laptop and eating the chocolate. Show the clock with the time 16:22";
            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Natural,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage image = await client.GenerateImageAsync(prompt, options);
            BinaryData bytes = image.ImageBytes;

            using FileStream stream = File.OpenWrite($"{Guid.NewGuid()}.png");
            bytes.ToStream().CopyTo(stream);
        }

        /// <summary>
        /// DOES NOT WORK. MASK IMAGE NOT SET
        /// </summary>
        /// <returns></returns>
        public static async Task SimpleImageEditAsync()
        {
            ImageClient client = new("dall-e-2", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            string imageFilePath = Path.Combine("Images", "daenet Schuerze.png");
            string prompt = "A graph in aquarium full of wather.";
            string maskFilePath = Path.Combine("Images", "SQL VectorSearch Performance Graph.png");

            ImageEditOptions options = new()
            {
                Size = GeneratedImageSize.W512xH512,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            GeneratedImage edit = await client.GenerateImageEditAsync(imageFilePath, prompt, maskFilePath, options);
            BinaryData bytes = edit.ImageBytes;

            using FileStream stream = File.OpenWrite($"{Guid.NewGuid()}.png");
            await bytes.ToStream().CopyToAsync(stream);
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

        public static async Task TextToSpeechAsync(string text = null)
        {
            AudioClient client = new("tts-1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            string input = text != null ? text : "Alright everyone, please keep your hands inside the code at all times. If you see any unexpected errors, don't worry—those are just features in disguise. And remember, if the demo works perfectly on the first try, it's probably witchcraft. Sit back, relax, and let’s enjoy the magic (and maybe some debugging) together!";

            BinaryData speech = await client.GenerateSpeechAsync(input, GeneratedSpeechVoice.Nova);

            using FileStream stream = File.OpenWrite($"{Guid.NewGuid()}.mp3");
            speech.ToStream().CopyTo(stream);
        }
    }
}
