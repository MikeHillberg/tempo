using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    // This is a combination of a stack and a StringBuilder.
    // Push creates a new StringBuilder, Pop restores the old one, Current gives the current one.
    public class StackingStringBuilder
    {
        StringBuilder _current = new StringBuilder();
        public StringBuilder Current => _current;

        Stack<StringBuilder> _stack = new Stack<StringBuilder>();
        public void Push()
        {
            _stack.Push(_current);
            _current = new StringBuilder();
        }

        public void Pop()
        {
            _current = _stack.Pop();
        }
    }
}
