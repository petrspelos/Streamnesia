using System;

namespace Streamnesia.Payloads.Entities
{
    internal class PayloadModel
    {
        public string Name { get; set; }
        public string File { get; set; }
        public TimeSpan? Duration { get; set; }
        public string Antidote { get; set; }
    }
}