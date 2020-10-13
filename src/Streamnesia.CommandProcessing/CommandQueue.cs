using System;
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
        private ICollection<PayloadAntidote> _antidoteQueue = new List<PayloadAntidote>();

        public async Task StartCommandProcessingAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await ProcessCommandQueue();
                await ProcessAntidoteQueue();
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
            if(_payloadQueue.Count == 0)
                return Task.CompletedTask;
            
            if(!Amnesia.LastInstructionWasExecuted())
                return Task.CompletedTask;

            var payload = _payloadQueue.Dequeue();

            return ProcessPayload(payload);
        }

        private async Task ProcessAntidoteQueue()
        {
            if(!Amnesia.LastInstructionWasExecuted())
                return;
            
            PayloadAntidote antidote;

            for(var i = 0; i < _antidoteQueue.Count; i++)
            {
                antidote = _antidoteQueue.ElementAt(i);
                if(DateTime.Now < antidote.ExecuteAfterDateTime)
                    continue;
                
                await Amnesia.ExecuteAsync(antidote.Angelcode);
                _antidoteQueue.Remove(antidote);
                return;
            }

            return;
        }

        private async Task ProcessPayload(Payload payload)
        {
            await Amnesia.ExecuteAsync(payload.Angelcode);

            if(string.IsNullOrWhiteSpace(payload.ReverseAngelcode) || payload.PayloadDuration is null)
                return;

            _antidoteQueue.Add(new PayloadAntidote
            {
                Angelcode = payload.ReverseAngelcode,
                ExecuteAfterDateTime = DateTime.Now.Add(payload.PayloadDuration.Value)
            });
        }
    }
}
