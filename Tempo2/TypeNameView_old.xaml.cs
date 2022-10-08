using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class TypeNameView : UserControl
    {
        public TypeNameView()
        {
            this.InitializeComponent();
        }

        public TypeViewModel Type
        {
            get { return (TypeViewModel)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(TypeViewModel), typeof(TypeNameView), 
                new PropertyMetadata(null, (s,e) => (s as TypeNameView).TypeChanged()));

        private void TypeChanged()
        {
            if( Type == null )
            {
                _textBlock.Text = "";
                return;
            }

            GenerateTypeName(Type, _textBlock);

        }


        // bugbug: consolidate with desktop
        void GenerateTypeName(TypeViewModel type, TextBlock textBlock, bool firstArgument = true)
        {

            string typeNameBase;
            if (type.IsGenericType)
                typeNameBase = type.Name.Split(new char[] { '`' })[0]; //Type.Name.Split(new char[] { '`' })[0];
            else
                typeNameBase = type.Name; // Type.Name;

            if (type == null)
                return;

            /*
            var tooltip = TooltipTypeView.GetIfAvailable();
            */

            Run hl = new Run();

            /*
            if (tooltip == null)
            {
                hl = new Span();
                hl.Foreground = Brushes.Gray;
            }
            else
                hl = new Hyperlink();

            if (tooltip != null)
            {
                hl.ToolTip = tooltip; //new TooltipTypeView() { DataContext = Type };// { TypeViewModel = Type };
                hl.ToolTipOpening += (s, e) => TooltipTypeView.OnOpening(type);
                hl.ToolTipClosing += (s, e) => TooltipTypeView.OnClosing();
                ToolTipService.SetShowDuration(hl, 1000000);
                hl.Tag = type;
                hl.Focusable = false;
            }
            */

            //var startedBracket = false;
            if (!firstArgument)
                //hl.Inlines.Add(",");
                textBlock.Inlines.Add( new Run() {  Text = ","});
            /*
            else if (firstType && SquareBrackets)
            {
                startedBracket = true;
                Inlines.Add("[");
            }
            */

            //firstType = false;




            //hl.Inlines.Add(new FilterHilighter() { SearchText = typeNameBase, SearchType = type });
            _textBlock.Inlines.Add(new Run() { Text = typeNameBase });
            

            /*
            if (!Type2Ancestors.CanShowType(type))
                hl.IsEnabled = false;
                */

            /*
            Inlines.Add(hl);
            */

            if (!type.IsGenericType)
            {
                /*
                if (startedBracket && SquareBrackets)
                    Inlines.Add("]");
                    */

                return;
            }

            _textBlock.Inlines.Add(new Run() { Text = "<" });

            firstArgument = true;
            foreach (var ta in type.GetGenericArguments())
            {
                GenerateTypeName(ta, textBlock, firstArgument);
                firstArgument = false;

            }
            textBlock.Inlines.Add( new Run() { Text = ">" });

            /*
            if (startedBracket && SquareBrackets)
                Inlines.Add("]");
                */


        }

    }
}
