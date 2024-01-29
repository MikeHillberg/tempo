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
    //public class WeakEventHandlerOld<THandler> where THandler : Delegate
    //{
    //    ConditionalWeakTable<object, List<THandler>> _delegateAndTargets
    //        = new ConditionalWeakTable<object, List<THandler>>();

    //    List<WeakReference<THandler>> _delegates
    //        = new List<WeakReference<THandler>>();

    //    public void Add(THandler value)
    //    {
    //        List<THandler> handlers;
    //        if (_delegateAndTargets.TryGetValue(value.Target, out handlers))
    //        {
    //            handlers.Add(value);
    //        }
    //        else
    //        {
    //            handlers = new List<THandler>() { value };
    //            _delegateAndTargets.Add(value.Target, handlers);
    //        }
    //        _delegates.Add(new WeakReference<THandler>(value));
    //    }

    //    public void Remove(THandler value)
    //    {
    //        Debugger.Break();
    //        if (_delegateAndTargets.TryGetValue(value.Target, out var handlers))
    //        {
    //            handlers.Remove(value);
    //        }
    //        else
    //        {
    //            Debug.Assert(false);
    //        }

    //        foreach (var wr in _delegates)
    //        {
    //            if (wr.TryGetTarget(out var handler))
    //            {
    //                if (handler == value)
    //                {
    //                    _delegates.Remove(wr);
    //                    break;
    //                }
    //            }
    //        }
    //    }

    //    public void Raise(Action handler)
    //    {
    //        var copy = _delegates.ToArray();
    //        foreach (var wr in copy)
    //        {
    //            if (wr.TryGetTarget(out var target))
    //            {
    //                handler();
    //            }
    //            else
    //            {
    //                _delegates.Remove(wr);
    //            }
    //        }
    //    }
    //}
}
