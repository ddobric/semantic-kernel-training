# OpenAI.Samples

A .NET 9 console application demonstrating the core capabilities of the [OpenAI .NET SDK](https://www.nuget.org/packages/OpenAI) (v2.10.0). Each sample is a self-contained method that showcases a different OpenAI API feature — from text embeddings and chat completions to image generation, vision, text-to-speech, and retrieval-augmented generation (RAG).

## Prerequisites

| Requirement | Details |
|---|---|
| **.NET 10 SDK** | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **OpenAI API Key** | Set the environment variable `OPENAI_API_KEY` |
| **Chat Model Deployment** | Set the environment variable `OPENAI_CHATCOMPLETION_DEPLOYMENT` (e.g. `gpt-4o`) |

## Getting Started

```bash
# Clone and navigate to the project
cd src/OpenAI.Samples

# Restore dependencies
dotnet restore

# Set required environment variables (PowerShell)
$env:OPENAI_API_KEY = "<your-openai-api-key>"
$env:OPENAI_CHATCOMPLETION_DEPLOYMENT = "gpt-4o"

# Run the application
dotnet run
```

Samples are launched from `Program.cs` → `Main()`. Comment or uncomment the calls in `Main()` to choose which samples to run.

---

## Samples Overview

### 1. Embedding Generation & Cosine Similarity

**Method:** `Program.CreateEmbeddingsAsync()`

Generates vector embeddings for two user-provided text strings using the `text-embedding-3-large` model, then calculates the **cosine similarity** between them. This demonstrates how embeddings can be used to measure semantic relatedness between texts (1.0 = identical meaning, 0.0 = unrelated).

**Key concepts:** Embedding generation, cosine similarity, semantic search fundamentals.

---

### 2. Document Classification via Embeddings

**Method:** `ClassificationSample.RunAsync()`  
**File:** `ClassificationSample.cs`

Loads text documents from the `Docs/` folder (Economy, Science, Sport), generates embeddings for each using **both** `text-embedding-3-large` and `text-embedding-3-small` models, and stores them in memory. When the user enters a text query, it computes cosine similarity against all stored document embeddings and displays scores — effectively classifying the input into the most relevant document category.

**Key concepts:** Document embeddings, text classification, comparing large vs. small embedding models.

**Sample documents:**
- `Economy.txt` — IMF economic growth forecast article
- `Science.txt` — Paleontology research paper abstract
- `sport.txt` — Article about the definition of sport

---

### 3. Streaming Chat Completion

**Method:** `Program.ChatStreamingAsync()`

An interactive chat loop that streams the model's response **token by token** in real time, providing a "typewriter" effect. Maintains full conversation history across turns for multi-turn dialogue.

**Key concepts:** Streaming API, real-time token delivery, multi-turn conversation history.

---

### 4. Chat Completions with Log Probabilities

**Method:** `Program.ChatChatCompletionsAsync()`

An interactive chat loop that returns completions along with the **top-5 token log probabilities** for each generated token. The selected token is highlighted in green, while alternatives are shown in cyan. This is invaluable for understanding model confidence and exploring what other tokens the model considered.

**Key concepts:** Log probabilities, token-level confidence analysis, prompt completion.

---

### 5. Text-to-Speech (TTS)

**Method:** `Program.TextToSpeechAsync()`

Converts a text string into spoken audio using the `tts-1` model with the **Nova** voice. The generated audio is saved as an MP3 file. Accepts an optional text parameter or uses a fun default message.

**Key concepts:** Text-to-speech generation, voice selection, audio file output.

---

### 6. Vision (Image Understanding)

**Method:** `Program.VisionAsync()`

Sends a local image (`Images/testimage.png`) to the chat model along with a text prompt asking for a description. Demonstrates multimodal input where text and image are combined in a single user message.

**Key concepts:** Multimodal chat, image analysis, vision capabilities.

---

### 7. Image Generation (DALL-E 3)

**Method:** `Program.ImageGenerationAsync()`

Generates a high-quality image from a detailed text prompt using **DALL-E 3**. The generated image is saved as a PNG file. Demonstrates configuration of quality, size (1792×1024), style (natural), and response format (raw bytes).

**Key concepts:** Text-to-image generation, DALL-E 3, image generation options.

---

### 8. Image Editing (DALL-E 2)

**Method:** `Program.SimpleImageEditAsync()`

Demonstrates the image editing (inpainting) API using **DALL-E 2** with a source image and a mask image. The mask defines the region of the image to be modified based on the text prompt.

> **Note:** This sample is experimental and may not work without a properly formatted RGBA mask image with transparent regions indicating areas to edit.

**Key concepts:** Image inpainting, mask-based editing, DALL-E 2.

---

### 9. Retrieval-Augmented Generation (RAG) with Assistants API

**Method:** `AssistentSample.RunRetrievalAugmentedGenerationAsync()`  
**File:** `AssistentSample.cs`

Demonstrates a full RAG workflow using the OpenAI Assistants API:

1. **Uploads** a JSON sales data document to OpenAI file storage
2. **Creates an assistant** with `FileSearchToolDefinition` (for searching the uploaded file) and `CodeInterpreterToolDefinition` (for generating charts and visualizations)
3. **Creates a thread** with an initial user query asking about product sales and trend visualization
4. **Polls** the thread run until completion
5. **Retrieves and displays** all messages including generated text, file citations, and chart images
6. **Cleans up** all created resources (thread, assistant, uploaded file)

**Key concepts:** Assistants API, file search, code interpreter, vector stores, RAG pattern, resource lifecycle management.

---

## Project Structure

```
OpenAI.Samples/
├── Program.cs                  # Entry point & most samples (embeddings, chat, vision, images, TTS)
├── AssistentSample.cs          # RAG sample using the Assistants API
├── ClassificationSample.cs     # Document classification via embeddings
├── Docs/                       # Sample text documents for classification
│   ├── Economy.txt
│   ├── Science.txt
│   └── sport.txt
├── Images/                     # Sample images for vision and image editing
├── OpenAI.Samples.csproj       # Project file (.NET 9, OpenAI SDK 2.10.0)
└── README.md                   # This file
```

## Utility: Cosine Similarity

**Method:** `Program.CalculateSimilarity(float[], float[])`

A helper method used across multiple samples to compute the cosine similarity between two embedding vectors. Returns a value between -1 and 1, where 1 indicates identical direction (high semantic similarity) and 0 indicates orthogonality (no similarity).

## Environment Variables

| Variable | Required By | Description |
|---|---|---|
| `OPENAI_API_KEY` | All samples | Your OpenAI API key |
| `OPENAI_CHATCOMPLETION_DEPLOYMENT` | Chat, Vision samples | The chat model deployment name (e.g. `gpt-4o`) |
