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

        private List<IObserver<T>> m_observers;

        public ObservableValue(string valueDescription, string unit, T value)
            : base(valueDescription, unit)
        {
            m_value = value;
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
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0:F3}", Value);
                }
                else if (typeof(T) == typeof(int) || typeof(T) == typeof(short))
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0}", Value);
                }
                else
                {
                    return Value.ToString();
                }
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
