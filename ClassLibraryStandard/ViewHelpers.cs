using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class ViewHelpers
    {
        public static string GetSearchSampleUri(MemberViewModel currentItem)
        {
            // Sample:  https://github.com/Microsoft/Windows-universal-samples/search?utf8=%E2%9C%93&q=GestureRecognizer+GetForCurrentView&type=Code

            string query = "";

            if (currentItem is TypeViewModel)
                query = (currentItem as TypeViewModel).Name;
            else
            {
                var memberVM = currentItem as MemberViewModel;
                query = memberVM.DeclaringType.Name + "+" + memberVM.Name;
            }

            return @"https://github.com/Microsoft/Windows-universal-samples/search?utf8=%E2%9C%93&q=" + query + @"&type=Code";

        }


        public static string GetIndexedSampleUri(MemberViewModel currentItem)
        {
            string query = "";

            if (currentItem is TypeViewModel)
                query = (currentItem as TypeViewModel).FullName;
            else
            {
                var memberVM = currentItem as MemberViewModel;
                query = memberVM.DeclaringType.FullName;

                var methodVM = memberVM as MethodViewModel;
                var constructorVM = memberVM as ConstructorViewModel;
                if (constructorVM != null)
                {
                    query = query + ".:ctor" + "`" + constructorVM.Parameters.Count;
                }
                else if (methodVM != null)
                {
                    query = query + "." + memberVM.Name + "`" + methodVM.Parameters.Count;
                }
                else
                {
                    query = query + "." + memberVM.Name;
                }
            }

            return @"http://oldnewthing.github.io/Windows-universal-samples/?" + query;

        }




    }




}
