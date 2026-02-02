using System;
using System.Collections.Generic;
using System.Text;

namespace Tempo
{
    public class OneTimeCallbacks
    {
        List<Action> _callbacks = null;

        public void Add(Action callback)
        {
            if (_callbacks == null)
            {
                _callbacks = new List<Action>();
            }
            _callbacks.Add(callback);
        }

        public void Invoke()
        {
            if (_callbacks != null)
            {
                foreach (var callback in _callbacks)
                {
                    callback();
                }
            }

            _callbacks = null;
        }
    }
}
