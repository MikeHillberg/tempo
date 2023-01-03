using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tempo
{
    static public class ApiComments
    {
        static List<ApiCommentItem> _items = null;
        static string _lockable = "ApiComments";


        static public Task<string> GetCommentsAsync(MemberOrTypeViewModelBase member)
        {
            //return await Task.Run(() =>
            //{
            //    return GetComments(member);
            //});

            
            return BackgroundHelper2.DoWorkAsync(() =>
            {
                return GetComments(member);
            });

        }

        private static string GetComments(MemberOrTypeViewModelBase member)
        {
            Load();
            var remarks = new StringBuilder();

            var isType = member is TypeViewModel;
            var first = true;

            foreach (var item in _items)
            {
                switch (item.Kind)
                {
                    case ApiCommentKind.Type:
                        {
                            if (member.DeclaringType.FullName == item.Thing)
                            {
                                AddLine(remarks, item.Remarks, ref first);
                            }
                        }
                        break;

                    case ApiCommentKind.Member:
                        {
                            var memberMinusArgs = member.FullName;
                            var index = memberMinusArgs.IndexOf('(');
                            if (index != -1)
                            {
                                memberMinusArgs = memberMinusArgs.Substring(0, index);
                            }
                            if (memberMinusArgs == item.Thing)
                            {
                                AddLine(remarks, item.Remarks, ref first);
                            }
                        }
                        break;

                    case ApiCommentKind.Namespace:
                        {
                            if (member.DeclaringType.Namespace == item.Thing)
                            {
                                AddLine(remarks, item.Remarks, ref first);
                            }
                        }
                        break;

                    case ApiCommentKind.Regexp:
                        {
                            if (item.Regex.IsMatch(member.FullName))
                            {
                                AddLine(remarks, item.Remarks, ref first);
                            }
                        }
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            return remarks.ToString();
        }

        static void AddLine(StringBuilder sb, string line, ref bool first)
        {
            if (!first)
            {
                sb.AppendLine();
            }
            first = false;

            sb.Append(line);
        }

        static void Load()
        {
            if (_items != null)
                return;


            lock (_lockable)
            {
                if (_items != null)
                    return;

                // Don't kill ourselves over this
                try
                {
                    _items = new List<ApiCommentItem>();

                    //var textReader = new StringReader(DesktopCommon.Properties.Resource1.NotProud);
                    //var line = textReader.ReadLine(); // Skip headers
                    //var lineNumber = 1;

                    //while ((line = textReader.ReadLine()) != null)
                    //{
                    //    lineNumber++;
                    //    var columns = line.Split('\t');
                    //    var item = new ApiCommentItem()
                    //    {
                    //        Thing = columns[1],
                    //        Category = columns[2],
                    //        Commentor = columns[3],
                    //        Remarks = columns[4],
                    //        LineNumber = lineNumber
                    //    };

                    //    var kind = columns[0];
                    //    switch (kind)
                    //    {
                    //        case "class":
                    //        case "enum":
                    //        case "type":
                    //        case "struct":
                    //            item.Kind = ApiCommentKind.Type;
                    //            break;

                    //        case "property":
                    //        case "method":
                    //        case "event":
                    //        case "member":
                    //        case "field":
                    //            item.Kind = ApiCommentKind.Member;
                    //            break;

                    //        case "namespace":
                    //            item.Kind = ApiCommentKind.Namespace;
                    //            break;

                    //        case "regexp":
                    //            item.Kind = ApiCommentKind.Regexp;
                    //            item.Regex = new Regex(item.Thing);
                    //            break;

                    //        default:
                    //            throw new Exception("Unrecognized API kind");
                    //    }

                    //    _items.Add(item);
                    //}
                }
                catch (Exception e)
                {
                    //MainWindow.InstanceOld.ProcessUnhandledException(e);
                    UnhandledExceptionManager.ProcessException(e);
                }
            }
        }

        public static void EnsureSync(IList<TypeViewModel> types)
        {
            foreach (var type in types)
            {
                type.SetApiDesignNotes(GetComments(type));

                foreach (var member in type.Members)
                {
                    member.SetApiDesignNotes(GetComments(member));
                }
            }
        }
    }


    public class ApiCommentItem
    {
        public ApiCommentKind Kind { get; set; }
        public string Thing { get; set; }
        public string Category { get; set; }
        public string Commentor { get; set; }
        public string Remarks { get; set; }
        public int LineNumber { get; set; }
        public Regex Regex { get; set; }
    }

    public enum ApiCommentKind
    {
        Type,
        Namespace,
        Member,
        Regexp
    }
}


