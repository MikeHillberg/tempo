using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace Tempo
{
    public class CsSyntaxGenerator
    {

        static Thickness Indent1 = new Thickness(20, 0, 0, 0);
        static Thickness Indent2 = new Thickness(40, 0, 0, 0);

        // In light mode, should be blue
        // In dark mode, should be #569cd6
        static SolidColorBrush _blueBrush = new SolidColorBrush(Colors.Blue); // bugbug: put into app or resources?

        // CsSyntaxGenerator.Member attached property
        // bugbug:  should type this as RTB, but that breaks x:Bind
        public static MemberOrTypeViewModelBase GetMember(DependencyObject obj)
        {
            return (MemberOrTypeViewModelBase)obj.GetValue(MemberProperty);
        }
        public static void SetMember(DependencyObject obj, MemberOrTypeViewModelBase value)
        {
            obj.SetValue(MemberProperty, value);
        }
        public static readonly DependencyProperty MemberProperty =
            DependencyProperty.RegisterAttached("Member", typeof(MemberOrTypeViewModelBase), typeof(CsSyntaxGenerator),
                new PropertyMetadata(null, (s, e) => MemberChanged(s as RichTextBlock)));


        // This is only being used in MemberDetailView.xaml  (targets RichTextBlock)
        private static void MemberChanged(RichTextBlock textBlock)
        {
            textBlock.Blocks.Clear();

            var member = GetMember(textBlock);
            if (member == null)
            {
                return;
            }

            var type = member.DeclaringType;

            var paragraph = new Paragraph();
            textBlock.Blocks.Add(paragraph);
            var inlines = paragraph.Inlines;

            var run = new Run()
            {
                Foreground = _blueBrush,
                Text = type.ModifierCodeString + " " + type.TypeKind.ToString().ToLower() + " "
            };
            inlines.Add(run);

            GenerateTypeName(type, inlines);

            var sb = new StringBuilder();

            if (type.TypeKind == TypeKind.Class
                && (type.BaseType != null
                    || type.PublicInterfaces != null && type.PublicInterfaces.Count != 0))
            {
                sb.Append(" : ...");
            }

            sb.Append(" \n{");
            inlines.Add(sb.ToString());

            paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            paragraph.Inlines.Add("...\n");

            if (member is PropertyViewModel)
                InsertProperty(textBlock, member as PropertyViewModel);
            else if (member is MethodViewModel)
                InsertMethod(textBlock, member as MethodViewModel);
            else if (member is EventViewModel)
                InsertEvent(textBlock, member as EventViewModel);
            else if (member is FieldViewModel)
                InsertField(textBlock, member as FieldViewModel);
            else if (member is ConstructorViewModel)
                InsertConstructor(textBlock, member as ConstructorViewModel);
            else
                Debug.Assert(false);

            // Highlight everything that matches the search string
            SearchHighlighter.HighlightMatches(textBlock, App.SearchExpression?.MemberRegex);

            paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            paragraph.Inlines.Add("\n...");

            paragraph = new Paragraph();
            textBlock.Blocks.Add(paragraph);
            paragraph.Inlines.Add("}");
        }

        static void InsertConstructor(RichTextBlock textBlock, ConstructorViewModel constructor)
        {
            var paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            var inlines = paragraph.Inlines;

            var run = new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue }, // bugbug
                Text = constructor.ModifierCodeString + " "
            };
            inlines.Add(run);

            var name = constructor.DeclaringType.CSharpName;
            var grav = name.IndexOf('`');
            if (grav != -1)
                name = name.Substring(0, grav);

            InsertMethodNameAndParameters(textBlock, name, constructor.Parameters, inlines);

            paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            paragraph.Inlines.Add("{...}");
        }



        static void InsertField(RichTextBlock textBlock, FieldViewModel field)
        {
            var paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            var inlines = paragraph.Inlines;

            if (!field.DeclaringType.IsEnum)
            {
                GenerateTypeName(field.FieldType, inlines, highlightMatch: true, withUpArrow: true);
                inlines.Add(" ");
            }

            inlines.AddWithSearchHighlighting($"{field.Name};");

        }


        static void InsertEvent(RichTextBlock textBlock, EventViewModel ev)
        {
            var paragraph = new Paragraph() { Margin = Indent1 };
            var inlines = paragraph.Inlines;
            textBlock.Blocks.Add(paragraph);

            var run = new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue }, // bugbug
                Text = $"{ev.ModifierCodeString} event "
            };
            inlines.Add(run);


            if (ev.IsInvokerMatch && !ev.EventHandlerType.IsNameMatch)
            {
                // Add an up arrow to the text to indicate that there's something
                // in the base class that matches.
                var upArrow = new Run() { Text = UpArrowCodePoint };
                upArrow.FontWeight = FontWeights.Bold;
                inlines.Add(upArrow);
            }



            GenerateTypeName(ev.EventHandlerType, inlines, withUpArrow: true);

            inlines.AddWithSearchHighlighting($" {ev.Name};");
        }



        static void InsertMethod(RichTextBlock textBlock, MethodViewModel method)
        {
            var paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            var inlines = paragraph.Inlines;

            var run = new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue }, // bugbug
                Text = method.ModifierCodeString + " "
            };
            inlines.Add(run);

            GenerateTypeName(method.ReturnType, inlines, highlightMatch: true, withHyperlink: true, withUpArrow: true);


            var parameters = method.Parameters;

            InsertMethodNameAndParameters(textBlock, method.Name, parameters, inlines);

            paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            paragraph.Inlines.Add("{...}");
        }

        private static void InsertMethodNameAndParameters(
            RichTextBlock textBlock, string methodName,
            IList<ParameterViewModel> parameters,
            InlineCollection inlines)
        {

            // Method name and open paren

            var text = "\n" + methodName;
            if (parameters.Count == 0)
                text += " ()";
            else
                text += " (";

            inlines.AddWithSearchHighlighting(text);

            if (parameters.Count != 0)
            {
                // Put parameters in a new paragraph so that it can be indented

                var paragraph = new Paragraph() { Margin = Indent2 };
                textBlock.Blocks.Add(paragraph);
                inlines = paragraph.Inlines;

                for (int i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];

                    if (i != 0)
                        inlines.Add("\n");

                    InsertParameter(inlines, parameter);

                    if (i + 1 == parameters.Count)
                        inlines.Add(")");
                    else
                        inlines.Add(",");
                }
            }
        }

        private static void InsertParameter(InlineCollection inlines, ParameterViewModel parameter)
        {
            if (parameter.IsOut)
            {
                var run = new Run() { Foreground = new SolidColorBrush(Colors.Blue) }; // bugbug
                run.Text = "out ";
                inlines.Add(run);
            }

            // If the parameter type doesn't match, then it won't get highlighted, so we need to highlight it here
            if (parameter.IsMatch
                && !parameter.IsNameMatch
                && !parameter.ParameterType.IsMatch)
            {
                // Add an up arrow to the text to indicate that there's something
                // in the base class that matches.
                var upArrow = new Run() { Text = UpArrowCodePoint };
                upArrow.FontWeight = FontWeights.Bold;
                inlines.Add(upArrow);
            }

            GenerateTypeName(parameter.ParameterType, inlines, withUpArrow: true);
            inlines.Add($" {parameter.Name}");
        }

        static void InsertProperty(RichTextBlock textBlock, PropertyViewModel property)
        {
            var paragraph = new Paragraph() { Margin = Indent1 };
            textBlock.Blocks.Add(paragraph);
            var inlines = paragraph.Inlines;

            var run = new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue }, // bugbug
                Text = property.ModifierCodeString + " "
            };
            inlines.Add(run);

            GenerateTypeName(property.PropertyType, inlines, withHyperlink: true, highlightMatch: true, withUpArrow: true);

            string text = " " + property.Name + "\n{";
            inlines.AddWithSearchHighlighting(text);

            text = property.GetSetCodeString;
            inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue },
                Text = text
            });

            text = "}";
            inlines.Add(text);

        }




        // CsSyntaxGenerator.TypeDeclaration attached property
        public static TypeViewModel GetTypeDeclaration(DependencyObject obj)
        {
            return (TypeViewModel)obj.GetValue(TypeDeclarationProperty);
        }
        public static void SetTypeDeclaration(DependencyObject obj, TypeViewModel value)
        {
            obj.SetValue(TypeDeclarationProperty, value);
        }
        public static readonly DependencyProperty TypeDeclarationProperty =
            DependencyProperty.RegisterAttached("TypeDeclaration", typeof(TypeViewModel), typeof(CsSyntaxGenerator),
                new PropertyMetadata(null, (s, e) => TypeDeclarationChanged(s as TextBlock)));

        private static void TypeDeclarationChanged(TextBlock textBlock)
        {

            Run run;
            var type = GetTypeDeclaration(textBlock);
            if (type == null)
            {
                textBlock.Text = string.Empty;
                return;
            }
            textBlock.ClearValue(TextBlock.TextProperty);

            if (type.IsFlagsEnum)
            {
                run = new Run()
                {
                    Text = "[flags]\n"
                };
                textBlock.Inlines.Add(run);
            }

            // E.g. public, static
            string allModifiers = type.ModifierCodeString;
            if (type.IsSealed && type.IsClass && !type.IsDelegate)
            {
                allModifiers = "sealed " + allModifiers;
            }

            string typeKind;
            if (type.IsDelegate)
            {
                // Show delegate classes as a delegate rather than a class
                typeKind = "delegate";
            }
            else
            {
                typeKind = type.TypeKind.ToString().ToLower();
            }

            run = new Run()
            {
                Foreground = new SolidColorBrush() { Color = Colors.Blue }, // bugbug
                Text = $"{allModifiers} {typeKind} "
            };
            textBlock.Inlines.Add(run);

            // We don't need to show the up arrow for the type, e.g. because its base class matched rather than its name,
            // because we're on the type batch and that will show up somewhere else
            GenerateTypeName(type, textBlock.Inlines, withHyperlink: false, withUpArrow: false);

            if (type.TypeKind == TypeKind.Class
                && (type.BaseType != null && !type.BaseType.ShouldIgnore || type.PublicInterfaces.Count != 0))
            {
                textBlock.Inlines.Add(" : ");
            }

            var first = true;
            if (type.BaseType != null && !type.BaseType.ShouldIgnore)
            {
                textBlock.Inlines.Add("\n    ");
                GenerateTypeName(type.BaseType, textBlock.Inlines);
                first = false;
            }

            foreach (var iface in type.PublicInterfaces)
            {
                if (first)
                {
                    textBlock.Inlines.Add("\n    ");
                    first = false;
                }
                else
                    textBlock.Inlines.Add(",\n    ");

                GenerateTypeName(iface, textBlock.Inlines);
            }

            if (type.IsDelegate)
            {
                // Delegates are classes but don't have normal class syntax

                textBlock.Inlines.Add("\n   (");
                var parameters = type.DelegateParameters;
                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];
                    InsertParameter(textBlock.Inlines, parameter);

                    if (i + 1 < parameters.Count)
                    {
                        textBlock.Inlines.Add(", ");
                    }

                }
                textBlock.Inlines.Add(");");
            }
            else
            {
                textBlock.Inlines.Add("\n{...}");
            }

            SearchHighlighter.HighlightMatches(textBlock, App.SearchExpression?.MemberRegex);
        }

        // CsSyntaxGenerator.MemberName attached property
        public static MemberOrTypeViewModelBase GetMemberName(DependencyObject obj)
        {
            return (MemberOrTypeViewModelBase)obj.GetValue(MemberNameProperty);
        }
        public static void SetMemberName(DependencyObject obj, MemberOrTypeViewModelBase value)
        {
            obj.SetValue(MemberNameProperty, value);
        }
        public static readonly DependencyProperty MemberNameProperty =
            DependencyProperty.RegisterAttached("MemberName", typeof(MemberOrTypeViewModelBase), typeof(CsSyntaxGenerator),
                new PropertyMetadata(null, (s, e) => MemberNameChanged(s as TextBlock)));

        private static void MemberNameChanged(TextBlock textBlock)
        {
            var memberName = GetMemberName(textBlock);
            if (memberName == null)
            {
                textBlock.Text = string.Empty;
                return;
            }

            GenerateMemberName(memberName, textBlock.Inlines);
        }

        static void GenerateMemberName(
            MemberOrTypeViewModelBase member,
            InlineCollection inlines,
            bool withHyperlink = true)
        {
            if (member == null)
                return;

            if (withHyperlink && !member.DeclaringType.IsInCurrentTypeSet)
            {
                withHyperlink = false;
            }

            if (withHyperlink)
            {
                var hl = new Hyperlink();
                hl.Click += (s, e) => TypeDetailView.GoToItem(member);

                inlines.Add(hl);
                inlines = hl.Inlines;
            }

            inlines.Add($"{member.MemberPrettyName}");
        }


        // CsSyntaxGenerator.TypeName attached property
        public static TypeViewModel GetTypeName(DependencyObject obj)
        {
            return (TypeViewModel)obj.GetValue(TypeNameProperty);
        }
        public static void SetTypeName(DependencyObject obj, TypeViewModel value)
        {
            obj.SetValue(TypeNameProperty, value);
        }
        public static readonly DependencyProperty TypeNameProperty =
            DependencyProperty.RegisterAttached("TypeName", typeof(TypeViewModel), typeof(CsSyntaxGenerator),
                new PropertyMetadata(null, (s, e) => TypeNameChanged(s as TextBlock)));


        private static void TypeNameChanged(TextBlock textBlock)
        {
            var type = GetTypeName(textBlock);
            if (type == null)
            {
                textBlock.Text = string.Empty;
                return;
            }

            textBlock.Text = String.Empty;
            GenerateTypeName(type, textBlock.Inlines);

            // Highlight everything that matches the search string
            SearchHighlighter.HighlightMatches(textBlock, App.SearchExpression?.TypeRegex);
        }

        public const string UpArrowCodePoint = "\u2191 ";

        static void GenerateTypeName(
            TypeViewModel type,
            InlineCollection inlines,
            bool withHyperlink = true,
            bool highlightMatch = true,
            bool firstArgument = true,
            bool withUpArrow = false)
        {
            if (type == null)
            {
                return;
            }

            if (!type.IsInCurrentTypeSet)
            {
                withHyperlink = false;
            }

            string typeNameBase;
            TypeViewModel targetType = type;
            if (type.IsGenericType)
            {
                typeNameBase = type.GenericTypeName;
                targetType = type.GetGenericTypeDefinition();
            }
            else if (!string.IsNullOrEmpty(type.CSharpName))
            {
                typeNameBase = type.CSharpName;
            }
            else
            {
                typeNameBase = type.Name;
            }

            var ampIndex = typeNameBase.LastIndexOf('&');
            if (ampIndex != -1)
            {
                // [out] parameter
                typeNameBase = typeNameBase.Substring(0, ampIndex);
            }

            if (!firstArgument)
            {
                inlines.Add(", ");
            }


            // Sometimes we add an up arrow next to a type to indicate
            // that the base type matched the search
            // Not sure if all these bools are actually used, the
            // `withUpArrow` is the main one
            //if (withHyperlink && withUpArrow && highlightMatch)
            if (withUpArrow && highlightMatch)
            {
                //var baseType = type.BaseType;
                //if (baseType != null && baseType.IsMatch)
                if (type.IsMatch && !type.IsNameMatch)
                {
                    // Add an up arrow to the text to indicate that there's something
                    // in the base class that matches.
                    // Tried doing this with a rectangle before, but TextBlock.Inlines
                    // doesn't support InlineUIContainer
                    var upArrow = new Run() { Text = UpArrowCodePoint };
                    upArrow.FontWeight = FontWeights.Bold;
                    inlines.Add(upArrow);
                }
            }


            inlines.AddWithSearchHighlighting(
                typeNameBase,
                withHyperlink ? targetType : null);

            if (!type.IsGenericType)
            {
                return;
            }

            inlines.Add(new Run() { Text = "<" });

            firstArgument = true;
            foreach (var ta in type.GetGenericArguments())
            {
                GenerateTypeName(ta, inlines, highlightMatch: highlightMatch, firstArgument: firstArgument);
                firstArgument = false;

            }
            inlines.Add(new Run() { Text = ">" });

        }

    }
}
