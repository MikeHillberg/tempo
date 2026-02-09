using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    /// <summary>
    /// Property value with built-in change notification
    /// This allows you to provide change notifications for a static property/field
    /// In code there's an overload to get the T value,
    /// but there's no overloaded assignment operator in C#, so you need to do ".Value" (same for x:Bind)
    /// </summary>
    public class ReifiedProperty<T> : INotifyPropertyChanged
    {
        T _value = default;
        private Action _changeCallback;
        private PropertyChangedEventHandler _propertyChanged;

        bool _oneChangeNotification = false;
        bool _hasChanged = false;

        public ReifiedProperty() { }

        /// <summary>
        /// oneChangeNotification means that after the first change the callback is cleared, avoiding leaks
        /// </summary>
        /// <param name="value"></param>
        /// <param name="oneChangeNotification"></param>
        public ReifiedProperty(T value, bool oneChangeNotification = false)
        {
            _value = value;
            this._oneChangeNotification = oneChangeNotification;
        }

        public ReifiedProperty(T value, Action changeCallback)
        {
            this.Value = value;
            this._changeCallback = changeCallback;
        }

        public T Value
        {
            get { return _value; }
            set
            {
                // Update _value and raise a change notification
                SetValue(value, quiet: false);
            }
        }

        /// <summary>
        /// Set the value but don't raise a change notification
        /// </summary>
        public void SetValueQuietly(T value)
        {
            SetValue(value, quiet: true);
        }

        void SetValue(T value, bool quiet)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }
            _value = value;
            _hasChanged = true;
            
            if (!quiet)
            {
                RaisePropertyChanged();
            }
        }

        public void RaisePropertyChanged()
        {
            //Debug.Assert(!_hasChanged || !_oneChangeNotification);

            _propertyChanged?.Invoke(this, _args);
            _changeCallback?.Invoke();

            if (_oneChangeNotification)
            {
                _propertyChanged = null;
                _changeCallback = null;
            }
        }

        static PropertyChangedEventArgs _args = new PropertyChangedEventArgs(nameof(Value));

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (_oneChangeNotification && _hasChanged)
                {
                    // Ignore any further subscriptions
                    return;
                }
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }

        static public implicit operator T(ReifiedProperty<T> r) => r.Value;

        public override string ToString()
        {
            if (Value != null)
            {
                return Value.ToString();
            }
            else
            {
                return "(null)";
            }
        }
    }
}
