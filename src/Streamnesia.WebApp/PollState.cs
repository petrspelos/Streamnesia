using System;
using Streamnesia.CommandProcessing;

namespace Streamnesia.WebApp
{
    public class PollState
    {
        private const double CooldownDurationInSeconds = 5.0;
        private const double RapidfireDurationInSeconds = 20.0;
        private const double VotingDurationInSeconds = 40.0;
        private const int RapidfireChancePrecent = 10;

        private readonly Random _rng;

        public bool Cooldown { get; set; } = true;
        public bool ShouldRegenerate { get; private set; }
        public bool IsRapidfire { get; private set; }
        private DateTime? _cooldownEnd;
        private DateTime _pollStartDateTime = DateTime.Now;
        private DateTime _pollEndDateTime = DateTime.Now;

        public PollState(Random rng)
        {
            _rng = rng;
        }

        internal void StepForward()
        {
            ShouldRegenerate = false;
            PreventStacking();

            if(Cooldown)
            {
                if(!_cooldownEnd.HasValue)
                    _cooldownEnd = DateTime.Now.Add(TimeSpan.FromSeconds(CooldownDurationInSeconds));

                if(DateTime.Now < _cooldownEnd)
                    return;
                
                _cooldownEnd = null;
                Cooldown = false;
                _pollStartDateTime = DateTime.Now;

                if(ShouldStartRapidfire())
                {
                    IsRapidfire = true;
                    _pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(RapidfireDurationInSeconds));
                }
                else
                {
                    IsRapidfire = false;
                    _pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(VotingDurationInSeconds));
                }

                ShouldRegenerate = true;
            }
        }

        private void PreventStacking()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                Cooldown = true;
        }

        private bool ShouldStartRapidfire() => IsRapidfire ? false : _rng.Next(101) <= RapidfireChancePrecent;

        internal double GetProgressPercentage()
        {
            var max = (_pollEndDateTime - _pollStartDateTime).TotalSeconds;
            var current = (DateTime.Now - _pollStartDateTime).TotalSeconds;

            return (current / max) * 100.0;
        }
    }
}
