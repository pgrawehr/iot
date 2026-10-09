using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DisplayControl.Views
{
    public class AisTargetControl : UserControl
    {
        public AisTargetControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
