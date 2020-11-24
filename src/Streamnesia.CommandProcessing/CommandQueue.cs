using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Streamnesia.CommandProcessing.Entities;
using Streamnesia.Payloads;

namespace Streamnesia.CommandProcessing
{
    public class CommandQueue
    {
        private Queue<Payload> _payloadQueue = new Queue<Payload>();
        private ICollection<PayloadExtension> _extensionQueue = new List<PayloadExtension>();

        public async Task StartCommandProcessingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Log("[[[ QUEUE ]]] Begins");
                try
                {
                    await ProcessCommandQueue();
                    await ProcessExtensionQueue();
                    await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The Queue itself threw");
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }
        }

        public Task AddPayloadAsync(Payload payload)
        {
            Log("Trying to enqueue a thing");
            try
            {
                _payloadQueue.Enqueue(payload);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("_payloadQueue.Enqueue threw");
                Console.WriteLine(e);
                Console.ResetColor();
            }

            return Task.CompletedTask;
        }

        private Task ProcessCommandQueue()
        {
            if (_payloadQueue.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (!Amnesia.LastInstructionWasExecuted())
            {
                Log("[CmdQ] Gotta wait, Amnesia is behind...");
                return Task.CompletedTask;
            }

            var payload = _payloadQueue.Dequeue();

            ProcessPayload(payload);
            return Task.CompletedTask;
        }

        private static void Log(string message) { return; Console.WriteLine($"[D] {message}"); }

        private async Task ProcessExtensionQueue()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                return;
            
            PayloadExtension extension;

            for(var i = 0; i < _extensionQueue.Count; i++)
            {
                extension = _extensionQueue.ElementAt(i);
                if(DateTime.Now < extension.ExecuteAfterDateTime)
                    continue;
                
                await Amnesia.ExecuteAsync(extension.Angelcode);
                _extensionQueue.Remove(extension);
                return;
            }

            return;
        }

        private void ProcessPayload(Payload payload)
        {
            foreach (var sequenceItem in payload.Sequence)
            {
                try
                {
                    _extensionQueue.Add(new PayloadExtension
                    {
                        Angelcode = sequenceItem.AngelCode,
                        ExecuteAfterDateTime = DateTime.Now.Add(sequenceItem.Delay)
                    });
                }
                catch(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("_extensionQueue.Add threw");
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }
        }
    }
}
