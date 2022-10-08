using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class CsvFieldViewModel : FieldViewModel
    {
        public override FieldAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();

        public override bool IsProtected => throw new NotImplementedException();

        public override bool IsAbstract => false;

        CsvTypeViewModel _fieldType;
        public void SetFieldType(CsvTypeViewModel value) { _fieldType = value; }
        public override TypeViewModel FieldType { get { return _fieldType; } }

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

        public override bool IsInitOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsLiteral
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

        public override bool GetIsSpecialName()
        {
            throw new NotImplementedException();
        }

        string _name;
        public void SetName(string value) { _name = value; }
        public override string Name { get { return _name; } }

        public override object RawConstantValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeViewModel ReturnType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override bool CheckIsStatic()
        {
            throw new NotImplementedException();
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        CsvTypeViewModel _declaringType;
        public void SetDeclaringType(CsvTypeViewModel value) { _declaringType = value; }
        protected override TypeViewModel GetDeclaringType() { return _declaringType; }

        protected override TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel)
        {
            throw new NotImplementedException();
        }
    }
}
