using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class CsvParameterViewModel : ParameterViewModel
    {
        public override bool IsAbstract
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();

        public override bool IsFamily
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsProtected => false;

        public override bool IsIn
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsOut
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

        public override bool IsVirtual
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _name;
        public void SetName( string name ) { _name = name; }
        public override string Name { get { return _name; } }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        CsvTypeViewModel _parameterType;
        public void SetParameterType( CsvTypeViewModel type ) { _parameterType = type; }
        protected override TypeViewModel CreateParameterType()
        {
            return _parameterType;
        }

        protected override TypeViewModel GetDeclaringType()
        {
            throw new NotImplementedException();
        }
    }
}
