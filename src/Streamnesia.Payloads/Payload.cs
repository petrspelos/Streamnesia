using System;

namespace Streamnesia.Payloads
{
    public class Payload
    {
        public string Name { get; set; }
        public string Angelcode { get; set; }
        public string ReverseAngelcode { get; set; }
        public TimeSpan? PayloadDuration { get; set; }
    }
}
