using System;
using System.Collections.Generic;
using System.Text;

namespace Streamnesia.Payloads.Entities
{
    public class SequenceModel
    {
        public string File { get; set; }
        public TimeSpan Delay { get; set; }
    }
}
