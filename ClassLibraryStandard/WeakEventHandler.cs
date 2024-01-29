using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class WeakEventHandler<THandler> where THandler : Delegate
    {
        ConditionalWeakTable<object, List<THandler>> _delegateAndTargets
            = new ConditionalWeakTable<object, List<THandler>>();

        List<WeakReference<THandler>> _weakDelegates
            = new List<WeakReference<THandler>>();

        public WeakEventHandler()
        {

        }

        public void Add(THandler value)
        {
            List<THandler> handlers;
            if (_delegateAndTargets.TryGetValue(value.Target, out handlers))
            {
                handlers.Add(value);
            }
            else
            {
                handlers = new List<THandler>() { value };
                _delegateAndTargets.Add(value.Target, handlers);
            }
            _weakDelegates.Add(new WeakReference<THandler>(value));
        }

        public void Remove(THandler value)
        {
            Debugger.Break();
            if (_delegateAndTargets.TryGetValue(value.Target, out var handlers))
            {
                handlers.Remove(value);
            }
            else
            {
                Debug.Assert(false);
            }

            foreach (var wr in _weakDelegates)
            {
                if (wr.TryGetTarget(out var handler))
                {
                    if (handler == value)
                    {
                        _weakDelegates.Remove(wr);
                        break;
                    }
                }
            }
        }

        public void Raise(Action<THandler> invoker)
        {
            var copy = _weakDelegates.ToArray();
            foreach (var wr in copy)
            {
                if (wr.TryGetTarget(out var handler))
                {
                    invoker(handler);
                }
                else
                {
                    _weakDelegates.Remove(wr);
                }
            }
        }
    }
}
