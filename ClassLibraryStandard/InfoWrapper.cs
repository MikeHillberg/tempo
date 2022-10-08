using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Windows.Data;
using System.Reflection;
using System.ComponentModel;

namespace Tempo
{
    public abstract class InfoWrapper<T>
        where T : BaseViewModel
    {
        public InfoWrapper(T info)
        {
            Info = info;
        }

        public T Info { get; private set; }

        public abstract BaseViewModel MemberViewModel { get; }

        public string Access
        {
            get
            {
                string r = "";

                if (MemberViewModel == null)
                    return r;

                //dynamic memberInfo = MemberInfo;
                var memberInfo = MemberViewModel;

                if (memberInfo.IsPublic)
                    r += "public ";
                else if (memberInfo.IsProtected)
                    r += "protected ";
                else if (memberInfo.IsInternal)
                    r += "internal ";
                else
                    r += "private ";

                if (!(memberInfo is FieldViewModel) && memberInfo.IsAbstract)
                    r += "abstract ";
                else if (
                    memberInfo.IsVirtual
                    && (!(memberInfo is MemberViewModel) || !(memberInfo as MemberViewModel).IsSealed))
                {
                    r += "virtual ";
                }

                if (memberInfo is FieldViewModel
                    && (memberInfo as FieldViewModel).IsConst)
                {
                    r += "const ";
                }
                else if (memberInfo.IsStatic)
                {
                    r += "static ";
                }

                return r;
            }
        }


        public IEnumerable<AttributeTypeInfo> CalculatedCustomAttributes
        {
            get
            {
                if (Info != null)
                {
                    var o = GetMemberAttributes(Info);

                    if (o.Count != 0)
                    {
                        return AttributeTypeInfo.WrapCustomAttributes(o); // bugbug
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
        }


        // Attribute or CustomAttributeData
        public static IList<CustomAttributeViewModel> GetMemberAttributes(BaseViewModel memberInfo)
        {
            //if (memberInfo.GetType().Name != "ReflectionOnlyType" &&
            //    memberInfo.DeclaringType != null && memberInfo.DeclaringType.GetType().Name != "ReflectionOnlyType")
            //{
            //    throw new Exception("non custom attributes");
            //}

            var attrs = memberInfo.CustomAttributes;
            //if (memberInfo is PropertyInfo)
            //{
            //    var propertyInfo = memberInfo as PropertyInfo;
            //    var attrs2 = propertyInfo.GetGetMethod().GetCustomAttributesData();
            //    return attrs.Union(attrs2).ToList();
            //}
            return attrs;

        }

    }


    public class InfoWrapper : InfoWrapper<MethodViewModel>
    {
        public InfoWrapper(MethodViewModel mi)
            : base(mi)
        {
        }

        public MethodViewModel MethodViewModel { get { return Info; } }

        public override BaseViewModel MemberViewModel
        {
            get { return MethodViewModel; }
        }

        public IEnumerable<string> Parameters
        {
            get
            {
                foreach (var p in MethodViewModel.Parameters)
                {
                    string r = "";
                    if (p.IsOut)
                        r += "[out] ";

                    r += p.ParameterType.ToString() + " " + p.Name;
                    //TypeValueConverter.ToString(p.ParameterType) + " " + p.Name;

                    yield return r;
                }
            }
        }

        public IEnumerable<object> Parameters2
        {
            get
            {
                foreach (var p in MethodViewModel.Parameters)
                {
                    string r = "";
                    if (p.IsOut)
                        r += "[out]";


                    yield return new { Prefix = r, Type = p.ParameterType, Name = p.Name };
                }
            }
        }

    }


    public class ConstructorInfoWrapper : InfoWrapper<ConstructorViewModel>
    {
        public ConstructorInfoWrapper(ConstructorViewModel ci)
            : base(ci)
        {
        }

        public TypeViewModel DeclaringType
        {
            get { return Info.DeclaringType; }
        }

        public override BaseViewModel MemberViewModel
        {
            get { return Info; }
        }

        public ConstructorViewModel ConstructorViewModel { get { return Info; } }


        public IEnumerable<string> Parameters
        {
            get
            {
                foreach (var p in ConstructorViewModel.Parameters)
                {
                    string r = "";
                    if (p.IsOut)
                        r += "[out] ";

                    r += p.ParameterType.ToString() + " " + p.Name;
                    //TypeValueConverter.ToString(p.ParameterType) + " " + p.Name;

                    yield return r;
                }
            }
        }

        public IEnumerable<object> Parameters2
        {
            get
            {
                foreach (var p in ConstructorViewModel.Parameters)
                {
                    string r = "";
                    if (p.IsOut)
                        r += "[out]";

                    yield return new { Prefix = r, Type = p.ParameterType, Name = p.Name };
                }
            }
        }
    }


    public class FieldInfoWrapper : InfoWrapper<FieldViewModel>
    {
        public FieldInfoWrapper(FieldViewModel fi)
            : base(fi)
        {
        }

        public override BaseViewModel MemberViewModel
        {
            get { return FieldViewModel; }
        }

        public FieldViewModel FieldViewModel { get { return Info; } }
        public object RawValue
        {
            get
            {
                if (FieldViewModel.IsLiteral)
                {
                    var o = FieldViewModel.RawConstantValue;
                    if (o is uint)
                        return (int)(uint)o;
                    else if (o is byte)
                        return (byte)o;
                    else
                        return (int)o;
                }
                else
                    return null;
            }
        }


    }

    public class EventInfoWrapper : InfoWrapper<EventViewModel>
    {
        public EventInfoWrapper(EventViewModel info)
            : base(info)
        {

        }

        override public BaseViewModel MemberViewModel
        {
            get { return Info; }
        }

        public TypeViewModel ArgsType
        {
            get
            {
                return Info.ArgsType;
                //if (Info == null)
                //    return null;

                //var invoke = Info.EventHandlerType.GetMethod("Invoke");
                //var parameters = invoke.GetParameters();
                //if (parameters.Length < 2)
                //    return null;
                //return parameters[1].ParameterType;

            }
        }

    }









    public class PropertyInfoWrapper : InfoWrapper<PropertyViewModel>
    {
        public PropertyInfoWrapper(PropertyViewModel propertyInfo)
            : base(propertyInfo)
        {
        }

        public override BaseViewModel MemberViewModel
        {
            get { return Info; }
        }

        public PropertyViewModel PropertyViewModel { get { return Info; } }

        public object GetterAttributes
        {
            get
            {
                return PropertyViewModel.GetterAttributes;
                //var o = PropertyInfo.GetGetMethod();
                //return (o != null) ? (object)o.Attributes : null;
            }
        }

        public object SetterAttributes
        {
            get
            {
                return PropertyViewModel.SetterAttributes;
                //var o = PropertyInfo.GetSetMethod();
                //return o != null ? (object)o.Attributes : null;
            }
        }

        public bool ActualCanWrite
        {
            get
            {
                return PropertyViewModel.CanWrite;
                //return PropertyInfo.CanWrite && PropertyInfo.GetSetMethod() != null;
            }

        }

        public object[] CalculatedCustomAttributesOld
        {
            get
            {
                throw new Exception("Type");
                //return null;

                //if (PropertyInfo != null)
                //{
                //    object[] o;
                //    try
                //    {
                //        o = PropertyInfo.GetCustomAttributes(true);
                //    }
                //    catch (InvalidOperationException)
                //    {
                //        var o2 = PropertyInfo.GetCustomAttributesData();
                //        o = o2.ToArray();
                //    }

                //    if (o.Length != 0)
                //        return o;
                //    else
                //        return null;
                //}
                //else
                //    return null;
            }
        }


    }


}
