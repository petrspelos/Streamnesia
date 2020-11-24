using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Streamnesia.CommandProcessing.Entities;
using Streamnesia.Payloads;

namespace Streamnesia.CommandProcessing
{
    public class CommandQueue
    {
        private ConcurrentQueue<Payload> _payloadQueue = new ConcurrentQueue<Payload>();
        private ConcurrentQueue<PayloadExtension> _instructionQueue = new ConcurrentQueue<PayloadExtension>();

        public async Task StartCommandProcessingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessCommandQueue();
                await ProcessInstructionQueue();
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }
        }

        public Task AddPayloadAsync(Payload payload)
        {
            if(payload is null)
            {
                Console.WriteLine("A queued payload was null.");
                return Task.CompletedTask;
            }
            
            _payloadQueue.Enqueue(payload);
            return Task.CompletedTask;
        }

        private Task ProcessCommandQueue()
        {
            if (_payloadQueue.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (!Amnesia.LastInstructionWasExecuted())
                return Task.CompletedTask;

            if(_payloadQueue.TryDequeue(out var payload) == false)
            {
                Console.WriteLine("A dequeue of a payload failed");
                return Task.CompletedTask;
            }

            ProcessPayload(payload);
            return Task.CompletedTask;
        }

        private async Task ProcessInstructionQueue()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                return;
            
            PayloadExtension extension;

            for(var i = 0; i < _instructionQueue.Count; i++)
            {
                if(_instructionQueue.TryDequeue(out extension) == false)
                {
                    Console.WriteLine("Failed to dequeue an instruction");
                    return;
                }

                if(DateTime.Now < extension.ExecuteAfterDateTime)
                {
                    _instructionQueue.Enqueue(extension);
                    continue;
                }

                await Amnesia.ExecuteAsync(extension.Angelcode);
                return;
            }
        }

        private void ProcessPayload(Payload payload)
        {
            foreach (var sequenceItem in payload.Sequence)
            {
                _instructionQueue.Enqueue(new PayloadExtension
                {
                    Angelcode = sequenceItem.AngelCode,
                    ExecuteAfterDateTime = DateTime.Now.Add(sequenceItem.Delay)
                });
            }
        }
    }
}
