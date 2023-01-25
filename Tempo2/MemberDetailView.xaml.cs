using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;

namespace Tempo
{
    /// <summary>
    /// Shows detail for a member (but not for types)
    /// </summary>
    public sealed partial class MemberDetailView : global::Tempo.MySerializableControl
    {
        public MemberDetailView()
        {
            this.InitializeComponent();
        }


        public MemberViewModelBase MemberVM
        {
            get { return (MemberViewModelBase)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberViewModelBase), typeof(MemberDetailView), 
                new PropertyMetadata(null, (d,_)=>(d as MemberDetailView).MemberVMChanged()));

        void MemberVMChanged()
        {
            if(MemberVM is EventViewModel eventVM)
            {
                EventVM = eventVM;
            }
            else
            {
                EventVM = null;
            }

            MemberKindString = MemberVM?.MemberKind.ToString().ToLower();
        }


        /// <summary>
        /// Member kind as a string, e.g. "property" or "method"
        /// </summary>
        public string MemberKindString
        {
            get { return (string)GetValue(MemberKindStringProperty); }
            set { SetValue(MemberKindStringProperty, value); }
        }
        public static readonly DependencyProperty MemberKindStringProperty =
            DependencyProperty.Register("MemberKindString", typeof(string), typeof(MemberDetailView), new PropertyMetadata(null));

        /// <summary>
        /// Non-null of the MemberVM is an event
        /// </summary>
        public EventViewModel EventVM
        {
            get { return (EventViewModel)GetValue(EventVMProperty); }
            set { SetValue(EventVMProperty, value); }
        }
        public static readonly DependencyProperty EventVMProperty =
            DependencyProperty.Register("EventVM", typeof(EventViewModel), typeof(MemberDetailView), new PropertyMetadata(null));


        protected override void OnActivated(object parameter)
        {
            MemberVM = parameter as MemberViewModelBase;
            App.CurrentItem = MemberVM;
        }
        protected override object OnSuspending()
        {
            return null;
        }
        protected override void OnReactivated(object parameter, object state)
        {
            OnActivated(parameter);
        }
        
        SolidColorBrush _whiteBrush = new SolidColorBrush(Colors.White);

        /// <summary>
        /// Background color to use for the root
        /// </summary>
        public Brush RootBackground
        {
            get
            {
                return IsRoot ? _whiteBrush : null;
            }
        }


        /// <summary>
        /// Indicates that it's not top level content (so use a subtitle font)
        /// </summary>
        public bool IsSubcontent
        {
            get
            {
                return !IsFullSearchContent && !IsRoot;
            }
        }
    }
}
