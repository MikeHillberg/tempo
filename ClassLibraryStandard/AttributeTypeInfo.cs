using System;
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
            // bugbug
            // In the c++ projection some attributes lose their arguments.
            // Maybe just AttributeUsage, because both CLI and WinRT have one but they're different?

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
                var namedArguments = a.NamedArguments;

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
                        for (int i = 0; i < args.Count; i++)
                        {
                            var constructorArgument = args[i];

                            string argumentValue = constructorArgument.Value == null
                                ? "null"
                                : constructorArgument.Value.ToString();

                            // For enums, find the field with the matching value and use the field's name
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

                            // Special case for AttributeTargets which in WinRT shows up as a .Net
                            // type rather than the Windows AttributeTargets type.
                            // Some attributes project wrong, e.g. ProtectedAttribute attribute targets projects as a 0, should be 1024 (Interface),
                            // seems to align with attributes that are internal implementation details for WinRT?
                            else if (constructorArgument.ArgumentType.FullName == "System.AttributeTargets"
                                      && constructorArgument.Value is UInt32)
                            {
                                var targets = (System.AttributeTargets)(UInt32)constructorArgument.Value;
                                argumentValue = targets.ToString();
                            }

                            // Otherwise just use the numeric value
                            else if (constructorArgument.Value is UInt32)
                            {
                                argumentValue = constructorArgument.Value.ToString();
                            }

                            sb.AppendFormat($"({constructorArgument.ArgumentType.Name}) {argumentValue}");

                            // Add a newline between constructor arguments and between constructor & named arguments
                            if (i < args.Count - 1 || namedArguments.Count != 0)
                            {
                                sb.AppendLine();
                            }
                        }
                    }
                }

                // Write out the named arguments in "Name=Value" format
                for (int i = 0; i < namedArguments.Count; i++)
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
