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
                return String.Format(CultureInfo.CurrentCulture, "{0:F3} {1}", Value, Unit);
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
