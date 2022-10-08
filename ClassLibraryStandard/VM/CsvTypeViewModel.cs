using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tempo;

namespace Tempo
{
    public class CsvTypeViewModel : TypeViewModel
    {

        public override bool IsProtected => false;

        public override Assembly Assembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsInternal => throw new NotImplementedException();
        public override string AssemblyLocation => null;
        public override TypeViewModel UnderlyingEnumType => null;

        public override TypeAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string FullName
        {
            get
            {
                if (_fullName == null)
                    _fullName = Namespace + "." + Name;
                return _fullName;
            }
        }
        string _fullName = null;

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
        public override string Name
        {
            get { return _name; }
        }
        public void SetName(string name ) { _name = name; }

        public override string Namespace
        {
            get
            {
                return _namespace;
            }
        }
        string _namespace;
        public void SetNamespace( string value ) { _namespace = value; }

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


        List<TypeViewModel> _genericArguments;
        public void AddGenericArgument( TypeViewModel type)
        {
            if (_genericArguments == null)
                _genericArguments = new List<TypeViewModel>();

            if (type == null)
                return;

            _genericArguments.Add(type);
        }

        public override TypeViewModel[] GetGenericArguments()
        {
            AddGenericArgument(null);
            return _genericArguments.ToArray();
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
            EnsureConstructors();
            return _constructors;
        }

        protected override IList<EventViewModel> CalculateEventsFromTypeOverride(bool shouldFlatten)
        {
            Debug.Assert(!shouldFlatten);
            EnsureEvents();
            return _events;
        }

        protected override IList<FieldViewModel> CalculateFieldsFromTypeOverride(bool shouldFlatten)
        {
            Debug.Assert(!shouldFlatten);
            EnsureFields();
            return _fields;
        }

        protected override IList<MethodViewModel> CalculateMethodsFromTypeOveride(bool shouldFlatten)
        {
            Debug.Assert(!shouldFlatten);
            EnsureMethods();
            return _methods;
        }

        void EnsureProperties()
        {
            if (_properties == null)
                _properties = new List<PropertyViewModel>();
        }
        protected override IList<PropertyViewModel> CalculatePropertiesFromTypeOverride(bool shouldFlatten)
        {
            Debug.Assert(!shouldFlatten);
            EnsureProperties();
            return _properties as IList<PropertyViewModel>;
        }
        List<PropertyViewModel> _properties;
        public void AddProperty( CsvPropertyViewModel property)
        {
            EnsureProperties();
            _properties.Add(property);
        }

        List<MethodViewModel> _methods;
        public void AddMethod( CsvMethodViewModel method )
        {
            EnsureMethods();
            _methods.Add(method);
        }
        private void EnsureMethods()
        {
            if (_methods == null)
                _methods = new List<MethodViewModel>();
        }

        List<ConstructorViewModel> _constructors;
        public void AddConstructor(CsvConstructorViewModel constructor)
        {
            EnsureConstructors();
            _constructors.Add(constructor);
        }
        private void EnsureConstructors()
        {
            if (_constructors == null)
                _constructors = new List<ConstructorViewModel>();
        }


        List<EventViewModel> _events;
        public void AddEvent( CsvEventViewModel member)
        {
            EnsureEvents();
            _events.Add(member);
        }

        private void EnsureEvents()
        {
            if (_events == null)
                _events = new List<EventViewModel>();
        }

        List<FieldViewModel> _fields;
        public void AddField(CsvFieldViewModel field)
        {
            EnsureFields();
            _fields.Add(field);
        }

        private void EnsureFields()
        {
            if (_fields == null)
                _fields = new List<FieldViewModel>();
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            throw new NotImplementedException();
        }

        CsvTypeViewModel _baseType;
        public void SetBaseType( CsvTypeViewModel value ) { _baseType = value; }
        protected override TypeViewModel GetBaseType() { return _baseType; }
    }
}
