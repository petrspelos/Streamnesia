using System;
using System.Threading.Tasks;
using Streamnesia.CommandProcessing;
using Streamnesia.Payloads;
using Xunit;

namespace Streamnesia.Tests
{
    public class ConcurrencyTests
    {
        [Fact]
        public void Bug_ConcurrentAccessWhileGeneratingPoll_ShouldNotBreak()
        {
            var payloads = new []
            {
                new Payload { Name = "Payload 1" },
                new Payload { Name = "Payload 2" },
                new Payload { Name = "Payload 3" },
                new Payload { Name = "Payload 4" },
                new Payload { Name = "Payload 5" }
            };

            var poll = new CommandPolling();

            poll.GeneratePoll(payloads);

            Action getPayload = () =>
            {
                var p = poll.GetPayloadAt(0);
            };

            Action regeneratePoll = () =>
            {
                poll.GeneratePoll(payloads);
            };

            // CAREFUL: The problem does not occur consistently!
            // However, in 100 cases, the bug reproduction is pretty consistent.
            for(var i = 0; i < 100; i++)
                Parallel.Invoke(getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, regeneratePoll, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload, getPayload);

            Assert.NotNull(poll.GetPayloadWithMostVotes());
            Assert.NotNull(poll.GetPayloadAt(0));
        }
    }
}
