namespace Streamnesia.Payloads
{
    public class StreamnesiaConfig
    {
        public bool AllowOnScreenMessages { get; set; } = true;
        public bool AllowDeathMessages { get; set; } = true;
        public bool DownloadLatestPayloads { get; set; } = true;
        public bool UseVanillaPayloads { get; set; } = true;
        public string CustomPayloadsFile { get; set; } = "custom-payloads.json";
        public int RapidFireChancePercent { get; set; } = 10;
        public double VotingDurationInSeconds { get; set; } = 40.0;
        public double RapidfireDurationInSeconds { get; set; } = 20.0;
        public double CooldownDurationInSeconds { get; set; } = 5.0;
    }
}
