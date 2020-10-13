using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Streamnesia.Payloads.Entities;

namespace Streamnesia.Payloads
{
    public class LocalPayloadLoader : IPayloadLoader
    {
        public string payloadsDirectory = "payloads";
        
        public Task<IEnumerable<Payload>> GetPayloadsAsync()
        {
            var payloads = GetLocalPayloads();

            return Task.FromResult(payloads);
        }

        private IEnumerable<Payload> GetLocalPayloads()
        {
            var json = File.ReadAllText(Path.Combine(payloadsDirectory, "payloads.json"));
            var payloadDefinitions = JsonConvert.DeserializeObject<IEnumerable<PayloadModel>>(json);

            return payloadDefinitions.Select(p => new Payload
            {
                Name = p.Name,
                Angelcode = GetPayloadFileText(p.File),
                PayloadDuration = p.Duration,
                ReverseAngelcode = GetPayloadFileText(p.Antidote)
            });
        }

        private string GetPayloadFileText(string file)
        {
            if(file is null)
                return null;

            return File.ReadAllText(Path.Combine(payloadsDirectory, file));
        }

        private string FormatPayloadName(string name)
            => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Replace("-", " ").ToLower());
    }
}
