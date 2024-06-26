﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tempo
{
    public class AttributeTypeInfo
    {
        public string TypeName { get; set; }
        public string Properties { get; set; }

        public static IEnumerable<AttributeTypeInfo> WrapCustomAttributes(IEnumerable<CustomAttributeViewModel> customAttributes)
        {
            foreach (var a in customAttributes)
            {
                var sb = new StringBuilder();
                var ta = new AttributeTypeInfo();

                ta.TypeName = a.Name;

                if (ta.TypeName == "ComImportAttribute"
                    || ta.TypeName == "StaticAttribute"
                    || ta.TypeName == "ActivatableAttribute"
                    || ta.TypeName == "ContractVersionAttribute"
                    || ta.TypeName == "ComposableAttribute")
                {
                    // Skip WinRT implementation details attributes
                    continue;
                }

                if (ta.TypeName == "GuidAttribute")
                {
                    // Guids get special processing so that don't look so ugly
                }

                int suffixIndex = ta.TypeName.LastIndexOf("Attribute");
                if (suffixIndex != -1)
                {
                    ta.TypeName = "[" + ta.TypeName.Substring(0, suffixIndex) + "]";
                }

                var args = a.ConstructorArguments;
                if (args != null)
                {
                    var guidString = a.TryParseGuidAttribute(); // String.Empty if it's not a Guid attribute


                    if (guidString != string.Empty)
                    {
                        sb.Append(guidString);
                    }
                    else
                    {
                        // Write out the constructor arguments in "(Type) value" format
                        for(int i = 0; i < args.Count; i++)
                        {
                            var constructorArgument = args[i];

                            string argumentValue = constructorArgument.Value == null
                                ? "null"
                                : constructorArgument.Value.ToString();

                            if (constructorArgument.ArgumentType.IsEnum)
                            {
                                foreach (var field in constructorArgument.ArgumentType.Fields)
                                {
                                    try
                                    {
                                        var val1 = field.RawConstantValue;
                                        var val2 = constructorArgument.Value;
                                        if (Object.Equals(val1, val2))
                                            argumentValue = field.Name;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                    }
                                }
                            }

                            sb.AppendFormat("({0}) ", constructorArgument.ArgumentType.Name);

                            if (constructorArgument.Value is UInt32)
                                sb.AppendFormat("0x{0:X}", constructorArgument.Value);
                            else
                                sb.AppendFormat("{0}", argumentValue);

                            if (i < args.Count - 1)
                            {
                                sb.AppendLine();
                            }
                        }
                    }
                }

                // Write out the named arguments in "Name=Value" format
                var namedArguments = a.NamedArguments;
                for(int i = 0; i < namedArguments.Count; i++)
                {
                    var property = a.NamedArguments[i];

                    sb.AppendFormat("{0}=", property.MemberName);
                    if (property.TypedValue.Value is UInt32)
                        sb.AppendFormat("0x{0:X}", property.TypedValue.Value);
                    else
                        sb.AppendFormat("{0}", property.TypedValue.Value);

                    if (i < namedArguments.Count - 1)
                    {
                        sb.AppendLine();
                    }
                }



                ta.Properties = sb.ToString();


                yield return ta;
            }
        }



    }

}
