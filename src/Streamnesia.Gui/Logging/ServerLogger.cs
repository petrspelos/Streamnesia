using Streamnesia.WebApp;
using System;

namespace Streamnesia.Gui.Logging
{
    internal class ServerLogger : IServerLogger
    {
        public Action<string>? OnLog;

        public void Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }
}
