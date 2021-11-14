using Microsoft.Extensions.DependencyInjection;
using Streamnesia.Gui.Logging;
using Streamnesia.WebApp;

namespace Streamnesia.Gui
{
    internal class AvaloniaServices : IGuiServices
    {
        public ServerLogger Logger { get; private set; }

        public AvaloniaServices()
        {
            Logger = new();
        }

        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IServerLogger>(Logger);
        }
    }
}
