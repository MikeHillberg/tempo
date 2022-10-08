using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class CsvMethodViewModel : MethodViewModel
    {
        public override MethodAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();
        public override bool IsProtected => throw new NotImplementedException();
        public override bool IsAbstract
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsFamily
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsFamilyAndAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsFamilyOrAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsFinal
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsPrivate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsPublic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsSealed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool GetIsSpecialName()
        {
            throw new NotImplementedException();
        }

        public override bool IsStatic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsVirtual
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MyMemberTypes MemberType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _name;
        public void SetName(string value) { _name = value; }
        public override string Name { get { return _name; } }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        List<ParameterViewModel> _parameters;
        public void AddParameter(CsvTypeViewModel type, string name)
        {
            if (_parameters == null)
                _parameters = new List<ParameterViewModel>();

            var parameter = new CsvParameterViewModel();
            parameter.SetParameterType(type);
            parameter.SetName(name);
            _parameters.Add(parameter);
        }

        protected override IList<ParameterViewModel> CreateParameters()
        {
            if (_parameters == null)
                _parameters = new List<ParameterViewModel>();
            return _parameters as IList<ParameterViewModel>;
        }

        CsvTypeViewModel _returnType;
        public void SetReturnType(CsvTypeViewModel value) { _returnType = value; }
        protected override TypeViewModel CreateReturnType()
        {
            return _returnType;
        }

        CsvTypeViewModel _declaringType;
        public void SetDeclaringType(CsvTypeViewModel type) { _declaringType = type; }
        protected override TypeViewModel GetDeclaringType() { return _declaringType; }
    }
}
