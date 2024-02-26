using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    // For things I don't support well yet:  CLR projection types and parameterized types
    public class ComplexTypeViewModel : TypeViewModel
    {
        public override Assembly Assembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override string AssemblyLocation => null;

        public override TypeAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeViewModel UnderlyingEnumType => null;

        public override bool IsProtected => false; // Types are either public or internal or private


        public override string FullName {  get { return Name; } }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsByRef
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsClass
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsDelegate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsEnum
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsEventArgs
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

        public override bool IsInternal => this.IsFamily;

        public override bool IsGenericParameter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsGenericType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInterface
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsNotPublic
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

        public override bool IsStatic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsStruct
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsValueType
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

        public override bool IsVoid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MemberKind MemberKind
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
        public void SetName( string value ) { _name = value; }
        public override string Name {  get { return _name; } }

        //public override string Namespace
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public override TypeViewModel ReturnType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IList<TypeViewModel> CalculateInterfacesFromType(bool includeInternal = false)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TypeViewModel> GetAllInterfaces()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TypeViewModel> GetConstructorInterfaces()
        {
            throw new NotImplementedException();
        }

        public override TypeViewModel[] GetGenericArguments()
        {
            throw new NotImplementedException();
        }

        public override TypeViewModel GetGenericTypeDefinition()
        {
            throw new NotImplementedException();
        }

        public override TypeViewModel GetInterface(string name)
        {
            throw new NotImplementedException();
        }

        protected override IList<ConstructorViewModel> CalculateConstructorsFromTypeOverride()
        {
            throw new NotImplementedException();
        }

        protected override IList<EventViewModel> CalculateEventsFromTypeOverride(bool shouldFlatten)
        {
            throw new NotImplementedException();
        }

        protected override IList<FieldViewModel> CalculateFieldsFromTypeOverride(bool shouldFlatten)
        {
            throw new NotImplementedException();
        }

        protected override IList<MethodViewModel> CalculateMethodsFromTypeOveride(bool shouldFlatten)
        {
            throw new NotImplementedException();
        }

        protected override IList<PropertyViewModel> CalculatePropertiesFromTypeOverride(bool shouldFlatten)
        {
            throw new NotImplementedException();
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        protected override TypeViewModel GetBaseType()
        {
            throw new NotImplementedException();
        }
    }
}
