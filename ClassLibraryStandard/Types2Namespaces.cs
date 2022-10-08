using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Tempo
{
    public class NamespaceTreeNode
    {
        public string Key { get; set; }
        public string Leaf { get; internal set; }
        public IEnumerable<Object> Items2 { get; internal set; }
    }


    public static class Types2Namespaces
    {
        public static IEnumerable<object> Convert(IEnumerable<TypeViewModel> types)
        {
            //var namespaces = (from t in types
            //                  select t.Namespace).Distinct().OrderBy(t => t).ToList();

            var namespaces = GetFlatList(types);
            var r = Convert(namespaces, 0);
            var r2 = r.ToList();
            return r2;

        }

        public static IList<string> GetFlatList(IEnumerable<TypeViewModel> types)
        {
            var namespaces = (from t in types
                              select t.Namespace).Distinct().OrderBy(t => t).ToList();

            return namespaces;
        }

        static IEnumerable<object> Convert(IEnumerable<string> namespaces, int level)
        {
            var list = from ns in namespaces
                       where !string.IsNullOrEmpty(ns)
                       group ns by GetNamespacePart(ns, level) into g
                       where g.Key != null
                       where g.Count() != 0
                       select  new NamespaceTreeNode
                       { 
                           Key = g.Key, 
                           Leaf = GetLeaf(g.Key),
                           Items2 = Convert( g, level+1).Distinct()
                       };

            var list2 = list.ToList();

            return list2;
        }

        static string GetLeaf( string ns )
        {
            var index = ns.LastIndexOf('.');
            if (index == -1)
                return ns;
            else
                return ns.Substring(index+1);
        }


        // Get a part out of a namespace. E.g. "foo.bar.baz",1 returns "bar"
        static string GetNamespacePart(string ns, int level)
        {
            int startIndex = 0;
            int prevStartIndex = 0;

            if(string.IsNullOrEmpty(ns))
            {
                // The type is in the global namespace
                return null;
            }

            while (true)
            {
                var index = ns.IndexOf('.', startIndex + 1);
                if (index < 0)
                {
                    if (level == 0)
                    {
                        prevStartIndex = startIndex;
                        startIndex = -1;
                    }
                    else
                        ns = null;

                    break;
                }

                prevStartIndex = startIndex;
                startIndex = index+1;

                level--;

                if (level < 0)
                    break;
            }

            if (ns != null)
            {
                if (startIndex >= 0)
                    ns = ns.Substring(0, startIndex-1);//startIndex - prevStartIndex - 1);
                else
                    ns = ns.Substring(0);
            }

            return ns;
        }
    }
}
