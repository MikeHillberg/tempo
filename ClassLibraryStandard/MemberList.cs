using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MemberList : List<MemberViewModel>
    {
        public MemberList(IEnumerable<MemberViewModel> list) : base( list)
        { }

        public string Heading { get; set; }
    }


    public class GroupedTypeMembers : List<MemberList>
    {

    }
}
