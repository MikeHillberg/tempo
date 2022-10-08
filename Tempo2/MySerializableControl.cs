using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            IsTopLevelChanged();
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

            IsRoot = Frame != null;

            OnActivated(parameter);
        }

        public void DoSuspend()
        {
            var state = OnSuspending();
            App.NavigationStateStack.Push(state);
        }

        public void DoResume(object parameter)
        {
            IsRoot = Frame != null;
            _activated = true;
            OnReactivated(parameter, App.NavigationStateStack.Pop());
        }


        protected override sealed void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
                DoActivate(e.Parameter);
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



        public bool IsRoot
        {
            get { return (bool)GetValue(IsRootProperty); }
            set { SetValue(IsRootProperty, value); }
        }
        public static readonly DependencyProperty IsRootProperty =
            DependencyProperty.Register("IsRoot", 
                typeof(bool), typeof(MySerializableControl), 
                new PropertyMetadata(false));



        public bool IsTopLevel
        {
            get { return (bool)GetValue(IsTopLevelProperty); }
            set { SetValue(IsTopLevelProperty, value); }
        }
        public static readonly DependencyProperty IsTopLevelProperty =
            DependencyProperty.Register("IsTopLevel", typeof(bool), typeof(MySerializableControl), 
                new PropertyMetadata(false, (s,e) => (s as MySerializableControl).IsTopLevelChanged()));
        protected virtual void IsTopLevelChanged()
        {
        }

    }

}
