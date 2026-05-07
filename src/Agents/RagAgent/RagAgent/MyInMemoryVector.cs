using System;
using System.Collections.Generic;
using System.Text;

namespace RagAgent
{
    /// <summary>
    /// Represents a single text chunk stored in the in-memory vector knowledge base,
    /// together with its embedding vector and a source reference.
    /// </summary>
    class MyInMemoryVector
    {
        /// <summary>
        /// A reference identifier indicating the source document of this chunk.
        /// </summary>
        public string Ref { get; set; }

        /// <summary>
        /// The embedding vector generated for <see cref="Chunk"/>.
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// The original text chunk extracted from the knowledge base document.
        /// </summary>
        public string Chunk { get; set; }
    }
}
