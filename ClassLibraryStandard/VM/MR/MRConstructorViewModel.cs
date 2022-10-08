using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MRConstructorViewModel : ConstructorViewModel
    {
        MRTypeViewModel _declaringMRType;
        MrMethod _constructor;
        TypeSet _typeSet;

        public MRConstructorViewModel(MrMethod constructor, MRTypeViewModel declaringType, TypeSet typeSet)
        {
            _declaringMRType = declaringType;
            _constructor = constructor;
            _typeSet = typeSet;
        }

        public override MethodAttributes Attributes => _constructor.MethodDefinition.Attributes;

        public override bool IsFinal => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Final);

        public override bool IsPrivate => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Private);

        public override bool IsSealed => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Final);

        public override TypeViewModel ReturnType => _declaringMRType;

        public override bool IsStatic => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Static);

        public override bool IsPublic => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Public);

        public override bool IsVirtual => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Virtual);

        public override bool IsAbstract => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Abstract);

        string _name;
        public override string Name
        {
            get
            {
                if(_name == null)
                {
                    _name = _constructor.GetName();
                }
                return _name;
            }
        }

        public override bool IsFamily => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Family);

        public override bool IsFamilyOrAssembly => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.FamORAssem);

        public override bool IsFamilyAndAssembly => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.FamANDAssem);

        public override bool IsAssembly => _constructor.MethodDefinition.Attributes.HasFlag(MethodAttributes.Assembly);

        public override MyMemberTypes MemberType => MyMemberTypes.Constructor;

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return
                (from mrAttribute in _constructor.GetCustomAttributes()
                 select new MRCustomAttributeViewModel(mrAttribute, this.DeclaringType as MRTypeViewModel))
                 .ToList<CustomAttributeViewModel>();
        }

        protected override IList<ParameterViewModel> CreateParameters()
        {
            var parameterVMs = new List<ParameterViewModel>();
            foreach (var parameter in _constructor.GetParameters())
            {
                parameterVMs.Add(new MRParameterViewModel(parameter, _declaringMRType, _declaringMRType.TypeSet));
            }

            return parameterVMs;
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringMRType;
        }

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

        ParsedMethodAttributes? _memberModifiers;
        public override bool IsProtected => this.EnsureMemberModifiers().IsProtected;

        public override bool IsInternal => this.EnsureMemberModifiers().IsInternal;

        private ParsedMethodAttributes EnsureMemberModifiers()
        {
            if (_memberModifiers == null)
            {
                _memberModifiers = _constructor.GetParsedMethodAttributes();
            }
            return _memberModifiers.Value;
        }
    }
}
