using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DisplayControl
{
    public abstract class SensorValueSource : INotifyPropertyChanged
    {
        private WarningLevel _warningLevel;

        public event PropertyChangedEventHandler PropertyChanged;

        public SensorValueSource(string valueDescription, string unit)
        {
            ValueDescription = valueDescription;
            Unit = unit;
            SuppressWarnings = false;
        }

        public abstract object GenericValue
        {
            get;
        }

        public string ValueDescription
        {
            get;
        }

        public virtual string ValueAsString
        {
            get
            {
                return ToString();
            }
        }

        public WarningLevel WarningLevel
        {
            get
            {
                return _warningLevel;
            }
            set
            {
                _warningLevel = value;
                NotifyPropertyChanged();
            }
        }

        public bool SuppressWarnings
        {
            get;
            set;
        }

        public string Unit
        {
            get;
            protected set;
        }

        protected void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var tempEvent = PropertyChanged;
            if (tempEvent != null)
            {
                tempEvent(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void ValueChanged()
        {
        }

        public override string ToString()
        {
            return GenericValue.ToString();
        }

    }
}
