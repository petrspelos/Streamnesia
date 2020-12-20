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
        private IHubContext<UpdateHub> _hub;

        public StreamnesiaHub(IHubContext<UpdateHub> hub)
        {
            _hub = hub;
            _ = StartLoop();
        }

        public async Task StartLoop()
        {
            var poll = new CommandPolling();
            IPayloadLoader payloadLoader = new LocalPayloadLoader();

            CommandQueue cmdQueue = new CommandQueue();
            _ = cmdQueue.StartCommandProcessingAsync(CancellationToken.None);

            var payloads = await payloadLoader.GetPayloadsAsync();
            var bot = new Bot();
            var isRapidMode = false;
            var rng = new Random();

            bot.OnVoted = (displayname, vote) => {
                if(vote < 0)
                    return;

                if(isRapidMode)
                {
                    try
                    {
                        var payload = poll.GetPayloadAt(vote);
                        cmdQueue.AddPayloadAsync(payload);
                        return;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                poll.Vote(displayname, vote);
            };
            bot.OnDeathSet = text => {
                Amnesia.SetDeathHintTextAsync(text);
            };
            bot.OnMessageSent = text => {
                Amnesia.DisplayTextAsync(text);
            };
            bot.DevCommand = cmd =>
            {
                Amnesia.ExecuteAsync(cmd);
            };
            bot.DevPayload = index =>
            {
                var payload = payloads.ElementAtOrDefault(index);
                if (payload is null)
                    return;

                cmdQueue.AddPayloadAsync(payload);
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
                        
                        if(rng.Next(101) <= 10)
                        {
                            isRapidMode = true;
                            pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(20));
                        }
                        else
                        {
                            isRapidMode = false;
                            pollEndDateTime = DateTime.Now.Add(TimeSpan.FromSeconds(40));
                        }

                        poll.GeneratePoll(payloads);
                    }
                }

                var max = (pollEndDateTime - pollStartDateTime).TotalSeconds;
                var current = (DateTime.Now - pollStartDateTime).TotalSeconds;

                per = (current / max) * 100.0;
                
                if(per < 100.0)
                {
                    await SendCurrentStatusAsync(per, poll.GetPollOptions(), isRapidMode);
                }
                else
                {
                    await SendClearStatusAsync();
                    var payload = poll.GetPayloadWithMostVotes();
                    await cmdQueue.AddPayloadAsync(payload);

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
