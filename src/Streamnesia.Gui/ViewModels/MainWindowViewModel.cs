using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Threading;
using System.Windows.Input;

namespace Streamnesia.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private CancellationTokenSource source = new();

        private readonly AvaloniaServices _services = new();

        public MainWindowViewModel()
        {
            _services.Logger.OnLog = msg =>
            {
                Dispatcher.UIThread.InvokeAsync(() => ServerLog = $"{DateTime.Now.ToShortTimeString()} - {msg}\n{_serverLog}");
            };

            RunWebAppCommand = ReactiveCommand.Create(() =>
            {
                source = new();
                WebApp.Program.Start(source.Token, _services);
            });

            StopWebAppCommand = ReactiveCommand.Create(() =>
            {
                source.Cancel();
            });
        }

        public ICommand RunWebAppCommand { get; }

        public ICommand StopWebAppCommand { get; }

        private string _serverLog = string.Empty;
        public string ServerLog
        {
            get => _serverLog;
            set => this.RaiseAndSetIfChanged(ref _serverLog, value);
        }
    }
}
