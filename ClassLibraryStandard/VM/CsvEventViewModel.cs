using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class CsvEventViewModel : EventViewModel
    {
        public override MyMemberTypes MemberType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();

        string _name;
        public void SetName( string value ) { _name = value; }
        public override string Name {  get { return _name; } }

        CsvTypeViewModel _returnType;
        public void SetReturnType(CsvTypeViewModel value) { _returnType = value; }

        public override TypeViewModel ReturnType {  get { return _returnType; } }

        public override bool IsProtected => throw new NotImplementedException();
        protected override MethodViewModel CreateAdderMethodViewModel()
        {
            throw new NotImplementedException();
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        protected override TypeViewModel CreateEventHandlerType()
        {
            throw new NotImplementedException();
        }

        protected override MethodViewModel CreateInvoker()
        {
            throw new NotImplementedException();
        }

        protected override MethodViewModel CreateRemoverMethodViewModel()
        {
            throw new NotImplementedException();
        }

        CsvTypeViewModel _declaringType;
        public void SetDeclaringType( CsvTypeViewModel type ) { _declaringType = type; }
        protected override TypeViewModel GetDeclaringType() { return _declaringType; }

        protected override TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel)
        {
            throw new NotImplementedException();
        }
    }
}
