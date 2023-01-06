using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{

    /// <summary>
    /// A list with a key
    /// </summary>
    public class ItemsGroup : List<object>
    {
        public ItemsGroup(IEnumerable<object> list) : base(list) { }

        public string Key { get; set; }

        public override string ToString()
        {
            return Key;
        }
    }

}
