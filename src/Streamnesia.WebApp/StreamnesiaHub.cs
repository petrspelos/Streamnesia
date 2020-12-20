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

        private bool _isRapidFire;
        private IEnumerable<Payload> _payloads;
        private bool _cooldown = true;
        private DateTime? _cooldownEnd;
        private DateTime _pollStartDateTime = DateTime.Now;
        private DateTime _pollEndDateTime = DateTime.Now;
        private double _rapidChance = 0.0;

        public StreamnesiaHub(
            IHubContext<UpdateHub> hub,
            CommandPolling poll,
            CommandQueue cmdQueue,
            IPayloadLoader payloadLoader,
            Bot bot,
            Random rng
        )
        {
            _hub = hub;
            _poll = poll;
            _cmdQueue = cmdQueue;
            _payloadLoader = payloadLoader;
            _bot = bot;
            _rng = rng;

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
            if(_cooldown)
            {
                if(!_cooldownEnd.HasValue)
                {
                    _cooldownEnd = DateTime.Now.Add(TimeSpan.FromSeconds(5));
                }

                if(DateTime.Now < _cooldownEnd)
                {
                    // maybe push an update about how long until
                    // cooldown ends
                    return;
                }
                else
                {
                    _cooldownEnd = null;
                    _cooldown = false;
                    _pollStartDateTime = DateTime.Now;
                    
                    if(_rng.Next(101) <= 10)
                    {
                        _isRapidFire = true;
                        _pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(20));
                    }
                    else
                    {
                        _isRapidFire = false;
                        _pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(40));
                    }

                    _poll.GeneratePoll(_payloads);
                }
            }

            var max = (_pollEndDateTime - _pollStartDateTime).TotalSeconds;
            var current = (DateTime.Now - _pollStartDateTime).TotalSeconds;

            _rapidChance = (current / max) * 100.0;
            
            if(_rapidChance < 100.0)
            {
                await SendCurrentStatusAsync(_rapidChance, _poll.GetPollOptions(), _isRapidFire);
            }
            else
            {
                await SendClearStatusAsync();
                var payload = _poll.GetPayloadWithMostVotes();
                _cmdQueue.AddPayload(payload);

                _cooldown = true;
            }
        }

        private void OnUserVoted(string displayname, int vote)
        {
            if(vote < 0)
                    return;

            if(_isRapidFire)
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
                    description = $"Send <code class='code-pop'>{p.Index}</code> in the chat to vote for:"
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
