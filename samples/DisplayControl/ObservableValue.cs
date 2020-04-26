using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace DisplayControl
{
    public class ObservableValue<T> : SensorValueSource, IObservable<T>
    {
        private T m_value;
        private string m_valueFormatter;

        private List<IObserver<T>> m_observers;

        public ObservableValue(string valueDescription, string unit, T value)
            : base(valueDescription, unit)
        {
            m_value = value;

            if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
            {
                m_valueFormatter = "{0:F3}";
            }
            else
            {
                m_valueFormatter = "{0}";
            }
            m_observers = new List<IObserver<T>>();
        }

        public ObservableValue(string valueDescription, string unit)
            : base(valueDescription, unit)
        {
            m_value = default(T);

            if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
            {
                m_valueFormatter = "{0:F3}";
            }
            else
            {
                m_valueFormatter = "{0}";
            }
            m_observers = new List<IObserver<T>>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!m_observers.Contains(observer))
            {
                m_observers.Add(observer);
            }
            return new Unsubscriber(m_observers, observer);
        }

        public T Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;
                foreach (var observer in m_observers)
                {
                    observer.OnNext(m_value);
                }
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(GenericValue));
                ValueChanged();
            }
        }

        internal string ValueFormatter
        {
            get
            {
                return m_valueFormatter;
            }
            set
            {
                m_valueFormatter = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(GenericValue));
                ValueChanged();
            }
        }

        public override object GenericValue
        {
            get
            {
                return Value;
            }
        }

        public override string ValueAsString
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, ValueFormatter, Value);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<T>> _observers;
            private IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}
