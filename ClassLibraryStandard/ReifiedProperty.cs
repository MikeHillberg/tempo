using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
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
                _value = value;
                PropertyChanged?.Invoke(this, _args);
                _changeCallback?.Invoke();
            }
        }

        static PropertyChangedEventArgs _args = new PropertyChangedEventArgs("");
        public event PropertyChangedEventHandler PropertyChanged;

        static public implicit operator T(ReifiedProperty<T> r) => r.Value;
    }
}
