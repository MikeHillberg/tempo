using System;
using System.Collections.Generic;
using System.Text;

namespace Tempo
{
    /// <summary>
    /// Simple class with Name and Value properties
    /// </summary>
    public class NameValue
    {
        public NameValue() { }

        public NameValue(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }
    }

}
