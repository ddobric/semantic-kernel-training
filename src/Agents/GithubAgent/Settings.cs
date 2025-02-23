using Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubAgent
{
    internal class Settings
    {
        public GitHubSettings GitHubSettings { get; set; }
        
        public string Model { get; set; }

        public string Key { get; set; }
    }
}
