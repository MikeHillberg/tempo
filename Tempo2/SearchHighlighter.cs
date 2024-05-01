using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace Tempo
{
    public class SearchHighlighter
    {

        // IsEnabled attached property
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SearchHighlighter),
                new PropertyMetadata(false, (s, e) => IsEnabledChanged(s as TextBlock)));

        private static void IsEnabledChanged(TextBlock textBlock)
        {
            var isEnabled = GetIsEnabled(textBlock);
            if (isEnabled)
            {
                var registrationId = textBlock.RegisterPropertyChangedCallback(
                    TextBlock.TextProperty,
                    (s, dp) => TextChangedCallback(s as TextBlock));
                SetTextChangedRegistrationId(textBlock, registrationId);

                // bugbug: no detach capability, but we never detach anyway, so the else case is actually dead code here
                WeakSearchExpressionChangedHandler.Attach(textBlock);
            }
            else
            {
                textBlock.UnregisterPropertyChangedCallback(TextBlock.TextProperty, GetTextChangedRegistrationId(textBlock));
                textBlock.ClearValue(TextChangedRegistrationIdProperty);
            }
        }

        class WeakSearchExpressionChangedHandler
        {
            static internal void Attach(TextBlock textBlock)
            {
                // The only reference to this object will be from the event source
                _ = new WeakSearchExpressionChangedHandler(textBlock);
            }

            WeakReference<TextBlock> _weakTarget;
            private WeakSearchExpressionChangedHandler(TextBlock textBlock)
            {
                _weakTarget = new WeakReference<TextBlock>(textBlock);

                // This is the ref that keeps the object alive
                App.SearchExpressionChanged += App_SearchExpressionChanged;
            }

            private void App_SearchExpressionChanged(object sender, object e)
            {
                _weakTarget.TryGetTarget(out var textBlock);
                if (textBlock != null)
                {
                    SearchHighlighter.TextChangedCallback(textBlock);
                }
                else
                {
                    App.SearchExpressionChanged -= App_SearchExpressionChanged;
                }
            }
        }


        public static int GetTestProp(DependencyObject obj)
        {
            return (int)obj.GetValue(TestPropProperty);
        }

        public static void SetTestProp(DependencyObject obj, int value)
        {
            obj.SetValue(TestPropProperty, value);
        }
        public static readonly DependencyProperty TestPropProperty =
            DependencyProperty.RegisterAttached("TestProp", typeof(int), typeof(SearchHighlighter), new PropertyMetadata(0));





        static bool _updatingText = false;
        private static void TextChangedCallback(TextBlock textBlock)
        {
            // bugbug: fix this when multi-threaded
            if (_updatingText)
            {
                return;
            }

            if (App.SearchExpression == null)
            {
                return;
            }

            HighlightMatches(textBlock, App.SearchExpression.MemberRegex);

        }

        static public void InsertSearchHighlightedString(
            InlineCollection inlines,
            string str,
            TypeViewModel hyperlinkTarget = null)
        {
            if (hyperlinkTarget != null && !hyperlinkTarget.IsDotNetType)
            {
                var hl = new Hyperlink();
                SetTypeTarget(hl, hyperlinkTarget);
                hl.Click += TypeClick;

                // Show the member of the type in a tooltip
                if (hyperlinkTarget != null)
                {
                    var toolTip = new ToolTipTypeView() { TypeViewModel = hyperlinkTarget };
                    ToolTipService.SetToolTip(hl, toolTip);
                }

                inlines.Add(hl);
                inlines = hl.Inlines;
            }

            inlines.Add(str);

            return;
        }

        private static void TypeClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            TypeDetailView.GoToItem(GetTypeTarget(sender));
        }

        public static TypeViewModel GetTypeTarget(Hyperlink obj)
        {
            return (TypeViewModel)obj.GetValue(TypeTargetProperty);
        }

        public static void SetTypeTarget(Hyperlink obj, TypeViewModel value)
        {
            obj.SetValue(TypeTargetProperty, value);
        }
        public static readonly DependencyProperty TypeTargetProperty =
            DependencyProperty.RegisterAttached("TypeTarget", typeof(TypeViewModel), typeof(SearchHighlighter), new PropertyMetadata(null));



        // Highlight search string matches in a TextBlock
        public static void HighlightMatches(TextBlock textBlock, Regex regex)
        {
            textBlock.TextHighlighters.Clear();
            _ = HighlightMatchesCommon(textBlock.TextHighlighters, textBlock.Text, regex, 0);
            return;
        }

        static Regex UpArrowRegex = new Regex(CsSyntaxGenerator.UpArrowCodePoint);

        // Highlight search string matches in a *Rich* TextBlock
        public static void HighlightMatches(RichTextBlock rtb, Regex regex)
        {
            var textHighlighters = rtb.TextHighlighters;
            textHighlighters.Clear();
            var index = 0;

            // Walk the paragraphs
            foreach (var block in rtb.Blocks)
            {
                // Highlight the runs and hyperlinks in this paragraph
                var paragraph = block as Paragraph;
                foreach (var inline in paragraph.Inlines)
                {
                    var run = inline as Run;
                    if (run != null)
                    {
                        var runText = run.Text;

                        // Highlight the up arrow that indicates there's a match in the base class
                        var regexToUse = regex;
                        if (runText == CsSyntaxGenerator.UpArrowCodePoint)
                        {
                            regexToUse = UpArrowRegex;
                        }

                        index += HighlightMatchesCommon(textHighlighters, runText, regexToUse, index);
                    }
                    else if (inline is Hyperlink)
                    {
                        var hyperlink = inline as Hyperlink;
                        index += HighlightInlines(hyperlink.Inlines, textHighlighters, regex, index);
                    }
                    // bugbug: Not handling the InlineUIContainer case
                }
            }
        }

        // Highlight an Inline collection with search string matches
        static int HighlightInlines(InlineCollection inlines, IList<TextHighlighter> textHighlighters, Regex regex, int index)
        {
            var returnIndex = 0;
            foreach (var inline in inlines)
            {
                var run = inline as Run;
                if (run != null)
                {
                    returnIndex += HighlightMatchesCommon(textHighlighters, run.Text, regex, index);
                }
                else
                {
                    var hyperlink = inline as Hyperlink;
                    returnIndex += HighlightInlines(inlines, textHighlighters, regex, index);
                }
            }

            return returnIndex;
        }


        // Highlighting code in common between a TextBlock or RichTextBlock
        // The input index is the base offset to use when creating a TextHighlighter
        static int HighlightMatchesCommon(IList<TextHighlighter> textHighlighters, string text, Regex regex, int index)
        {
            var consumed = text.Length;

            // If there's nothing to do then don't do anything
            if (regex == null || string.IsNullOrEmpty(text))
            {
                return consumed;
            }

            var match = regex.Match(text);
            if (match == Match.Empty)
            {
                return consumed;
            }


            var textHighlighter = new TextHighlighter()
            {
                // bugbug: can't figure out how to do this with correct accessibility
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 229, 153))
            };
            var ranges = textHighlighter.Ranges;

            while (match != Match.Empty)
            {
                var range = new TextRange()
                {
                    StartIndex = match.Index + index,
                    Length = match.Length
                };
                ranges.Add(range);

                // Sanity check (e.g. search for "$")
                if (ranges.Count > 1000)
                {
                    return consumed;
                }

                match = regex.Match(text, match.Index + match.Length);
            }

            textHighlighters.Add(textHighlighter);
            return consumed;
        }



        // private TextChangedRegistrationId
        private static long GetTextChangedRegistrationId(DependencyObject obj)
        {
            return (long)obj.GetValue(TextChangedRegistrationIdProperty);
        }

        private static void SetTextChangedRegistrationId(DependencyObject obj, long value)
        {
            obj.SetValue(TextChangedRegistrationIdProperty, value);
        }
        private static readonly DependencyProperty TextChangedRegistrationIdProperty =
            DependencyProperty.RegisterAttached("TextChangedRegistrationId", typeof(long), typeof(SearchHighlighter),
                new PropertyMetadata(-1));


    }
}
