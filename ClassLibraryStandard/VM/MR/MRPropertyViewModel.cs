using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MRPropertyViewModel : PropertyViewModel
    {
        MRTypeViewModel _declaringMRType;
        MrProperty _property;
        public MRPropertyViewModel(MRTypeViewModel declaringType, MrProperty property)
        {
            _declaringMRType = declaringType;
            _property = property;
        }

        public override bool CanRead => true; // bugbug: Why is this here? When is  it false?

        public override bool CanWrite
        {
            get
            {
                return CreateSetterMethodViewModel() != null; // Cached
            }
        }

        TypeViewModel _propertyType = null;
        public override TypeViewModel PropertyType
        {
            get
            {
                if (_propertyType == null)
                {
                    _propertyType = MRTypeViewModel.GetFromCache(_property.GetPropertyType(), DeclaringType.TypeSet);
                }
                return _propertyType;
            }
        }

        public override bool TryGetVMProperty(string key, out object value)
        {
            return MRTypeViewModel.TryGetVMPropertyHelper(this, key, out value);
        }

        public override string ToWhereString()
        {
            return PrettyName;
        }

        public override TypeViewModel ReturnType => PropertyType;

        string _name;
        public override string Name
        {
            get
            {
                if (_name == null)
                {
                    _name = _property.GetName();
                }
                return _name;
            }
        }

        public override string ToString()
        {
            return $"{this.DeclaringType.PrettyName}.{this.PrettyName}";
        }

        public override MyMemberTypes MemberType => MyMemberTypes.Property;

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return
                (from mrAttribute in _property.GetCustomAttributes()
                 select new MRCustomAttributeViewModel(mrAttribute, this.DeclaringType as MRTypeViewModel))
                 .ToList<CustomAttributeViewModel>();
        }

        MRMethodViewModel _getter = null;
        MRMethodViewModel _setter = null;
        protected override MethodViewModel CreateGetterMethodViewModel()
        {
            if (_getter == null && _setter == null)
            {
                if (_property.Getter != null)
                {
                    _getter = new MRMethodViewModel(DeclaringType as MRTypeViewModel, _property.Getter);
                }

                if (_property.Setter != null)
                {
                    _setter = new MRMethodViewModel(DeclaringType as MRTypeViewModel, _property.Setter);
                }

                Debug.Assert(_getter != null || _setter != null);
            }
            return _getter;
        }

        protected override IList<ParameterViewModel> CreateIndexParameters()
        {
            return null;
        }

        protected override MethodViewModel CreateSetterMethodViewModel()
        {
            CreateGetterMethodViewModel();
            return _setter;
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringMRType;
        }

        ParsedMethodAttributes? _memberModifiers;
        public override bool IsProtected => this.GetMemberModifiers().IsProtected;

        public override bool IsInternal => this.GetMemberModifiers().IsInternal;

        private ParsedMethodAttributes GetMemberModifiers()
        {
            if (_memberModifiers == null)
            {
                _memberModifiers = _property.GetParsedMethodAttributes();
            }

            return _memberModifiers.Value;
        }
    }
}
