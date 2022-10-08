using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace Tempo
{
    /// <summary>
    /// TextBox that raises an Invoked event if the user presses enter, or after typing anything and delaying
    /// </summary>
    public sealed partial class TimedTextBox : UserControl
    {
        public TimedTextBox()
        {
            this.InitializeComponent();

            _timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += _timer_Tick;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TimedTextBox), new PropertyMetadata(""));

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(TimedTextBox), new PropertyMetadata(""));

        private void _timer_Tick(object sender, object e)
        {
            _timer.Stop();
            Invoked?.Invoke(this, null);
        }

        DispatcherTimer _timer;

        private void _tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            // If the timer's already running, restart it
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }

            _timer.Start();
        }

        public event EventHandler Invoked;

        private void _tb_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // When Enter is pressed, invoke

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _timer.Stop();
                Invoked?.Invoke(this, null);
            }
        }
    }
}
