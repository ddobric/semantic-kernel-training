using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using System.ClientModel;
using System.Text;

namespace OpenAI.Samples
{
    /// <summary>
    /// Entry point for the OpenAI Samples application.
    /// This program demonstrates various OpenAI API capabilities including embeddings,
    /// chat completions, vision, image generation, image editing, text-to-speech,
    /// and retrieval-augmented generation (RAG) with the Assistants API.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, OpenAI Samples!");

            // Sample 1: Embedding Generation & Cosine Similarity
            // Prompts the user for two texts, generates embeddings using text-embedding-3-large,
            // and calculates the cosine similarity between them.
            await CreateEmbeddingsAsync();

            // Sample 2: Document Classification via Embeddings
            // Loads documents from the Docs/ folder, embeds them with both large and small models,
            // and classifies user input by comparing embedding similarity against each document.
            //await ClassificationSample.RunAsync();

            // Sample 3: Streaming Chat Completion
            // Interactive chat loop that streams the model's response token by token in real time.
            //await ChatStreamingAsync();

            // Sample 4: Chat Completions with Log Probabilities
            // Interactive chat loop that returns completions along with the top-5 token
            // log probabilities for each generated token, useful for understanding model confidence.
            await ChatChatCompletionsAsync();

            // Sample 5: Text-to-Speech
            // Converts a text string to spoken audio using the "tts-1" model and saves it as an MP3 file.
            //await TextToSpeechAsync();

            // Sample 6: Vision (Image Understanding)
            // Sends a local image to the chat model and asks it to describe the image content.
            await VisionAsync();

            // Sample 7: Image Generation (DALL-E 3)
            // Generates a high-quality image from a text prompt using DALL-E 3 and saves it to disk.
            await ImageGenerationAsync();

            // Sample 8: Image Editing (DALL-E 2)
            // Demonstrates the image edit API using DALL-E 2 with a source image and mask.
            // Note: This sample is experimental and may not work without a valid mask image.
            await SimpleImageEditAsync();

            // Sample 9: Retrieval-Augmented Generation (RAG) with Assistants API
            // Uploads a sales data file, creates an assistant with file search and code interpreter tools,
            // runs a query about the data, and displays the assistant's augmented response.
            await AssistentSample.RunRetrievalAugmentedGenerationAsync();

            Console.ReadLine();
        }

        /// <summary>
        /// Demonstrates chat completions with log probabilities enabled.
        /// For each generated token, the model returns the top-5 most likely tokens
        /// along with their log probability scores. This is useful for analyzing
        /// model confidence, debugging prompt behavior, and understanding alternatives
        /// the model considered at each position.
        /// </summary>
        public static async Task ChatChatCompletionsAsync()
        {
            // Configure completion options:
            // - Low temperature (0.1) for more deterministic output
            // - Max 60 tokens per response
            // - Log probabilities enabled with top 5 alternatives per token
            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 60,
                IncludeLogProbabilities = true,
                TopLogProbabilityCount = 5,
            };

            // Initialize the chat client using environment variables for model and API key
            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
               apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Maintain conversation history across turns for multi-turn context
            List<ChatMessage> history = new List<ChatMessage>();

            // Set system message to instruct the model to complete prompts
            history.Add(ChatMessage.CreateSystemMessage("Your only task is to complete the given prompt."));

            while (true)
            {
                var sb = new StringBuilder();

                Console.ForegroundColor = ConsoleColor.White;

                Console.Write($"[User]: ");

                string? userIntent = Console.ReadLine();

                // Append user message to conversation history
                history.Add(ChatMessage.CreateUserMessage(userIntent));

                // Send the full conversation history to get a contextual completion
                var result = await client.CompleteChatAsync(history, options);

                // Display the model's response text
                Console.ForegroundColor = ConsoleColor.Cyan;

                foreach (var item in result.Value.Content)
                {
                    Console.WriteLine(item.Text);
                    sb.AppendLine(item.Text);
                }

                Console.WriteLine();

                // Display log probabilities for each generated token.
                // The first (chosen) token is shown in green; alternatives in cyan.
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

                // Add assistant response to history for multi-turn conversation
                history.Add(ChatMessage.CreateAssistantMessage(sb.ToString()));

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demonstrates streaming chat completions.
        /// Tokens are printed to the console as they arrive from the model,
        /// providing a real-time "typewriter" effect. Maintains conversation history
        /// for multi-turn dialogue.
        /// </summary>
        public static async Task ChatStreamingAsync()
        {
            // Configure with temperature 0 for fully deterministic output and max 60 tokens
            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Temperature = 0,
                MaxOutputTokenCount = 60,
            };

            // Initialize the chat client
            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
               apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Maintain conversation history for multi-turn context
            List<ChatMessage> history = new List<ChatMessage>();

            history.Add(ChatMessage.CreateSystemMessage("You are a helpful funny assistant."));

            while (true)
            {
                Console.Write($"[User]: ");

                string? userIntent = Console.ReadLine();

                history.Add(ChatMessage.CreateUserMessage(userIntent));

                // Use streaming API to receive tokens incrementally as they are generated
                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = client.CompleteChatStreamingAsync(history, options);

                Console.Write($"[Agent]: ");

                StringBuilder sb = new StringBuilder();

                // Process each streaming update and print tokens as they arrive
                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        sb.Append(ChatMessage.CreateAssistantMessage(completionUpdate.ContentUpdate[0].Text));
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                // Add the full assistant response to history for context in future turns
                history.Add(ChatMessage.CreateAssistantMessage(sb.ToString()));

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demonstrates vision capabilities by sending a local image to the chat model.
        /// The model analyzes the image content and returns a textual description.
        /// Uses multimodal input (text + image) in a single user message.
        /// </summary>
        public static async Task VisionAsync()
        {
            ChatClient client = new(model: Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT"),
             apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Load a local image file as binary data
            string imageFilePath = Path.Combine("Images", "testimage.png");

            using (Stream imageStream = File.OpenRead(imageFilePath))
            {
                BinaryData imageBytes = BinaryData.FromStream(imageStream);

                // Construct a multimodal message with both a text prompt and an image
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

        /// <summary>
        /// Demonstrates embedding generation and cosine similarity calculation.
        /// Prompts the user to enter two text strings, generates vector embeddings
        /// for each using the text-embedding-3-large model, and computes the cosine
        /// similarity score between them (1.0 = identical meaning, 0.0 = unrelated).
        /// </summary>
        public static async Task CreateEmbeddingsAsync()
        {
            // Initialize the embedding client with the large embedding model
            EmbeddingClient client = new("text-embedding-3-large",//*text-embedding-3-large"
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
          
            while (true)
            {
                // Prompt user for two texts to compare semantically
                Console.WriteLine("Enter text 1: ");
                var inp1 = Console.ReadLine();

                Console.WriteLine("Enter text 2: ");
                var inp2 = Console.ReadLine();

                List<string> inputs = [inp1, inp2];

                // Generate embeddings for both texts in a single API call
                OpenAIEmbeddingCollection collection = await client.GenerateEmbeddingsAsync(inputs);

                // Calculate cosine similarity between the two embedding vectors
                var similarity = CalculateSimilarity(collection[0].ToFloats().ToArray(), collection[1].ToFloats().ToArray());

                Console.WriteLine($"Similarity: {similarity}");

                Console.WriteLine();
                Console.WriteLine();

            }
        }

      

      

        /// <summary>
        /// Demonstrates image generation using DALL-E 3.
        /// Creates a high-quality image from a detailed text prompt and saves
        /// the generated image as a PNG file with a unique GUID filename.
        /// </summary>
        public static async Task ImageGenerationAsync()
        {
            // Initialize the image client with DALL-E 3
            ImageClient client = new("dall-e-3", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // A detailed descriptive prompt for the image to generate
            string prompt = "The concept for a living room that blends mediteran simplicity with Japanese minimalism for"
                + " a serene and cozy atmosphere. It's a space that invites relaxation and mindfulness, with natural light"
                + " and fresh air. Using neutral tones, including colors like white, beige, gray, and black, that create a"
                + " sense of harmony. Featuring sleek wood furniture with clean lines and subtle curves to add warmth and"
                + " elegance. Plants and flowers in ceramic pots adding color and life to a space. They can serve as focal"
                + " points, creating a connection with nature. Soft textiles and cushions in organic fabrics adding comfort"
                + " and softness to a space. They can serve as accents, adding contrast and texture. Dog sitting at the table. Tiger is laying under the table. One women is wrking on laptop and eating the chocolate.";

            prompt = "Julia Roberts is dancing in Tanzania with monkeys, elephants and Cristiano Ronaldo is working on laptop and eating the chocolate. Show the clock with the time 16:22";

            // Configure generation options: high quality, wide aspect ratio, natural style
            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                Style = GeneratedImageStyle.Natural,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            // Generate the image and save the raw bytes to a file
            GeneratedImage image = await client.GenerateImageAsync(prompt, options);
            BinaryData bytes = image.ImageBytes;

            using FileStream stream = File.OpenWrite($"{Guid.NewGuid()}.png");
            bytes.ToStream().CopyTo(stream);
        }

        /// <summary>
        /// Demonstrates image editing using DALL-E 2.
        /// Takes an existing image and a mask image, then generates an edited version
        /// based on a text prompt. The mask defines the region to be edited.
        /// NOTE: This sample is experimental and may not work without a properly formatted mask image.
        /// </summary>
        public static async Task SimpleImageEditAsync()
        {
            // DALL-E 2 supports image editing (inpainting); DALL-E 3 does not
            ImageClient client = new("dall-e-2", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Source image and mask paths
            string imageFilePath = Path.Combine("Images", "daenet Schuerze.png");
            string prompt = "A graph in aquarium full of wather.";
            string maskFilePath = Path.Combine("Images", "SQL VectorSearch Performance Graph.png");

            ImageEditOptions options = new()
            {
                Size = GeneratedImageSize.W512xH512,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            // Generate the edited image and save to disk
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
        public static double CalculateSimilarity(float[] embedding1, float[] embedding2)
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

        /// <summary>
        /// Demonstrates text-to-speech (TTS) using the "tts-1" model.
        /// Converts the provided text (or a default humorous message) into spoken audio
        /// using the "Nova" voice and saves the result as an MP3 file.
        /// </summary>
        public static async Task TextToSpeechAsync(string text = null)
        {
            // Initialize the audio client with the TTS model
            AudioClient client = new("tts-1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Use provided text or fall back to a fun default message
            string input = text != null ? text : "Alright everyone, please keep your hands inside the code at all times. If you see any unexpected errors, don't worry—those are just features in disguise. And remember, if the demo works perfectly on the first try, it's probably witchcraft. Sit back, relax, and let's enjoy the magic (and maybe some debugging) together!";

            // Generate speech audio from the text using the Nova voice
            BinaryData speech = await client.GenerateSpeechAsync(input, GeneratedSpeechVoice.Nova);

            // Save the generated audio to an MP3 file with a unique name
            using FileStream stream = File.OpenWrite($"{Guid.NewGuid()}.mp3");
            speech.ToStream().CopyTo(stream);
        }
    }
}
