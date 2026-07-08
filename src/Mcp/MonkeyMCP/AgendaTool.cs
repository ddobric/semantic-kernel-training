using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyMCP
{
    [McpServerToolType]
    internal class AgendaTool
    {
        [McpServerTool, Description("Show the agenda for the DWX Masterclass.")]
        public string ShowDwxMasterclassAgenda()
        {
            const int W = 80;
            string top = "┌" + new string('─', W - 2) + "┐";
            string bot = "└" + new string('─', W - 2) + "┘";
            string mid = "├" + new string('─', W - 2) + "┤";

            string C(string t)   // centre
            {
                int pad = Math.Max(0, W - 2 - t.Length);
                return $"│{new string(' ', pad / 2)}{t}{new string(' ', pad - pad / 2)}│";
            }
            string L(string t)   // left-align
            {
                if (t.Length > W - 2) t = t[..(W - 2)];
                return $"│{t.PadRight(W - 2)}│";
            }
            IEnumerable<string> Wrap(string text, int max)
            {
                var line = new StringBuilder();
                foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length == 0) line.Append(word);
                    else if (line.Length + 1 + word.Length <= max) line.Append(' ').Append(word);
                    else { yield return line.ToString(); line.Clear().Append(word); }
                }
                if (line.Length > 0) yield return line.ToString();
            }

            // ── Agenda data ──────────────────────────────────────────────────────────

            var sessions = new[]
            {
        (Time: "10:45 – 11:15", Title: "FOUNDATIONS", Topics: new[]
        {
            ("Tokens",
             "What are tokens? Byte-Pair Encoding, token pricing, " +
             "counting tokens and the Tokenizer, working with tokens in C#"),

            ("Embeddings",
             "Embedding vectors explained, embedding models, cosine similarity, " +
             "semantic similarity in C#, use-cases: search, classification, " +
             "clustering, recommendations"),

            ("Vector Databases",
             "Popular vector databases overview (Qdrant, Weaviate, Redis, Azure AI Search, …), " +
             "SQL Server 2026 native vector search, T-SQL VECTOR type, " +
             "INSERT & VECTOR_DISTANCE queries in C#"),
        }),

        (Time: "13:00 – 14:00", Title: "COMPLETIONS & RAG", Topics: new[]
        {
            ("Completions",
             "How LLMs complete text, Transformers under the hood, " +
             "ChatCompletions API with the OpenAI NuGet package in C#, " +
             "exploring token probabilities"),

            ("Retrieval Augmented Generation (RAG)",
             "What is RAG and why it matters (arxiv 2005.11401), building a RAG pipeline: " +
             "chunking → embeddings → in-memory vector DB → similarity search, " +
             "the embedding-similarity vs. true-relevance problem, " +
             "reranking: second-pass scoring for higher precision results"),
        }),

        (Time: "15:45 – 16:15", Title: "AGENTS & MCP", Topics: new[]
        {
            ("Microsoft Agent Framework (MAF)",
             "Single Agent & service connectors (Azure OpenAI, Anthropic, Gemini, Ollama, …), " +
             "middleware hooks (safety filters, logging, custom policies), " +
             "agent memory & context providers (Mem0, Redis, Neo4j, vector retrieval), " +
             "graph-based agent workflows with checkpointing, " +
             "multi-agent orchestration: sequential, concurrent, handoff, group chat, Magentic-One, " +
             "declarative agents & workflows via YAML, function calling & tool/plugin integration, " +
             "A2A (Agent-to-Agent) protocol & MCP tool discovery, " +
             "migration assistants from Semantic Kernel & AutoGen"),

            ("MCP – Model Context Protocol",
             "Created by Anthropic, adopted across the AI ecosystem, " +
             "how MCP works under the hood: server discovery, tool invocation, structured messaging"),
        }),
    };

            // ── Render ───────────────────────────────────────────────────────────────

            var sb = new StringBuilder();

            sb.AppendLine(top);
            sb.AppendLine(C("DWX 2026  ·  MASTER CLASS"));
            sb.AppendLine(C("Architecting and Development of Custom Agents"));
            sb.AppendLine(C(""));
            sb.AppendLine(C("Dr. Damir Dobric"));
            sb.AppendLine(C("Lead Software Architect · daenet GmbH / ACP Digital"));
            sb.AppendLine(C("Microsoft Regional Director  ·  MVP: AI"));
            sb.AppendLine(mid);
            sb.AppendLine(C("GitHub: https://github.com/ddobric/semantic-kernel-training"));
            sb.AppendLine(bot);
            sb.AppendLine();

            foreach (var (time, title, topics) in sessions)
            {
                sb.AppendLine(top);
                sb.AppendLine(L($"  {time}   {title}"));
                sb.AppendLine(mid);

                foreach (var (name, desc) in topics)
                {
                    sb.AppendLine(L($"  ▶  {name}"));
                    foreach (var line in Wrap(desc, W - 8))
                        sb.AppendLine(L($"       {line}"));
                    sb.AppendLine(L(""));
                }

                sb.AppendLine(bot);
                sb.AppendLine();
            }

            return sb.ToString();
        }


        [McpServerTool, Description("Show the agenda for the tutorial.")]
        public string ShowTutorialAgenda()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"
    ╔══════════════════════════════════════════════════════════════════════╗
    ║          ✦  A long time ago in a galaxy far, far away...  ✦        ║
    ║                                                                      ║
    ║         ...actually, it's TODAY. In COLOGNE. At ECS 2026! 🚀        ║
    ╚══════════════════════════════════════════════════════════════════════╝
            ");

            sb.AppendLine(@"
       _____ _____ _____   ___   ___ ___   __
      | ____/ ____/ ____| |__ \ / _ \__ \ / /_
      | |__ | |   | (___      ) | | | | ) | '_ \
      |  __|| |    \___ \    / /| | | |/ /| (_) |
      | |___| |___ ____) |  / /_| |_| / /_ \__, |
      |______\____|_____/  |____|\___/____|   /_/
            ");

            sb.AppendLine("  ═══════════════════════════════════════════════════════════════");
            sb.AppendLine("  ║                                                             ║");
            sb.AppendLine("  ║   ⚡ AI FOR ENTERPRISE DEVELOPERS TUTORIAL ⚡              ║");
            sb.AppendLine("  ║                                                             ║");
            sb.AppendLine("  ║   Presented by: Damir Dobric                                ║");
            sb.AppendLine("  ║   Microsoft MVP & Regional Director                        ║");
            sb.AppendLine("  ║   \"Strong with the Force, this one is.\" — Yoda      ║");
            sb.AppendLine("  ║                                                             ║");
            sb.AppendLine("  ═══════════════════════════════════════════════════════════════");

            sb.AppendLine();
            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │              📋  TODAY'S AGENDA  (May the Code Be With You) │");
            sb.AppendLine("  ├─────────┬───────────────────────────────────────────────────┤");
            sb.AppendLine("  │  09:00  │ 🚀 Tutorial Begins — \"The Force Awakens\"              │");
            sb.AppendLine("  │  10:00  │ ☕ Morning Break — \"The Caffeine Strikes Back\"         │");
            sb.AppendLine("  │  10:30  │ 💻 Back to Code — \"Return of the Developer\"           │");
            sb.AppendLine("  │  12:00  │ 🍕 Lunch Break — \"The Phantom Menu\"                  │");
            sb.AppendLine("  │  13:00  │ ⚡ Afternoon Session — \"A New Prompt\"                │");
            sb.AppendLine("  │  14:00  │ 🍩 Afternoon Break — \"Attack of the Donuts\"           │");
            sb.AppendLine("  │  14:30  │ 🔥 Final Push — \"Revenge of the Codebase\"             │");
            sb.AppendLine("  │  16:30  │ 🎉 Finish — \"The Developer Strikes Out... the door\"  │");
            sb.AppendLine("  └─────────┴───────────────────────────────────────────────────┘");

            sb.AppendLine();
            sb.AppendLine("        ╔═══════════════════════════════════════════════════╗");
            sb.AppendLine("        ║  \"Do. Or do not. There is no try...catch.\" —Yoda  ║");
            sb.AppendLine("        ║                                                   ║");
            sb.AppendLine("        ║    ...okay fine, always use try-catch. 😅          ║");
            sb.AppendLine("        ╚═══════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine("  May the Force (and your Wi-Fi) be with you! 🌟");
            sb.AppendLine();

            return sb.ToString();
        }

        [McpServerTool, Description("Show the agenda for the Best Practices Enterprise Devs session.")]
        public string ShowBestPracticedEntepriseDevsSessionAgenda()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"
    ╔══════════════════════════════════════════════════════════════════════════╗
    ║  Best Practices — Building Enterprise Applications with AI Agents      ║
    ║  Presented by: Dr. Damir Dobric                                        ║
    ╚══════════════════════════════════════════════════════════════════════════╝
            ");

            sb.AppendLine("  ═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("  ║                                                                 ║");
            sb.AppendLine("  ║   📋  SESSION AGENDA                                            ║");
            sb.AppendLine("  ║                                                                 ║");
            sb.AppendLine("  ═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🔢  EMBEDDINGS: Turning Text Into Vectors                     │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Core idea — Text → numerical vectors (embeddings)           │");
            sb.AppendLine("  │    so machines can compare meaning.                             │");
            sb.AppendLine("  │  • How it works — \"Making a vector from text\",                  │");
            sb.AppendLine("  │    embedding models with example sentences.                     │");
            sb.AppendLine("  │  • Measuring similarity — dot product, vector norm,             │");
            sb.AppendLine("  │    cosine similarity to quantify closeness.                     │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🎯  WHEN EMBEDDINGS ARE USEFUL                                │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Primary use cases — semantic search, classification,         │");
            sb.AppendLine("  │    clustering, and recommendation workflows.                    │");
            sb.AppendLine("  │  • Why it matters — matching by meaning, not just keywords.     │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  📚  RAG (Retrieval-Augmented Generation)                      │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • What RAG is — extends model knowledge by retrieving          │");
            sb.AppendLine("  │    relevant info and combining it with generation.              │");
            sb.AppendLine("  │  • Mechanism — information retrieval + text generation          │");
            sb.AppendLine("  │    (RAG paper: arXiv:2005.11401).                               │");
            sb.AppendLine("  │  • Demo — chunking → embeddings → in-memory vector DB           │");
            sb.AppendLine("  │    → similarity calculation.                                    │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🔄  THE RAG RELEVANCE PROBLEM + RERANKING                     │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Key limitation — embedding similarity ≠ true relevance,      │");
            sb.AppendLine("  │    especially with nuance, negation, or specificity.            │");
            sb.AppendLine("  │  • Reranking — second-stage model scores top chunks against     │");
            sb.AppendLine("  │    the query to keep only the best few for generation.          │");
            sb.AppendLine("  │  • Operational — \"Retrieve top-k, rerank, keep top-n\".          │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🗄️  VECTOR DATABASES AND STORAGE OPTIONS                      │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Where embeddings live — vector databases, graph databases.    │");
            sb.AppendLine("  │  • Ecosystem — SQL Server 2026, Pinecone, Weaviate, Milvus,     │");
            sb.AppendLine("  │    Qdrant, pgvector, MongoDB Atlas, Elasticsearch, Redis,       │");
            sb.AppendLine("  │    Azure AI Search, FAISS, Annoy, ScaNN, and others.            │");
            sb.AppendLine("  │  • ⚠️ Keep in mind!! — tradeoffs in performance, scaling,       │");
            sb.AppendLine("  │    retrieval quality, and integration.                          │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🤖  AGENTS: From LLM Calls to Tool-Using Systems              │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Agent concept — systems that call tools/functions to act     │");
            sb.AppendLine("  │    or fetch data, not just respond.                             │");
            sb.AppendLine("  │  • Implementation — agent with instructions, name, and toolset  │");
            sb.AppendLine("  │    (e.g., process and vehicle info functions).                  │");
            sb.AppendLine("  │  • Framework — Microsoft Agent Framework.                       │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🛠️  EXAMPLE AGENT WORKFLOW (T-SQL Agent)                       │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • User question — \"How many messages were created today?\"      │");
            sb.AppendLine("  │  • Tool-driven steps — retrieve schema → get current UTC date   │");
            sb.AppendLine("  │    → compose T-SQL → query SQL Server → format results.         │");
            sb.AppendLine("  │  • Takeaway — agents couple reasoning with deterministic        │");
            sb.AppendLine("  │    tool execution.                                              │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🐾  CLAW                                                      │");
            sb.AppendLine("  ├─────────────────────────────────────────────────────────────────┤");
            sb.AppendLine("  │  • Build your own CLAW.                                         │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            sb.AppendLine("  ╔═════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("  ║  ⭐  BOTTOM LINE                                               ║");
            sb.AppendLine("  ╠═════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("  ║  • Embeddings — foundation for meaning-based retrieval.         ║");
            sb.AppendLine("  ║  • RAG — improves factual grounding; needs reranking to         ║");
            sb.AppendLine("  ║    close the \"similar-but-not-relevant\" gap.                    ║");
            sb.AppendLine("  ║  • Vector databases — scalable retrieval across many options.    ║");
            sb.AppendLine("  ║  • Agents — LLMs that use tools, query data, and execute        ║");
            sb.AppendLine("  ║    workflows reliably.                                          ║");
            sb.AppendLine("  ╚═════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            sb.AppendLine("  ┌─────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("  │  🚀  NEXT DIRECTIONS                                           │");
            sb.AppendLine("  │  • Create a one-page executive summary version.                 │");
            sb.AppendLine("  │  • Turn this into speaker notes you can read verbatim.          │");
            sb.AppendLine("  │  • Add a \"key takeaways + recommended stack\" closing slide.      │");
            sb.AppendLine("  └─────────────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
