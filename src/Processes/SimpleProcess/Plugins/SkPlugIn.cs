using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzessFrameworkSamples.Plugins
{
    public class SkPlugIn
    {
        [KernelFunction(nameof(LoadFileAsync))]
        [Description("Loads the content from the file.")]
        public Task<string> LoadFileAsync([Description("The name of the file to be loaded.")]string url)
        {            
            return new StreamReader(url).ReadToEndAsync();
        }

        [KernelFunction(nameof(LoadWebContentAsync))]
        [Description("Loads the content from the given webpage.")]
        public async Task<string> LoadWebContentAsync([Description("The url location of the web resource to be loaded.")] string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var page = await client.GetStringAsync(url);
                return page;
            }
        }


        [KernelFunction(nameof(SaveTextAsync))]
        [Description("Saves the given text to a file.")]
        public async Task<string> SaveTextAsync(
        [Description("The text to be saved.")] string text,
        [Description("Where to save the text.")] string? file=null)
        {
            if (string.IsNullOrEmpty(file))
            {
                file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            }

            using (StreamWriter writer = new StreamWriter(file))
            {
                await writer.WriteAsync(text);
            }

            return $"Saved to file {file}";
        }

        [KernelFunction(nameof(SimplifyContentAsync))]
        [Description("Simplifies the given text by following user's instruction.")]
        public Task<string> SimplifyContentAsync(
            [Description("The text to be simplified.")] string text,
            [Description("Instructon which describes how to perform simplificaiton of the content..")] string instruction)
        {
            return Task.FromResult<string>("");
        }
    }
}
