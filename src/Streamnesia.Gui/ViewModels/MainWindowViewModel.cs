using Avalonia.Threading;
using ReactiveUI;
using Streamnesia.Gui.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Streamnesia.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        private CancellationTokenSource source = new();

        private ServerLogger logger = new();

        public MainWindowViewModel()
        {
            logger.OnLog = msg =>
            {
                Dispatcher.UIThread.InvokeAsync(() => ServerLog = $"{DateTime.Now.ToShortTimeString()} - {msg}\n{_serverLog}");
            };

            RunWebAppCommand = ReactiveCommand.Create(() =>
            {
                source = new();
                WebApp.Program.Start(source.Token, logger);
            });

            StopWebAppCommand = ReactiveCommand.Create(() =>
            {
                source.Cancel();
            });
        }

        public ICommand RunWebAppCommand { get; }

        public ICommand StopWebAppCommand { get; }

        private string _serverLog = "---";
        public string ServerLog
        {
            get => _serverLog;
            set => this.RaiseAndSetIfChanged(ref _serverLog, value);
        }
    }
}
