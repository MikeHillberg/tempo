using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tempo
{

    public  class ExportHelper
    {
        List<string> _keys = new List<string>();
        List<Dictionary<string, string>> _rows = new List<Dictionary<string, string>>();

        public void AppendCell(string key, object value)
        {
            AddKey(key);
            var currentRow = _rows.Last();
            currentRow[key] = value.ToString();
        }

        public void CreateNewRow()
        {
            _rows.Add(new Dictionary<string, string>());
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach(var key in _keys)
            {
                sb.Append(key);
                sb.Append(",");
            }
            sb.AppendLine();

            foreach (var row in _rows)
            {
                foreach(var key in _keys)
                {
                    if (row.TryGetValue(key, out var value))
                    {
                        sb.AppendQuoted(value);
                    }
                    sb.Append(",");
                }

                sb.AppendLine();
            }

            return sb.ToString();
         }

        public void AddRow()
        {
            _rows.Add(new Dictionary<string, string>());
        }

        public void AddKey(string key)
        {
            if (!_keys.Contains(key))
                _keys.Add(key);
        }
    }

    static public class CopyExport
    {
        static string memberKindKey = "Member Kind";
        static string nameKey = "Name";
        static string declaringTypeKey = "Declaring type";
        static string namespaceKey = "Namespace";
        static string baseTypeKey = "Base type";
        static string returnTypeKey = "Return type";
        static string contractKey = "Contract";
        static string contractVersionKey = "Contract version";
        static string versionKey = "Version";

        public static string GetItemsAsCsv(
            IEnumerable<BaseViewModel> items,
            bool forceAllTypes = false)
        {
            var helper = new ExportHelper();
            helper.AddKey(declaringTypeKey);
            helper.AddKey(nameKey);
            helper.AddKey(memberKindKey);
            helper.AddKey(namespaceKey);
            helper.AddKey(baseTypeKey);
            helper.AddKey(returnTypeKey);

            foreach (var item in items)
            {
                var typeVM = item as TypeViewModel;
                var methodVM = item as MethodViewModel;
                var propertyVM = item as PropertyViewModel;
                var eventVM = item as EventViewModel;
                var fieldVM = item as FieldViewModel;
                var constructorVM = item as ConstructorViewModel;
                var memberBaseVM = item as BaseViewModel;

                if (typeVM != null && !typeVM.ReallyMatchedInSearch && !forceAllTypes)
                {
                    continue;
                }

                helper.AddRow();

                if (typeVM != null)
                {
                    helper.AppendCell(declaringTypeKey, typeVM.PrettyName);
                    helper.AppendCell(memberKindKey, typeVM.TypeKind.ToString());
                    helper.AppendCell(namespaceKey, typeVM.Namespace);

                    if (typeVM.BaseType != null && !typeVM.BaseType.ShouldIgnore)
                    {
                        helper.AppendCell(baseTypeKey, typeVM.BaseType.PrettyName);
                    }
                }

                else
                {
                    if (constructorVM != null || methodVM != null)
                    {
                        string prettyName = item.PrettyName;

                        var nameBuilder = new StringBuilder();
                        IList<ParameterViewModel> parameters;

                        if (constructorVM != null)
                        {
                            nameBuilder.Append(item.DeclaringType.PrettyName);
                            parameters = constructorVM.Parameters;
                        }
                        else
                        {
                            nameBuilder.Append(item.PrettyName);
                            parameters = methodVM.Parameters;
                        }

                        if (parameters.Count != 0 && memberBaseVM.IsOverloaded)
                        {
                            nameBuilder.Append(GetParametersAsCsvSafeString(parameters, includeParameterNames: false));
                        }
                        prettyName = nameBuilder.ToString();
                        helper.AppendCell(nameKey, $"{prettyName}");
                        helper.AppendCell(declaringTypeKey, memberBaseVM.DeclaringType.PrettyName);

                        if(methodVM != null)
                        {
                            helper.AppendCell(returnTypeKey, methodVM.ReturnType.PrettyName);
                        }
                        
                    }
                    else if(propertyVM != null)
                    {
                        helper.AppendCell(nameKey, propertyVM.PrettyName);
                        helper.AppendCell(returnTypeKey, propertyVM.PropertyType.PrettyName);
                    }
                    else if(eventVM != null)
                    {
                        helper.AppendCell(nameKey, eventVM.PrettyName);
                        helper.AppendCell(returnTypeKey, eventVM.EventHandlerType.PrettyName);
                    }
                    else if(fieldVM != null)
                    {
                        helper.AppendCell(nameKey, fieldVM.PrettyName);
                        helper.AppendCell(returnTypeKey, fieldVM.FieldType.PrettyName);
                    }

                    helper.AppendCell(memberKindKey, memberBaseVM.MemberKind);
                    helper.AppendCell(declaringTypeKey, memberBaseVM.DeclaringType);
                    helper.AppendCell(namespaceKey, memberBaseVM.DeclaringType.Namespace);

                }

                var memberVM = memberBaseVM as MemberViewModel;
                if (memberVM != null)
                {
                    var contractParts = memberVM.Contract.Split(',');
                    if (contractParts.Length == 2)
                    {
                        helper.AppendCell(contractKey, contractParts[0]);
                        helper.AppendCell(contractVersionKey, contractParts[1]);
                    }

                    // Attributes don't have contract specifiers (or reflection doesn't expose them)
                    if (!string.IsNullOrEmpty(memberVM.Contract))
                    {
                        helper.AppendCell(versionKey, memberVM.VersionFriendlyName);
                    }
                }
            }

            return helper.ToString();
        }

        public static string GetResultsForClipboard(
            IEnumerable itemsIn,
           bool csv, 
            bool flat)
        {
            const string separator = ",";

            var memberTableText = new StringBuilder();
            StringBuilder typeTableText;
            typeTableText = memberTableText;

            var lastWasType = false;

            IEnumerable<BaseViewModel> itemsT, items;
            itemsT = from object i in itemsIn // listBox.Items
                         select i as BaseViewModel;

            if (csv)
            {
                items = from i in itemsT
                        orderby (i as BaseViewModel).DeclaringType.Namespace,
                                (i as BaseViewModel).DeclaringType.Name
                        select i;

                memberTableText.AppendLine("Kind,Version,Namespace,Type,Member,Return,Parameters");
            }
            else
            {
                items = itemsT;
            }

            bool showedMembers = false;

            foreach (var item in items)
            {
                if (item is TypeViewModel && !lastWasType)
                {
                    lastWasType = true;
                }
                else
                    lastWasType = false;

                if (!csv)
                {
                    if (!(item is TypeViewModel))
                        memberTableText.Append("    ");
                }

                {
                    if (item is TypeViewModel)
                    {
                        if (showedMembers && true) // copyToClipboard)
                        {
                            typeTableText.AppendLine();
                        }
                        showedMembers = false;

                        AppendType(
                            csv,
                            typeTableText,
                            memberTableText,
                            item as TypeViewModel,
                            flat);

                    }
                    else
                    {
                        showedMembers = true;
                        var vm = item as MemberViewModel;

                        if (csv)
                        {
                            {
                                memberTableText.Append(item.MemberKind.ToString());
                                memberTableText.Append(separator);

                                memberTableText.AppendQuoted(item.VersionFriendlyName);
                                memberTableText.Append(separator);

                                memberTableText.Append(vm.DeclaringType.Namespace);
                                memberTableText.Append(separator);

                                memberTableText.AppendQuoted(vm.DeclaringType.Name);
                                memberTableText.Append(separator);

                                memberTableText.Append(vm.PrettyName);
                                memberTableText.Append(separator);

                                memberTableText.AppendQuoted(vm.ReturnType.PrettyName);
                                memberTableText.Append(separator);

                                IList<ParameterViewModel> parameters = null;
                                if (vm is MethodViewModel)
                                    parameters = (vm as MethodViewModel).Parameters;
                                else if (vm is ConstructorViewModel)
                                    parameters = (vm as ConstructorViewModel).Parameters;

                                if (parameters != null)
                                {
                                    memberTableText.Append(GetParametersAsCsvSafeString(parameters, includeParameterNames : true));
                                    /*
                                    bool first = true;
                                    memberTableText.Append("(");

                                    foreach (var parameter in parameters)
                                    {
                                        if (first)
                                            first = false;
                                        else
                                            memberTableText.Append("|");

                                        memberTableText.AppendQuoted(
                                            parameter.ParameterType.PrettyName + " "
                                            + parameter.Name);
                                    }

                                    memberTableText.Append(")");
                                    */
                                }

                                else if (vm is PropertyViewModel)
                                {
                                    if ((vm as PropertyViewModel).CanWrite)
                                        memberTableText.Append("{get|set}");
                                    else
                                        memberTableText.Append("{get}");
                                }
                            }

                            memberTableText.AppendLine("");

                        }
                        else
                        {
                            if (flat)//(this.Settings.Flat)
                            {
                                memberTableText.AppendLine(
                                    (item as BaseViewModel).DeclaringType.PrettyFullName
                                    + "."
                                    + (item as BaseViewModel).PrettyName);
                            }
                            else
                            {
                                memberTableText.AppendLine((item as BaseViewModel).PrettyName);  //   (selectedItem.ToString());
                            }
                        }
                    }
                }
            }

            string results = null;
            results = memberTableText.ToString();

            //if (copyToClipboard)
            //{
            //    Clipboard.SetText(results);
            //}

            return results;
        }


        static string GetParametersAsCsvSafeString(IList<ParameterViewModel> parameters, bool includeParameterNames )
        {
            var sb = new StringBuilder();
            bool first = true;
            sb.Append("(");

            foreach (var parameter in parameters)
            {
                if (first)
                    first = false;
                else
                    sb.Append(",");

                var val = parameter.ParameterType.PrettyName;
                if (includeParameterNames)
                    val += $" {parameter.Name}";

                sb.Append(val);
            }

            sb.Append(")");
            return sb.ToString();
        }

        static private void AppendType(
            bool csv,
            StringBuilder typeTableText,
            StringBuilder memberTableText,
            TypeViewModel type,
            bool flat)
        {
            const string separator = ",";
            typeTableText = memberTableText;

            if (csv)
            {
                // Type:8.0 (6.2):Windows.Storage.Search:StorageFileQueryResult:3

                typeTableText.Append(type.MemberKind.ToString());
                typeTableText.Append(separator);

                typeTableText.AppendQuoted(type.VersionFriendlyName);
                typeTableText.Append(separator);

                typeTableText.Append((type as TypeViewModel).Namespace);
                typeTableText.Append(separator);

                typeTableText.AppendQuoted((type as TypeViewModel).PrettyName);
                typeTableText.Append(separator);

                if (type.BaseType == null)
                {
                    typeTableText.Append("");
                }
                else
                {
                    typeTableText.AppendQuoted(type.BaseType.FullName);
                }

                typeTableText.AppendLine("");
            }
            else if (flat) //(this.Settings.Flat)
            {
                typeTableText.AppendLine((type as TypeViewModel).PrettyFullName);
            }
            else
            {
                typeTableText.Append((type as TypeViewModel).PrettyName);
                typeTableText.Append(" (");
                typeTableText.Append((type as TypeViewModel).Namespace);
                typeTableText.AppendLine(")");
            }
        }

        // Write a csv string to a file and open it in Excel
        public static bool OpenInExcel(string csv, out string errorMessage)
        {
            errorMessage = null;

            string tempDir = System.Environment.ExpandEnvironmentVariables("%Temp%");
            var tempName = $@"{tempDir}\TempoExport_{Path.GetRandomFileName()}.csv";

            StreamWriter file = null;
            try
            {
                file = File.CreateText(tempName);
            }
            catch (Exception e)
            {
                errorMessage = "Couldn't create temporary file\n" + tempName + "\n" + e.Message;
                return false;
            }

            file.Write(csv);
            file.Close();

            var procInfo = new ProcessStartInfo();
            procInfo.FileName = tempName;
            procInfo.UseShellExecute = true;

            Process process;
            try
            {
                process = Process.Start(procInfo);
            }
            catch (Exception)
            {
                errorMessage = "Couldn't start Excel";
                return false;
            }

            process.EnableRaisingEvents = true; // Enable Exited
            process.Exited += (s, e) =>
            {
                try
                {
                    DesktopManager2.SafeDelete(tempName);
                }
                catch (Exception)
                { }
            };

            return true;
        }

    }
}
