using Microsoft.SemanticKernel.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiktoken;

namespace semantickernelsample
{
    internal class TextSplittingSample
    {

        public static void Split()
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

            //WriteParagraphsToConsole(paragraphs);
        }

        private static void WriteParagraphsToConsole(List<string> paragraphs)
        {
            for (var i = 0; i < paragraphs.Count; i++)
            {
                Console.WriteLine(paragraphs[i]);

                if (i < paragraphs.Count - 1)
                {
                    Console.WriteLine("------------------------");
                }
            }
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
        """;
    }
}

