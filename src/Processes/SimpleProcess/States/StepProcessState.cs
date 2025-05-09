using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleProcess.States
{
    public class StepProcessState
    {
        public string? State { get; set; }

        public string? Content { get; set; }

        public DateTime StartedAt { get; set; }
    }
}
