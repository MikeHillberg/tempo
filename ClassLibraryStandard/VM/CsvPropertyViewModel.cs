using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class CsvPropertyViewModel : PropertyViewModel
    {
        public override bool CanRead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();
        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsProtected => throw new NotImplementedException();

        public override MyMemberTypes MemberType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name {  get { return _name; } }
        string _name;
        public void SetName( string value ) { _name = value; }

        public override TypeViewModel PropertyType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeViewModel ReturnType {  get { return _returnType; } }
        CsvTypeViewModel _returnType;
        string _returnTypeString;
        public void SetReturnTypeString( string type)
        {
            _returnTypeString = type;
        }
        public void SetReturnType( CsvTypeViewModel type )
        {
            _returnType = type;
            _returnTypeString = null;
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        protected override MethodViewModel CreateGetterMethodViewModel()
        {
            throw new NotImplementedException();
        }

        protected override IList<ParameterViewModel> CreateIndexParameters()
        {
            throw new NotImplementedException();
        }

        protected override MethodViewModel CreateSetterMethodViewModel()
        {
            throw new NotImplementedException();
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _typeVM;
        }
        CsvTypeViewModel _typeVM;
        public void SetDeclaringType( CsvTypeViewModel typeVM) { _typeVM = typeVM; }
    }
}
