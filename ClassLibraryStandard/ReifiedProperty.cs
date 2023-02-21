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
    // Property value with built-in change notification
    public class ReifiedProperty<T> : INotifyPropertyChanged
    {
        T _value;
        private Action _changeCallback;

        public ReifiedProperty() {}

        public ReifiedProperty(T value) { this.Value = value; }

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
            _value = value;

            if (!quiet)
            {
                RaisePropertyChanged();
            }
        }

        public void RaisePropertyChanged()
        {
            PropertyChanged?.Invoke(this, _args);
            _changeCallback?.Invoke();
        }

        static PropertyChangedEventArgs _args = new PropertyChangedEventArgs(nameof(Value));

        public event PropertyChangedEventHandler PropertyChanged;

        static public implicit operator T(ReifiedProperty<T> r) => r.Value;

        public override string ToString()
        {
            if(Value != null)
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
