using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public static class MsdnHelper
    {
        public static string CalculateWinMDMsdnAddress(object item)
        {

            var m = item as MethodViewModel;
            var t = item as TypeViewModel;
            var f = item as FieldViewModel;
            var p = item as PropertyViewModel;
            var e = item as EventViewModel;
            var c = item as ConstructorViewModel;

            TypeViewModel declaringType = null;
            string memberName = null;


            if (t != null)
            {
                declaringType = t;
                memberName = "";
            }
            else if (p != null)
            {
                memberName = p.Name;
                declaringType = p.DeclaringType;
            }
            else if (m != null)
            {
                memberName = m.Name;
                declaringType = m.DeclaringType;
            }
            else if (e != null)
            {
                memberName = e.Name;
                declaringType = e.DeclaringType;
            }
            else if (c != null)
            {
                memberName = "-ctor";
                declaringType = c.DeclaringType;
            }
            else if (f != null)
            {
                memberName = f.Name;
                declaringType = f.DeclaringType;
            }

            // https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.button.-ctor
            // https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Controls.WebView._ctor

            // This base address works for most APIs whether Windows or WASDK
            var typicalAddress = @"https://docs.microsoft.com/uwp/api/";

            // Need to use this base address for some WASDK APIs
            var wasdkAddress = @"https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/";

            string typeName;
            if (declaringType.IsGenericType)
            {
                typeName = $"{declaringType.Namespace}.{declaringType.GenericTypeName}";

                var args = declaringType.GetGenericArguments();

                typeName = $"{typeName}-{args.Count().ToString()}";
            }
            else
                typeName = declaringType.FullName;


            StringBuilder address;
            if(typeName.StartsWith("Windows."))
            {
                address = new StringBuilder(typicalAddress);
            }
            else
            {
                // bugbug: need to add special-case for WinUI2
                address = new StringBuilder(wasdkAddress);
            }

            address.Append(typeName);
            if (!string.IsNullOrEmpty(memberName))
            {
                if (declaringType.IsEnum)
                {
                    address.Append("#fields");
                }
                else
                {
                    address.Append(".");
                    address.Append(memberName);
                }
            }

            return address.ToString();
        }

    }
}
