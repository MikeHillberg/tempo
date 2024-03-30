using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tempo;

namespace TempoPSProvider
{
    public class NamespaceViewModel 
        : PSHack // for PSNameHack
    {
        // This is the name that will show up in the PowerShell provider. See PSHack class for more info.
        public override string PSNameHack => $"[{Name}]";

        TypeSet _typeSet;

        public NamespaceViewModel(TypeSet typeSet, string ns)
        {
            _typeSet = typeSet;
            FullName = ns;
            Name = ns.Split('.').Last();
        }

        public string FullName { get; private set; }
        public string Name { get; private set; }

        public IEnumerable<NamespaceViewModel> Namespaces
        {
            get
            {
                var nodes = _typeSet.Namespaces;
                NamespaceTreeNode treeNode = null;
                var parts = FullName.Split('.');
                var partial = "";
                foreach(var part in parts)
                {
                    if(string.IsNullOrEmpty(partial))
                    {
                        partial = part;
                    }
                    else
                    {
                        partial = $"{partial}.{part}";
                    }

                    treeNode = null;
                    foreach(var node in nodes)
                    {
                        treeNode = node as NamespaceTreeNode;
                        if(treeNode.Key == partial)
                        {
                            nodes = treeNode.Items2;
                            break;
                        }
                        else
                        {
                            treeNode = null;
                        }
                    }

                    if(treeNode == null)
                    {
                        return null;
                    }
                }

                return from node in nodes select new NamespaceViewModel(_typeSet, (node as NamespaceTreeNode).Key);
            }
        }
        
        public IEnumerable<TypeViewModel> Types
        {
            get
            {
                var types = new List<TypeViewModel>();
                foreach(var type in TempoPSProvider.GetPublicTypes())
                {
                    if(type.Namespace == FullName)
                    {
                        types.Add(type);
                    }
                }

                return types;
            }
        }

    }
}
