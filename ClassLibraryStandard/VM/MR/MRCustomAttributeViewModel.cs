using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MRCustomAttributeViewModel : CustomAttributeViewModel
    {
        MrCustomAttribute _attribute;
        MRTypeViewModel _targetType;

        public MRCustomAttributeViewModel(MrCustomAttribute attribute, MRTypeViewModel targetType)
        {
            _attribute = attribute;
            _targetType = targetType;
        }

        public override string ToString()
        {
            return $"MRCustomAttributeViewModel: {Name}";
        }

        void EnsureNameAndNamespace()
        {
            if (_name != null)
                return;

            _attribute.GetNameAndNamespace(out _name, out var ns);
            _fullName = $"{ns}.{_name}";
        }

        string _name;
        string _fullName;

        public override string Name
        {
            get
            {
                EnsureNameAndNamespace();
                return _name;
            }
        }

        public override string FullName
        {
            get
            {
                EnsureNameAndNamespace();
                return _fullName;
            }
        }

        bool _ensuredArguments = false;
        void EnsureArguments()
        {
            if(_ensuredArguments)
            {
                return;
            }
            _ensuredArguments = true;

            _namedArguments = new List<CustomAttributeNamedArgumentViewModel>();
            _constructorArguments = new List<CustomAttributeTypedArgumentViewModel>();

            _attribute.GetArguments(out var constructorArguments, out var namedArguments);

            if (constructorArguments != null)
            {
                foreach( var arg in constructorArguments)
                {
                    var value = arg.Value;
                    if(arg.Type.GetFullName() == "System.Type")
                    {
                        value = MRTypeViewModel.GetFromCache(arg.Value as MrType, _targetType.TypeSet);
                    }

                    _constructorArguments.Add(new MRCustomAttributeTypedArgumentViewModel(
                                                                MRTypeViewModel.GetFromCache(arg.Type, _targetType.TypeSet) as MRTypeViewModel,
                                                                value)
                                                                as CustomAttributeTypedArgumentViewModel);
                }
            }

            if(namedArguments != null)
            {
                foreach (var arg in namedArguments)
                {
                    var value = arg.Value;

                    _namedArguments.Add(new MRCustomAttributeNamedArgumentViewModel(
                                                                arg.Name,
                                                                MRTypeViewModel.GetFromCache(arg.Type, _targetType.TypeSet) as MRTypeViewModel,
                                                                value)
                                                                as CustomAttributeNamedArgumentViewModel);
                }
            }
        }

        IList<CustomAttributeTypedArgumentViewModel> _constructorArguments;
        public override IList<CustomAttributeTypedArgumentViewModel> ConstructorArguments
        {
            get
            {
                EnsureArguments();
                return _constructorArguments;
            }
        }

        IList<CustomAttributeNamedArgumentViewModel> _namedArguments;
        public override IList<CustomAttributeNamedArgumentViewModel> NamedArguments
        {
            get
            {
                EnsureArguments();
                return _namedArguments;
            }
        }
    }


    public class MRCustomAttributeTypedArgumentViewModel : CustomAttributeTypedArgumentViewModel
    {
        object _value;
        MRTypeViewModel _type;
        public MRCustomAttributeTypedArgumentViewModel(MRTypeViewModel type, object value)
        {
            _value = value;
            _type = type;
        }
        public override object Value => _value;

        public override TypeViewModel ArgumentType => _type;
    }

    public class MRCustomAttributeNamedArgumentViewModel : CustomAttributeNamedArgumentViewModel
    {
        string _name;
        CustomAttributeTypedArgumentViewModel _typedArgumentViewModel;

        public MRCustomAttributeNamedArgumentViewModel(string name, MRTypeViewModel valueType, object value)
        {
            _name = name;
            _typedArgumentViewModel = new MRCustomAttributeTypedArgumentViewModel(valueType, value);
        }

        public override string MemberName => _name;

        public override CustomAttributeTypedArgumentViewModel TypedValue => _typedArgumentViewModel;
    }

}
