using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DisplayControl
{
    public abstract class SensorValueSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SensorValueSource(string valueDescription)
        {
            ValueDescription = valueDescription;
        }

        public abstract object GenericValue
        {
            get;
        }

        public string ValueDescription
        {
            get;
        }

        protected void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return GenericValue.ToString();
        }

    }
}
