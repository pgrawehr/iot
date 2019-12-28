using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace DisplayControl.ViewModels
{
    public class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
    {
        public ViewModelActivator Activator { get; }
        
        public ViewModelBase()
        {
            Activator = new ViewModelActivator();
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
