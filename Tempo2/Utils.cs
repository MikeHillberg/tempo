using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualBasic;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace Tempo
{
    public class Utils
    {
        // E.g. Text="{x:Bind l:Utils.DebugHook('foo')}"
        static public object DebugHook(object o)
        {
            if(Debugger.IsAttached)
            {
                Debugger.Break();
            }

            return o;
        }

        static public Visibility CollapsedIfTrue(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        static public Visibility VisibleIf(bool? b)
        {
            return b == true ? Visibility.Visible : Visibility.Collapsed;
        }
        static public Orientation HorizontalIf(bool b)
        {
            return b ? Orientation.Horizontal : Orientation.Vertical;
        }

        static public bool Or(bool b1, bool b2)
        {
            return b1 || b2;
        }

        static public string Parenthesize(string value)
        {
            if(string.IsNullOrEmpty(value))
            {
                return "";
            }

            return $"({value})";
        }

        /// <summary>
        /// b ? Visibility.Collapsed : Visibility.Visible;
        /// </summary>
        static public Visibility NotVisibleIf(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        static public Visibility VisibleIfEither(bool b1, bool b2)
        {
            return (b1 || b2) ? Visibility.Visible : Visibility.Collapsed;
        }

        static public Visibility VisibleIfNeither(bool b1, bool b2)
        {
            return (!b1 && !b2) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Invert Visibility (e.g. Collapsed becomes Visibile)
        /// </summary>
        static public Visibility NotVisibility(Visibility v)
        {
            return v == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        static public bool Not(bool b)
        {
            return !b;
        }

        // x:Bind has a problem mixing xBind methods with the Visibility special case
        static public Visibility NotHack(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        static public Visibility VisibilityNot(bool value)
        {
            return value ? Visibility.Collapsed : Visibility.Visible;
        }

        static public object Tertiary(bool b)
        {
            return ListViewSelectionMode.None;
        }

        // Helper to set text onto the clipboard
        // Can't make it an extension method on Clipboard because it's a static class
        public static void SetClipboardText(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }

        static public string IfThenElse(bool i, string t, string e)
        {
            return i ? t : e;
        }

        public static bool IsEmpty(string[] strings)
        {
            return strings == null || strings.Length == 0;
        }

        public static bool IsntEmpty(string[] strings)
        {
            return !IsEmpty(strings);
        }

        public static bool TrueOnly(bool? b)
        {
            return b == true;
        }

        public static bool FalseOnly(bool? b)
        {
            return b == false;
        }
    }
}
