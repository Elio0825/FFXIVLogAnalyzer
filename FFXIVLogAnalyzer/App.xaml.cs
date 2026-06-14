using System.IO;
using System.Windows;

namespace FFXIVLogAnalyzer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (s, ev) =>
        {
            File.WriteAllText("crash.log", ev.Exception.ToString());
            MessageBox.Show(ev.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ev.Handled = true;
        };
    }
}

