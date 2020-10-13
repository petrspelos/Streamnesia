using System.Collections.Generic;
using System.Threading.Tasks;

namespace Streamnesia.Payloads
{
    public interface IPayloadLoader
    {
        Task<IEnumerable<Payload>> GetPayloadsAsync();
    }
}
