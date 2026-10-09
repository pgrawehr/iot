using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DisplayControl.ViewModels;
using DisplayControl.Views;
using System.Device.Gpio;

namespace DisplayControl
{
    public class App : Application
    {
        public App()
        {
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DataContainer dc = new DataContainer();
                dc.Initialize();
                var vm = new MainWindowViewModel(dc);
                desktop.MainWindow = new MainWindow
                {
                    ViewModel = vm,
                    DataContext = vm
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
