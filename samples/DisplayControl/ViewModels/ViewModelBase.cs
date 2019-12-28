using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class ViewModelBase : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }
        
        public ViewModelBase()
        {
            Activator = new ViewModelActivator();
        }
    }
}
