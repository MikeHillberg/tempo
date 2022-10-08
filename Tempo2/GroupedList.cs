using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class GroupedList : List<object>
    {
    }

    public class ItemsGroup : List<object>
    {
        public ItemsGroup(IEnumerable<object> list) : base(list) { }

        public string Key { get; set; }

        public override string ToString()
        {
            return Key;
        }
    }

    public class ItemWrapper
    {
        public object Item { get; set; }
        public string ItemAsString { get; set; }

        public override string ToString()
        {
            return ItemAsString;
        }
    }

}
