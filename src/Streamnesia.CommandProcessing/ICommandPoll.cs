using Streamnesia.Payloads;
using System.Collections.Generic;

namespace Streamnesia.CommandProcessing
{
    public interface ICommandPoll
    {
        void SetPayloadSource(IEnumerable<Payload> payloads);
        void GenerateNew();
        IEnumerable<PollOption> GetPollOptions();
        Payload GetPayloadWithMostVotes();
        Payload GetPayloadAt(int index);
        void Vote(string displayname, int vote);
    }
}