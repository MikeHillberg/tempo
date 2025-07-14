using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Tempo
{
    abstract public class MySerializableControl : Page
    {
        public MySerializableControl()
        {
            Loaded += OnLoaded;
        }
        bool _activated = false;

        private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (!_activated)
            {
                DoActivate(null);
            }
        }

        protected override sealed void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                DoSuspend();
            }
        }

        abstract protected void OnActivated(object parameter);
        abstract protected object OnSuspending();
        abstract protected void OnReactivated(object parameter, object state);

        public void DoActivate(object parameter)
        {
            _activated = true;


            OnActivated(parameter);
        }

        public void DoSuspend()
        {
            var state = OnSuspending();
            App.NavigationStateStack.Push(state);
        }

        public void DoResume(object parameter)
        {
            IsRoot = true;
            _activated = true;
            OnReactivated(parameter, App.NavigationStateStack.Pop());
        }


        protected override sealed void OnNavigatedTo(NavigationEventArgs e)
        {
            DebugLog.Append($"Navigated to {this.GetType().Name}");

            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                DoActivate(e.Parameter);
                IsRoot = true;
            }
            else
            {
                Debug.Assert(e.NavigationMode == NavigationMode.Back);
                DoResume(e.Parameter);
            }
        }

        protected override sealed void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }


        public bool TryNavigateBack()
        {
            return OnNavigateBack();
        }

        protected virtual bool OnNavigateBack()
        {
            return false;
        }


        /// <summary>
        /// Indicates that this is at the root of the content
        /// </summary>
        /// (Is this a dupe of this.Frame != null?)
        public bool IsRoot
        {
            get { return (bool)GetValue(IsRootProperty); }
            set { SetValue(IsRootProperty, value); }
        }
        public static readonly DependencyProperty IsRootProperty =
            DependencyProperty.Register("IsRoot", typeof(bool), typeof(MySerializableControl), new PropertyMetadata(false));

        /// <summary>
        /// Indicates that this is the full content area (both panes) of SearchResults
        /// </summary>
        public bool IsFullSearchContent
        {
            get { return (bool)GetValue(IsFullSearchContentProperty); }
            set { SetValue(IsFullSearchContentProperty, value); }
        }
        public static readonly DependencyProperty IsFullSearchContentProperty =
            DependencyProperty.Register("IsFullSearchContent", typeof(bool), typeof(MySerializableControl), new PropertyMetadata(false));



        /// <summary>
        /// Allows on the documentation pane to be shown
        /// </summary>
        public bool CanShowDocPane
        {
            get { return (bool)GetValue(CanShowDocPaneProperty); }
            set { SetValue(CanShowDocPaneProperty, value); }
        }
        public static readonly DependencyProperty CanShowDocPaneProperty =
            DependencyProperty.Register("CanShowDocPane", typeof(bool), typeof(MySerializableControl), new PropertyMetadata(true));



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

        /// <summary>
        /// Indicates that this is the second pane of the SearchResults content area
        /// </summary>
        public bool IsSecondSearchPane
        {
            get { return (bool)GetValue(IsSecondSearchPaneProperty); }
            set { SetValue(IsSecondSearchPaneProperty, value); }
        }
        public static readonly DependencyProperty IsSecondSearchPaneProperty =
            DependencyProperty.Register("IsSecondSearchPane", typeof(bool), typeof(MySerializableControl), 
                new PropertyMetadata(false, (d,dp) => (d as MySerializableControl).IsSecondSearchPaneChanged()));
        void IsSecondSearchPaneChanged()
        {
            //IsNotSecondSearchPane = !IsSecondSearchPane;
        }

        //public bool IsNotSecondSearchPane
        //{
        //    get { return (bool)GetValue(IsNotSecondSearchPaneProperty); }
        //    private set { SetValue(IsNotSecondSearchPaneProperty, value); }
        //}
        //public static readonly DependencyProperty IsNotSecondSearchPaneProperty =
        //    DependencyProperty.Register("IsNotSecondSearchPane", typeof(bool), typeof(MySerializableControl), new PropertyMetadata(true));


    }

}
