using Microsoft.Extensions.DependencyInjection;

namespace Streamnesia.WebApp
{
    public interface IGuiServices
    {
        void Configure(IServiceCollection services);
    }
}
