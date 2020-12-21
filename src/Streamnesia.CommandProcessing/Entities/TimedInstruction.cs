using System;

namespace Streamnesia.CommandProcessing.Entities
{
    internal class TimedInstruction
    {
        public DateTime ExecuteAfterDateTime { get; set; }
        public string Angelcode { get; set; }
    }
}
