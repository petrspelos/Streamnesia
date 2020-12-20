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
        private ConcurrentQueue<TimedInstruction> _instructionQueue = new ConcurrentQueue<TimedInstruction>();

        public async Task StartCommandProcessingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ProcessCommandQueue();
                await ProcessInstructionQueue();
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }
        }

        public void AddPayload(Payload payload)
        {
            if(payload is null)
            {
                Console.WriteLine("A queued payload was null.");
                return;
            }
            
            _payloadQueue.Enqueue(payload);
        }

        private void ProcessCommandQueue()
        {
            if (_payloadQueue.Count == 0)
                return;

            if (!Amnesia.LastInstructionWasExecuted())
                return;

            if(_payloadQueue.TryDequeue(out var payload) == false)
            {
                Console.WriteLine("A dequeue of a payload failed");
                return;
            }

            ProcessPayload(payload);
        }

        private async Task ProcessInstructionQueue()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                return;
            
            TimedInstruction extension;

            for(var i = 0; i < _instructionQueue.Count; i++)
            {
                if(_instructionQueue.TryDequeue(out extension) == false)
                {
                    Console.WriteLine("Failed to dequeue an instruction");
                    return;
                }

                if(DateTime.Now >= extension.ExecuteAfterDateTime)
                {
                    await Amnesia.ExecuteAsync(extension.Angelcode);
                    return;
                }

                _instructionQueue.Enqueue(extension);
            }
        }

        private void ProcessPayload(Payload payload)
        {
            foreach (var sequenceItem in payload.Sequence)
            {
                _instructionQueue.Enqueue(new TimedInstruction
                {
                    Angelcode = sequenceItem.AngelCode,
                    ExecuteAfterDateTime = DateTime.Now.Add(sequenceItem.Delay)
                });
            }
        }
    }
}
