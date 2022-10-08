using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MRParameterViewModel : ParameterViewModel
    {
        MrParameter _parameter;
        MRTypeViewModel _declaringType;
        TypeSet _typeSet;

        public MRParameterViewModel(MrParameter parameter, MRTypeViewModel declaringType, TypeSet typeSet)
        {
            _parameter = parameter;
            _declaringType = declaringType;
            _typeSet = typeSet;
        }

        public override bool IsProtected => false;
        public override bool IsIn => _parameter.Attributes.HasFlag(ParameterAttributes.In);

        public override bool IsInternal => false;
        public override bool IsOut => _parameter.Attributes.HasFlag(ParameterAttributes.Out);

        public override bool IsPublic => true;

        public override bool IsVirtual => false;

        public override bool IsAbstract => false;

        string _name;
        public override string Name
        {
            get
            {
                if(_name == null)
                {
                    _name = _parameter.GetParameterName();
                }
                return _name;
            }
        }

        public override string ToWhereString()
        {
            return $"{ParameterType.PrettyFullName} {PrettyName}";
        }

        public override bool IsFamily => false;

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return new List<CustomAttributeViewModel>();
        }

        protected override TypeViewModel CreateParameterType()
        {
            return MRTypeViewModel.GetFromCache(_parameter.GetParameterType(), _typeSet);
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringType;
        }
    }
}
