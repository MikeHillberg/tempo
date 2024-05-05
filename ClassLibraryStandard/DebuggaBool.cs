using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    /// <summary>
    /// A trivial/featureless wrapper for a bool to help debugging (you can set a breakpoint on the setter).
    /// Implicitly converts to/from a bool
    /// </summary>
    public struct DebuggaBool
    {
        bool _value;

        public DebuggaBool(bool value)
        {
            // Explicit syntax so that you can put a breakpoing on the true case
            if (value)
            {
                _value = true;
            }
            else
            {
                _value = false;
            }
        }

        public bool Value 
        { 
            get { return _value; } 
            set 
            {
                // Can't call the setter from the constructor, so go the other way around
                // This is a struct so it's a stack alloc
                _value = (DebuggaBool)value;
            }
        }

        public static implicit operator DebuggaBool(bool value)
        { 
            return new DebuggaBool(value); 
        }

        public static implicit operator bool(DebuggaBool value)
        {
            return value._value;
        }
    }
}
