using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MRMethodViewModel : MethodViewModel
    {
        MRTypeViewModel _declaringMRType;
        MrMethod _method;

        public MRMethodViewModel(MRTypeViewModel declaringType, MrMethod method)
        {
            _declaringMRType = declaringType;
            _method = method;
            Debug.Assert(method != null);
        }

        public override bool IsFinal => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Final);

        public override MethodAttributes Attributes => _method.MethodDefinition.Attributes;

        public override string ToString()
        {
            return $"{this.DeclaringType.PrettyName}.{this.PrettyName}";
        }

        public override bool TryGetVMProperty(string key, out object value)
        {
            return MRTypeViewModel.TryGetVMPropertyHelper(this, key, out value);
        }

        public override string ToWhereString()
        {
            return PrettyName;
        }

        public override bool IsPrivate => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Private);

        public override bool IsSealed => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Final);

        public override bool IsStatic => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Static);

        public override bool IsPublic => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Public);

        public override bool IsVirtual => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Virtual);

        public override bool IsAbstract => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Abstract);

        string _name;
        public override string Name
        {
            get
            {
                if(_name == null)
                {
                    _name = _method.GetName();
                }

                return _name;
            }
        }
        //public bool IsExplicitImplementation()
        //{
        //    if (!this.IsPrivate)
        //        return false;

        //    if (this.Name.Substring(1).Contains('.'))
        //    {
        //        var interfaceName = this.Name.Substring(0, this.Name.LastIndexOf('.'));
        //        var i = this.DeclaringType.GetInterface(interfaceName);
        //        if (i == null || i.IsNotPublic)
        //            return false;
        //        return true; // explicit interface 
        //    }

        //    return false;
        //}

        public override bool IsFamily => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Family);

        public override bool IsFamilyOrAssembly => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.FamORAssem);

        public override bool IsFamilyAndAssembly => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.FamANDAssem);

        public override bool IsAssembly => _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.Assembly);

        public override MyMemberTypes MemberType => MyMemberTypes.Method;

        public override bool GetIsSpecialName()
        {
            return _method.MethodDefinition.Attributes.HasFlag(MethodAttributes.SpecialName);
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return 
                (from mrAttribute in _method.GetCustomAttributes()
                 select new MRCustomAttributeViewModel(mrAttribute, this.DeclaringType as MRTypeViewModel))
                 .ToList<CustomAttributeViewModel>();
        }

        protected override IList<ParameterViewModel> CreateParameters()
        {
            var parameterVMs = new List<ParameterViewModel>();

            // Can be null for fake types
            if (_method != null)
            {
                foreach (var parameter in _method.GetParameters())
                {
                    parameterVMs.Add(new MRParameterViewModel(parameter, _declaringMRType, _declaringMRType.TypeSet));
                }
            }

            return parameterVMs;
        }

        protected override TypeViewModel CreateReturnType()
        {
            return MRTypeViewModel.GetFromCache(_method.ReturnType, DeclaringType.TypeSet);
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringMRType;
        }

        ParsedMethodAttributes? _memberModifiers;
        public override bool IsProtected => GetMemberModifiers().IsProtected;
        public override bool IsInternal => GetMemberModifiers().IsInternal;

        private ParsedMethodAttributes GetMemberModifiers()
        {
            if (_memberModifiers == null)
            {
                _memberModifiers = _method.GetParsedMethodAttributes();
            }
            return _memberModifiers.Value;
        }
    }
}
