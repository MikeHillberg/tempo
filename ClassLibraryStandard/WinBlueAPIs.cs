using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo 
{
    public class ImportedApis
    {
        Dictionary<string, ImportedTypeInfo> _importedTypeInfo = new Dictionary<string, ImportedTypeInfo>();
        public List<CsvTypeViewModel> _importedCsvTypes = new List<CsvTypeViewModel>();

        public static bool Initialized { get; private set; }

        public static ImportedApis Win8 = new ImportedApis();
        public static ImportedApis WinBlue = new ImportedApis();
        public static ImportedApis PhoneBlue = new ImportedApis();
        public static ImportedApis Win10 = new ImportedApis();

        static public void Initialize(string win8Raw, string winBlueRaw, string phoneBlueRaw, string win10Raw)
        {
            if (win8Raw != null)
                Win8.Initialize(win8Raw);

            if (winBlueRaw != null)
                WinBlue.Initialize(winBlueRaw);

            if (phoneBlueRaw != null)
                PhoneBlue.Initialize(phoneBlueRaw);

            if (win10Raw != null)
                Win10.Initialize2(win10Raw);

        }

        void Initialize(string raw)
        {
            ImportedTypeInfo importedTypeInfo = null;
            var lines = new List<string>();

            using (var stringReader = new StringReader(raw))
            {
                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null)
                        break;

                    lines.Add(line);
                }
            }


            var lineNumber = 0;
            foreach (var line in lines)
            {
                ++lineNumber;

                var parts = line.Split(':');
                if (parts.Length < 3)
                    continue;

                var kind = parts[0].Trim();
                if (kind == "Type")
                {
                    AddToTable(importedTypeInfo);

                    importedTypeInfo = new ImportedTypeInfo();

                    importedTypeInfo.FullName = parts[1].Trim() + "." + parts[2].Trim();

                }
                else
                {
                    var memberInfo = new ImportedMemberInfo();
                    memberInfo.Name = parts[3].Trim();

#if DEBUG
                    memberInfo.OriginalLine = line;
#endif

                    memberInfo.Kind = (MemberKind)Enum.Parse(typeof(MemberKind), parts[0].Trim(), false);

                    if (memberInfo.Kind == MemberKind.Method || memberInfo.Kind == MemberKind.Constructor)
                    {
                        var parameters = parts[5].Trim();
                        var split = parameters.Split('|');
                        var check = split[0];
                        if (check == "()")
                            memberInfo.ParameterCount = 0;
                        else
                            memberInfo.ParameterCount = split.Length;
                    }

                    importedTypeInfo.Members.Add(memberInfo);
                }

            }

            AddToTable(importedTypeInfo);
            Initialized = true;

        }




        private void AddToTable(ImportedTypeInfo importedTypeInfo)
        {
            if (importedTypeInfo == null)
                return;

            lock (_importedTypeInfo)
            {
                _importedTypeInfo[importedTypeInfo.FullName] = importedTypeInfo;
            }
        }

        public ImportedTypeInfo Find(string fullName)
        {
            lock (_importedTypeInfo)
            {
                ImportedTypeInfo importedTypeInfo = null;

                if (_importedTypeInfo.TryGetValue(fullName, out importedTypeInfo))
                    return importedTypeInfo;
                else
                    return null;
            }
        }

        public bool MemberExists(TypeViewModel type, MemberOrTypeViewModelBase member)
        {
            var typeInfo = Find(type.Namespace + "." + type.PrettyName); // bugbug
            if (typeInfo == null)
                return false;

            foreach (var m in typeInfo.Members)
            {

                if (member.Name == m.Name)
                {
                    if (member.MemberKind == MemberKind.Method || member.MemberKind == MemberKind.Constructor)
                    {
                        int parameterCount = 0;

                        if (member.MemberKind == MemberKind.Method)
                            parameterCount = (member as MethodViewModel).Parameters.Count;
                        else
                            parameterCount = (member as ConstructorViewModel).Parameters.Count;

                        if (parameterCount == m.ParameterCount)
                            return true;
                        else
                            continue;
                    }
                    else
                        return true;
                }
            }

            return false;
        }



        public void Initialize2(string raw)
        {
            var lines = new List<string>();

            using (var stringReader = new StringReader(raw))
            {
                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null)
                        break;

                    var parts = line.Split(':');
                    if (parts.Length < 3)
                        continue;

                    var kind = parts[0].Trim();

                    // Type:10:Windows.ApplicationModel.Activation:ActivatedEventsContract:9
                    // Type:10:Windows.Foundation:IAsyncOperation<22>:
                    if (kind == "Type")
                    {
                        var importedTypeInfo = new CsvTypeViewModel();

                        importedTypeInfo.SetNamespace(parts[2].Trim());
                        importedTypeInfo.SetBaseType(GetTypeAtIndex(parts[4]));

                        var typeName = parts[3];
                        var openIndex = typeName.IndexOf('<');
                        if( openIndex == -1 )
                        {
                            importedTypeInfo.SetName(parts[3].Trim());
                        }
                        else
                        {
                            var closeIndex = typeName.LastIndexOf('>');
                            var sub = typeName.Substring(openIndex+1, closeIndex-openIndex-1);
                            var split = sub.Split(',');

                            importedTypeInfo.SetName(typeName.Substring(0, openIndex) + "`" + split.Length.ToString());
                            foreach( var t in split)
                            {
                                importedTypeInfo.AddGenericArgument(GetTypeAtIndex(t));
                            }
                        }


                        AddToTypeTable(importedTypeInfo);
                    }

                    //Property: 10:3:AppUserModelId: 1:{ get}
                    else if (kind == "Property")
                    {
                        var property = new CsvPropertyViewModel();
                        property.SetName(parts[3]);

                        var declaringType = GetTypeAtIndex(parts[2]);
                        property.SetDeclaringType(declaringType);

                        property.SetReturnType(GetTypeAtIndex(parts[4]));
                        var rt = property.ReturnType;

                        declaringType.AddProperty(property);
                    }

                    // Method:8.1 (6.3):5521:TryParse:11:(5 input|5522 transferCodingHeaderValue)
                    else if (kind == "Method")
                    {
                        var method = new CsvMethodViewModel();
                        method.SetName(parts[3]);

                        var declaringType = GetTypeAtIndex(parts[2]);
                        method.SetDeclaringType(declaringType);

                        method.SetReturnType(GetTypeAtIndex(parts[4]));

                        if( parts[5] != "()")
                        {
                            var parameters = parts[5].Substring(1, parts[5].Length - 2).Split('|');
                            foreach( var parameter in parameters )
                            {
                                var split = parameter.Split(' ');
                                var parameterType = GetTypeAtIndex(split[0]);
                                method.AddParameter(parameterType, split[1]);
                            }
                        }

                        declaringType.AddMethod(method);
                    }

                    // Constructor:8.1 (6.3):158:.ctor:158:()
                    // bugbug: combine with methods
                    else if (kind == "Constructor")
                    {
                        var constructor = new CsvConstructorViewModel();
                        constructor.SetName(parts[3]);

                        var declaringType = GetTypeAtIndex(parts[2]);
                        constructor.SetDeclaringType(declaringType);


                        if (parts[5] != "()")
                        {
                            var parameters = parts[5].Substring(1, parts[5].Length - 2).Split('|');
                            foreach (var parameter in parameters)
                            {
                                var split = parameter.Split(' ');
                                var parameterType = GetTypeAtIndex(split[0]);
                                constructor.AddParameter(parameterType, split[1]);
                            }
                        }

                        declaringType.AddConstructor(constructor);
                    }



                    // Event:10:169:SyncStatusChanged:172:
                    else if (kind == "Event")
                    {
                        var member = new CsvEventViewModel();
                        member.SetName(parts[3]);

                        var declaringType = GetTypeAtIndex(parts[2]);
                        member.SetDeclaringType(declaringType);

                        member.SetReturnType(GetTypeAtIndex(parts[4]));

                        declaringType.AddEvent(member);
                    }

                    // Field:10:171:AuthenticationError:171:
                    else if (kind == "Field")
                    {
                        var member = new CsvFieldViewModel();
                        member.SetName(parts[3]);

                        var declaringType = GetTypeAtIndex(parts[2]);
                        member.SetDeclaringType(declaringType);

                        member.SetFieldType(GetTypeAtIndex(parts[4]));
                        declaringType.AddField(member);
                    }


                    Initialized = true;

                }
            }
        }

        private void AddToTypeTable(CsvTypeViewModel importedTypeInfo)
        {
            if (importedTypeInfo == null)
                return;

            lock (_importedCsvTypes)
            {
                _importedCsvTypes.Add(importedTypeInfo);
            }
        }

        private CsvTypeViewModel GetTypeAtIndex(string indexString)
        {
            if (string.IsNullOrEmpty(indexString))
                return null;

            int index = int.Parse(indexString);
            return _importedCsvTypes[index];
        }






    }


    public class ImportedTypeInfo
    {
        public ImportedTypeInfo()
        {
            Members = new List<ImportedMemberInfo>();
        }

        public string FullName { get; set; }
        public IList<ImportedMemberInfo> Members { get; private set; }

#if DEBUG
        public string OriginalLine { get; set; }
#endif

    }


    public class ImportedMemberInfo
    {
        public string Name { get; set; }
        public MemberKind Kind { get; set; }
        public int ParameterCount { get; set; }

#if DEBUG
        public string OriginalLine { get; set; }
#endif
    }

}
