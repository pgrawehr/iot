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
        private static GpioController s_gpioController;
        public App()
        {
        }

        public static void SetGpioController(GpioController gpioController)
        {
            s_gpioController = gpioController;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DataContainer dc = new DataContainer(s_gpioController);
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
