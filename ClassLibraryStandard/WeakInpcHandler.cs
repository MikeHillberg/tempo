using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tempo
{
    public class WeakInpcHandler
    {
        WeakReference _weakTarget;
        Action<object, PropertyChangedEventArgs> _handler;

        public WeakInpcHandler(object target, Action<object, PropertyChangedEventArgs> handler)
        {
            _weakTarget = new WeakReference(target);
            _handler = handler;
        }

        public void Invoke(object sender, PropertyChangedEventArgs e)
        {
            var target = _weakTarget.Target;
            if (target != null)
                _handler(target, e);
            else
            {
                if (Remove != null)
                    Remove(this, null);
            }

        }

        public void Cleanup()
        {
            var target = _weakTarget.Target;
            if (target != null && Remove != null)
            {
                Remove(this, null);
            }
        }

        public EventHandler Remove;
    }

}
