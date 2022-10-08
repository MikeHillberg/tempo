using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tempo;

namespace CommonLibrary
{
    public class ExportContext
    {
        public StringBuilder TypeTableText { get; private set; } = new StringBuilder();
        public StringBuilder MemberTableText { get; private set; } = new StringBuilder();

        public void WriteTypeEntry(string entry)
        {
            TypeTableText.Append(entry.Trim() + "|");
        }
        public void WriteTypeEntry(int entry)
        {
            TypeTableText.Append(entry.ToString() + "|");
        }
        public void WriteTypeEntry( IList<TypeViewModel> types)
        {
            if (types == null || types.Count == 0)
            {
                WriteTypeEntry(-1);
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var type in types)
                    sb.Append(type.ExportIndex + ",");

                WriteTypeEntry(sb.Remove(sb.Length - 1, 1).ToString());
            }
        }
    }



    public static class ImportExport
    {
        public static void FullExport(TypeSet typeSet)
        {
            var context = new ExportContext();

            int index = 0;
            foreach (var type in typeSet.Types)
            {
                type.ExportIndex = index++;
            }

            foreach (var type in typeSet.Types)
            {
                type.Serialize(context);
            }


        }

        //public static TypeViewModel ConvertFromClr( TypeViewModel type )
        //{
        //    switch( type.FullName)
        //    {
        //        //case "System.String";
        //    }
        //}

    }


    public class ImportedTypeSet
    {
        public IList<TypeViewModel> Types { get; private set; } = new List<TypeViewModel>();
    }

}
