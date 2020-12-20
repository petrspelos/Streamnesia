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
#region Dependencies & Constructors
        private readonly IHubContext<UpdateHub> _hub;
        private readonly CommandPolling _poll;
        private readonly CommandQueue _cmdQueue;
        private readonly IPayloadLoader _payloadLoader;
        private readonly Bot _bot;
        private readonly Random _rng;

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

            _ = _cmdQueue.StartCommandProcessingAsync(CancellationToken.None);
            _ = StartLoop();
        }
#endregion

        public async Task StartLoop()
        {
            var payloads = await _payloadLoader.GetPayloadsAsync();
            var isRapidMode = false;

            _bot.OnVoted = (displayname, vote) => {
                if(vote < 0)
                    return;

                if(isRapidMode)
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
            };
            _bot.OnDeathSet = text => {
                Amnesia.SetDeathHintTextAsync(text);
            };
            _bot.OnMessageSent = text => {
                Amnesia.DisplayTextAsync(text);
            };
            _bot.DevCommand = cmd =>
            {
                Amnesia.ExecuteAsync(cmd);
            };
            _bot.DevPayload = index =>
            {
                var payload = payloads.ElementAtOrDefault(index);
                if (payload is null)
                    return;

                _cmdQueue.AddPayload(payload);
            };

            var cooldown = true;
            DateTime? cooldownEnd = null;

            var pollStartDateTime = DateTime.Now;
            var pollEndDateTime = DateTime.Now;

            var per = 0.0;

            while(true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                if(cooldown)
                {
                    if(!cooldownEnd.HasValue)
                    {
                        cooldownEnd = DateTime.Now.Add(TimeSpan.FromSeconds(5));
                    }

                    if(DateTime.Now < cooldownEnd)
                    {
                        // maybe push an update about how long until
                        // cooldown ends
                        continue;
                    }
                    else
                    {
                        cooldownEnd = null;
                        cooldown = false;
                        pollStartDateTime = DateTime.Now;
                        
                        if(_rng.Next(101) <= 10)
                        {
                            isRapidMode = true;
                            pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(20));
                        }
                        else
                        {
                            isRapidMode = false;
                            pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(40));
                        }

                        _poll.GeneratePoll(payloads);
                    }
                }

                var max = (pollEndDateTime - pollStartDateTime).TotalSeconds;
                var current = (DateTime.Now - pollStartDateTime).TotalSeconds;

                per = (current / max) * 100.0;
                
                if(per < 100.0)
                {
                    await SendCurrentStatusAsync(per, _poll.GetPollOptions(), isRapidMode);
                }
                else
                {
                    await SendClearStatusAsync();
                    var payload = _poll.GetPayloadWithMostVotes();
                    _cmdQueue.AddPayload(payload);

                    cooldown = true;
                }
            }
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
