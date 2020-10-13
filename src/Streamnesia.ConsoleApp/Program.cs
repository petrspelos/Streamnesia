using System;
using System.Threading.Tasks;
using Streamnesia.CommandProcessing;
using Streamnesia.Payloads;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Streamnesia.Twitch;

namespace Streamnesia.ConsoleApp
{
    class Program
    {
        private static Random Rng = new Random();
        private static CommandQueue CommandQueue = new CommandQueue();

        static async Task Main(string[] args)
        {
            IPayloadLoader payloadLoader = new LocalPayloadLoader();
            var payloads = await payloadLoader.GetPayloadsAsync();

            _ = CommandQueue.StartCommandProcessingAsync(CancellationToken.None);

            if(args.Length == 2 && args[0] == "--chaos" && uint.TryParse(args[1], out var wait))
            {
                await RunChaosModAsync(payloads, wait);
            }
            else if(args.Any() && args[0] == "--bot")
            {
                var bot = new Bot();
                bot.OnCommandSelected = async index => {
                    if(index > 22 || index < 0)
                        return;

                    var p = payloads.ElementAt(index);
                    await CommandQueue.AddPayloadAsync(p);
                };
                bot.OnMessageSent = async msg => {
                    await Amnesia.DisplayTextAsync(msg);
                };
                bot.OnDeathSet = msg => {
                    Amnesia.SetDeathHintTextAsync(msg);
                };
            }
            else if(args.Length == 2 && args[0] == "--run")
            {
                await CommandQueue.AddPayloadAsync(payloads.First(p => p.Name.Contains(args[1])));
            }

            for(var i = 0; i < payloads.Count(); i++)
            {
                var p = payloads.ElementAt(i);
                Console.WriteLine($"{i} - {p.Name}");
            }

            await Task.Delay(-1);
        }

        static async Task RunChaosModAsync(IEnumerable<Payload> payloads, uint wait)
        {
            var interval = TimeSpan.FromSeconds(wait);

            while(true)
            {
                Console.WriteLine($"Waiting for {wait} second(s)...");
                await Task.Delay(interval);
                var p = payloads.Random(Rng);
                Console.WriteLine($"Running: {p.Name}");
                await CommandQueue.AddPayloadAsync(p);
            }
        }
    }

    public static class EnumerableExtensions
    {
        public static T Random<T>(this IEnumerable<T> input, Random rng)
        {
            return input.ElementAt(rng.Next(input.Count()));
        }
    }
}
