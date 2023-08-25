using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DisplayControl.Views
{
    public class SensorValueControl : UserControl
    {
        public SensorValueControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
