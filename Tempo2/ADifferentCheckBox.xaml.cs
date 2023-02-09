// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    /// <summary>
    /// A checkbox that's made up of three radio buttons
    /// </summary>
    public sealed partial class ADifferentCheckBox : UserControl
    {
        public ADifferentCheckBox()
        {
            this.InitializeComponent();
        }

        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool?), typeof(ADifferentCheckBox), 
                new PropertyMetadata(null,(d,_) => (d as ADifferentCheckBox).IsCheckedChanged()));

        void IsCheckedChanged()
        {
            // When we set IsChecked in response to a button being pushed, raise an event,
            // but don't try to update the buttons
            if(_updating)
            {
                Click?.Invoke(this, null);
                return;
            }

            // IsChecked was set externally
            if(IsChecked == true)
            {
                IsYChecked = true;
                IsNChecked = IsXChecked = false;
            }
            else if(IsChecked == false)
            {
                IsNChecked = true;
                IsYChecked = IsXChecked = false;
            }
            else
            {
                IsXChecked = true;
                IsYChecked = IsNChecked = false;
            }

        }


        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(ADifferentCheckBox), 
                new PropertyMetadata(""));



        public bool IsThreeState
        {
            get { return (bool)GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, value); }
        }
        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register("IsThreeState", typeof(bool), typeof(ADifferentCheckBox), 
                new PropertyMetadata(false));




        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ADifferentCheckBox), 
                new PropertyMetadata(""));




        /// <summary>
        /// Is the "Yes" button checked
        /// </summary>
        public bool IsYChecked
        {
            get { return (bool)GetValue(IsYCheckedProperty); }
            set { SetValue(IsYCheckedProperty, value); }
        }
        public static readonly DependencyProperty IsYCheckedProperty =
            DependencyProperty.Register("IsYChecked", typeof(bool), typeof(ADifferentCheckBox), 
                new PropertyMetadata(false, (d,_) => (d as ADifferentCheckBox).Update(true)));

        bool _updating = false;

        /// <summary>
        /// Respond to one of the buttons being clicked. The parameter indicates if it was Y/N/X
        /// </summary>
        void Update(bool? updated)
        {
            _updating = true;
            try
            {
                if (updated == true && IsYChecked)
                {
                    IsChecked = true;
                    IsNChecked = IsXChecked = false;
                }
                else if (updated == false && IsNChecked)
                {
                    IsChecked = false;
                    IsYChecked = IsXChecked = false;
                }
                else if (updated == null && IsXChecked)
                {
                    IsChecked = null;
                    IsYChecked = IsNChecked = false;
                }
            }
            finally
            {
                _updating = false;
            }
        }

        /// <summary>
        /// Raised when the user changes the state
        /// </summary>
        public event TypedEventHandler<ADifferentCheckBox, object> Click;


        /// <summary>
        /// Is the "No" button checked
        /// </summary>
        public bool IsNChecked
        {
            get { return (bool)GetValue(IsNCheckedProperty); }
            set { SetValue(IsNCheckedProperty, value); }
        }
        public static readonly DependencyProperty IsNCheckedProperty =
            DependencyProperty.Register("IsNChecked", typeof(bool), typeof(ADifferentCheckBox), 
                new PropertyMetadata(false, (d, _) => (d as ADifferentCheckBox).Update(false)));


        /// <summary>
        /// Is the "X" button checked
        /// </summary>
        public bool IsXChecked
        {
            get { return (bool)GetValue(IsXCheckedProperty); }
            set { SetValue(IsXCheckedProperty, value); }
        }
        public static readonly DependencyProperty IsXCheckedProperty =
            DependencyProperty.Register("IsXChecked", typeof(bool), typeof(ADifferentCheckBox), 
                new PropertyMetadata(true, (d, _) => (d as ADifferentCheckBox).Update(null)));

    }
}
