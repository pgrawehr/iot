using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DisplayControl.ViewModels;

namespace DisplayControl
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            var type = data.GetType();
            var viewName = type.FullName.Replace("ViewModel", "View");
            var viewType = Type.GetType(viewName);

            // Should actually place that in a folder named "Controls", but the designer seems to have problems with that
            var ns = type.Namespace.Replace("ViewModel", "View");
            var controlName = ns + "." + type.Name.Replace("ViewModel", "Control");
            var controlType = Type.GetType(controlName);

            if (viewType != null)
            {
                return (Control)Activator.CreateInstance(viewType);
            }
            else if (controlType != null)
            {
                return (Control)Activator.CreateInstance(controlType);
            }
            else
            {
                return new TextBlock { Text = $"View for {data.GetType().FullName} not found."};
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
