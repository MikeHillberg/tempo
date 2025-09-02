using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tempo;

namespace Tempo
{
    public class UwpTypeSet : TypeSet
    {
        public UwpTypeSet() : base(StaticName, false, null) { }
        public static string StaticName = "UWP";
        public override bool IsWinmd => true;
    }
}
