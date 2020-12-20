using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Streamnesia.CommandProcessing;
using Streamnesia.Payloads;
using Streamnesia.Twitch;
using Streamnesia.WebApp.Hubs;

namespace Streamnesia.WebApp
{
    public class StreamnesiaHub
    {
        private readonly IHubContext<UpdateHub> _hub;
        private readonly CommandPolling _poll;
        private readonly CommandQueue _cmdQueue;
        private readonly IPayloadLoader _payloadLoader;
        private readonly Bot _bot;
        private readonly Random _rng;
        private readonly PollState _pollState;

        private IEnumerable<Payload> _payloads;

        public StreamnesiaHub(
            IHubContext<UpdateHub> hub,
            CommandPolling poll,
            CommandQueue cmdQueue,
            IPayloadLoader payloadLoader,
            Bot bot,
            Random rng,
            PollState pollState
        )
        {
            _hub = hub;
            _poll = poll;
            _cmdQueue = cmdQueue;
            _payloadLoader = payloadLoader;
            _bot = bot;
            _rng = rng;
            _pollState = pollState;

            _bot.OnVoted = OnUserVoted;
            _bot.OnDeathSet = text => {
                Amnesia.SetDeathHintTextAsync(text);
            };
            _bot.OnMessageSent = text => {
                Amnesia.DisplayTextAsync(text);
            };
            _bot.DevCommand = cmd => {
                Amnesia.ExecuteAsync(cmd);
            };

            _ = _cmdQueue.StartCommandProcessingAsync(CancellationToken.None);
            _ = StartLoop();
        }

        public async Task StartLoop()
        {
            _payloads = await _payloadLoader.GetPayloadsAsync();

            while(true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await UpdatePollAsync();
            }
        }

        private async Task UpdatePollAsync()
        {
            _pollState.StepForward();

            if(_pollState.Cooldown)
                return;

            if(_pollState.ShouldRegenerate)
                _poll.GeneratePoll(_payloads);

            var progressPercentage = _pollState.GetProgressPercentage();

            if(progressPercentage < 100.0)
            {
                await SendCurrentStatusAsync(progressPercentage, _poll.GetPollOptions(), _pollState.IsRapidfire);
            }
            else
            {
                await SendClearStatusAsync();

                var payload = _poll.GetPayloadWithMostVotes();
                _cmdQueue.AddPayload(payload);

                _pollState.Cooldown = true;
            }
        }

        private void OnUserVoted(string displayname, int vote)
        {
            vote--; // From label to index

            if(vote < 0)
                return;

            if(_pollState.IsRapidfire)
            {
                try
                {
                    var payload = _poll.GetPayloadAt(vote);
                    _cmdQueue.AddPayload(payload);
                    return;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            _poll.Vote(displayname, vote);
        }

        private async Task SendCurrentStatusAsync(double percentage, IEnumerable<PollOption> options, bool isRapidMode)
        {
            await _hub.Clients.All.SendCoreAsync("UpdateTimePercentage", new object[] { new {
                percentage,
                options = options.Select(p => new {
                    name = p.Name,
                    votes = p.Votes,
                    description = $"Send <code class='code-pop'>{p.Index + 1}</code> in the chat to vote for:"
                }),
                rapidFire = isRapidMode
            } });
        }

        private async Task SendClearStatusAsync()
        {
            await _hub.Clients.All.SendCoreAsync("UpdateTimePercentage", new object[] { new {
                percentage = 100.0,
                options = new object[0],
                rapidFire = false
            } });
        }
    }
}
