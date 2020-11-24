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
        private ConcurrentStack<PayloadExtension> _instructionStack = new ConcurrentStack<PayloadExtension>();

        public async Task StartCommandProcessingAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessCommandQueue();
                await ProcessInstructionStack();
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }
        }

        public Task AddPayloadAsync(Payload payload)
        {
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
            {
                return Task.CompletedTask;
            }

            if(_payloadQueue.TryDequeue(out var payload) == false)
                return Task.CompletedTask;

            ProcessPayload(payload);
            return Task.CompletedTask;
        }

        private async Task ProcessInstructionStack()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                return;
            
            PayloadExtension extension;

            for(var i = 0; i < _instructionStack.Count; i++)
            {
                if(_instructionStack.TryPop(out extension) == false)
                    return;

                if(DateTime.Now < extension.ExecuteAfterDateTime)
                {
                    _instructionStack.Push(extension);
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
                _instructionStack.Push(new PayloadExtension
                {
                    Angelcode = sequenceItem.AngelCode,
                    ExecuteAfterDateTime = DateTime.Now.Add(sequenceItem.Delay)
                });
            }
        }
    }
}
