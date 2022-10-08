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
    public class MRFieldViewModel : FieldViewModel
    {
        MrField _field;
        MRTypeViewModel _declaringType;

        public MRFieldViewModel(MrField field, MRTypeViewModel declaringType)
        {
            _field = field;
            _declaringType = declaringType;
            Debug.Assert(field.GetName() != "value__");
        }

        public override bool IsLiteral => _field.Definition.Attributes.HasFlag(FieldAttributes.Literal);

        public override bool IsAbstract => false;

        object _rawConstantValue;
        public override object RawConstantValue
        {
            get
            {
                if(_rawConstantValue == null)
                {
                    _rawConstantValue = _field.GetConstantValue(out var constantType);
                }

                return _rawConstantValue;
            }
        }

        public override bool IsPrivate => _field.Definition.Attributes.HasFlag(FieldAttributes.Private);

        public override FieldAttributes Attributes => _field.Definition.Attributes;

        public override bool IsInitOnly => _field.Definition.Attributes.HasFlag(FieldAttributes.InitOnly);

        MRTypeViewModel _fieldType;
        public override TypeViewModel FieldType
        {
            get
            {
                if(_fieldType == null)
                {
                    _fieldType = MRTypeViewModel.GetFromCache(_field.GetFieldType(), this.DeclaringType.TypeSet) as MRTypeViewModel;
                }

                return _fieldType;
            }
        }

        public override TypeViewModel ReturnType => FieldType;

        public override bool IsPublic => _field.Definition.Attributes.HasFlag(FieldAttributes.Public);

        public override string Name => _field.GetName();

        public override bool IsFamily => _field.Definition.Attributes.HasFlag(FieldAttributes.Family);

        public override bool IsFamilyOrAssembly => _field.Definition.Attributes.HasFlag(FieldAttributes.FamORAssem);

        public override bool IsFamilyAndAssembly => _field.Definition.Attributes.HasFlag(FieldAttributes.FamANDAssem);

        public override bool IsAssembly => _field.Definition.Attributes.HasFlag(FieldAttributes.Assembly);

        public override bool GetIsSpecialName()
        {
            return _field.Definition.Attributes.HasFlag(FieldAttributes.SpecialName);
        }

        protected override bool CheckIsStatic()
        {
            return _field.Definition.Attributes.HasFlag(FieldAttributes.Static);
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

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return _field.GetCustomAttributes()
                            .Select(a => new MRCustomAttributeViewModel(a, this.DeclaringType as MRTypeViewModel) as CustomAttributeViewModel)
                            .ToList();

        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringType;
        }

        protected override TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel)
        {
            return MRTypeViewModel.GetFromCache((typeViewModel as MRTypeViewModel).Type, typeViewModel.TypeSet);
        }

        override public bool IsCompilerGenerated
        {
            get
            {
                var attributes = CustomAttributes;
                foreach (var attribute in attributes)
                {
                    if (attribute.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        ParsedFieldAttributes? _memberModifiers;
        public override bool IsProtected => this.EnsureMemberModifiers().IsProtected;
        public override bool IsInternal => this.EnsureMemberModifiers().IsInternal;

        private ParsedFieldAttributes EnsureMemberModifiers()
        {
            if (_memberModifiers == null)
            {
                _memberModifiers = _field.GetFieldModifiers();
            }
            return _memberModifiers.Value;
        }
    }
}
