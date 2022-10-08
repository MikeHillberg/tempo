using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tempo
{
    public class DuckMethod
    {
        //MethodViewModel _methodInfo;
        //FieldViewModel _fieldInfo;
        //ConstructorViewModel _constructorInfo;

        BaseViewModel _baseViewModel = null;

        public DuckMethod(BaseViewModel m)
        {
            if (m is PropertyViewModel)
            {
                var p = m as PropertyViewModel;
                if (p.Getter != null)
                    m = p.Getter;
                else
                    m = p.Setter;

                if (m == null)
                    throw new Exception("Type");
            }
            else if (m is EventViewModel)
            {
                m = (m as EventViewModel).Adder;
            }

            _baseViewModel = m;

            if (_baseViewModel == null)
                throw new Exception("Type");

        }

        public TypeViewModel DeclaringType
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).DeclaringType;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).DeclaringType;
                else
                    return (_baseViewModel as FieldViewModel).DeclaringType;
            }
        }

        public string Contract
        {
            get
            {
                if (_baseViewModel is MemberViewModel)
                    return (_baseViewModel as MemberViewModel).Contract;
                else
                    return string.Empty;
            }
        }

        public bool IsDeprecated
        {
            get
            {
                if (_baseViewModel is MemberViewModel)
                    return (_baseViewModel as MemberViewModel).IsDeprecated;
                else
                    return false;
            }
        }

        public int WordCount
        {
            get
            {
                if (_baseViewModel is MemberViewModel)
                    return (_baseViewModel as MemberViewModel).WordCount;
                else
                    return 0;
            }
        }


        public IList<ParameterViewModel> GetParameters()
        {
            if (_baseViewModel is MethodViewModel)
                return (_baseViewModel as MethodViewModel).Parameters;//.GetParameters();
            else if (_baseViewModel is ConstructorViewModel)
                return (_baseViewModel as ConstructorViewModel).Parameters;//.GetParameters();
            else
                return null;
        }


        public bool IsRestricted
        {
            get { return _baseViewModel.IsRestricted; }
        }



        public bool IsPrivate
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsPrivate;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsPrivate;
                else
                    return (_baseViewModel as FieldViewModel).IsPrivate;
            }
        }

        public bool IsExperimental
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsExperimental;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsExperimental;
                else
                    return (_baseViewModel as FieldViewModel).IsExperimental;
            }
        }

        public bool IsPublic
        {
            get
            {
                return _baseViewModel.IsPublic;
            }
        }

        public string Version
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).Version;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).Version;
                else
                    return (_baseViewModel as FieldViewModel).Version;
            }
        }

        public bool IsSpecialName
        {
            get
            {
                //return _memberViewModel.IsSpecialName;
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsSpecialName;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsSpecialName;
                else
                    return (_baseViewModel as FieldViewModel).IsSpecialName;
            }
        }

        BaseViewModel MemberInfo
        {
            get
            {
                return _baseViewModel;
            }
        }

        public IList<CustomAttributeViewModel> GetCustomAttributesData()
        {
            return InfoWrapper.GetMemberAttributes(MemberInfo);
        }

        public string Name
        {
            get
            {
                return _baseViewModel.Name;
                //if (_methodInfo != null)
                //    return _methodInfo.Name;
                //else if (_constructorInfo != null)
                //    return _constructorInfo.Name;
                //else
                //    return _fieldInfo.Name;
            }
        }

        private bool IsFamily
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsFamily;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsFamily;
                else
                    return (_baseViewModel as FieldViewModel).IsFamily;
            }
        }

        private bool IsAssembly
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsAssembly;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsAssembly;
                else
                    return (_baseViewModel as FieldViewModel).IsAssembly;
            }
        }

        private bool IsFamilyOrAssembly
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsFamilyOrAssembly;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsFamilyOrAssembly;
                else
                    return (_baseViewModel as FieldViewModel).IsFamilyOrAssembly;
            }
        }

        public bool IsVirtual
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsVirtual;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsVirtual;
                else
                    return false;
            }
        }

        public bool IsFinal
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsFinal;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsFinal;
                else
                    return true;
            }
        }

        public bool IsAbstract
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsAbstract;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsAbstract;
                else
                    return false;
            }
        }

        public object Attributes
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).Attributes;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).Attributes;
                else
                    return (_baseViewModel as FieldViewModel).Attributes;
            }
        }

        public bool IsStatic
        {
            get
            {
                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsStatic;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsStatic;
                else
                    return (_baseViewModel as FieldViewModel).IsStatic;
            }
        }

        public bool IsProtected
        {
            get
            {
                // bugbug: For some reason, enum fields show up as both protected and public
                if(this.IsPublic)
                {
                    return false;
                }

                if (_baseViewModel is MethodViewModel)
                    return (_baseViewModel as MethodViewModel).IsProtected;
                else if (_baseViewModel is ConstructorViewModel)
                    return (_baseViewModel as ConstructorViewModel).IsProtected;
                else
                    return (_baseViewModel as FieldViewModel).IsProtected;



            }
        }

    }



}




