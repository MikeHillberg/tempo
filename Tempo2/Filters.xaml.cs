using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace Tempo
{
    /// <summary>
    /// Old UI for search filters
    /// </summary>
    public sealed partial class Filters : Page, INotifyPropertyChanged
    {
        public Filters()
        {
            Application.Current.DebugSettings.IsBindingTracingEnabled = true;

            this.InitializeComponent();

            Loaded += Filters_Loaded;

            KeyDown += (s, e) =>
            {
                // If we navigated here, rather than being shown in a flyout, then navigate back on Escape key
                if (e.Key == VirtualKey.Escape && !IsFlyoutMode)
                {
                    App.GoBack();
                }
            };

            Settings.Changed += (s, e) =>
            {
                // Reinitialize everything if Settings is reset

                if (string.IsNullOrEmpty(e.PropertyName))
                {
                    Initialize();
                    HomePage.AdaptiveSettings.UpdateFilterBackground();
                    RaisePropertyChanged(nameof(Settings));
                }
            };
        }

        static public int ShowCount = 0;

        private void Filters_Loaded(object sender, RoutedEventArgs e)
        {
            // This can't be in the constructor because the constructor runs before we navigate
            // away from the previous page.
            // bugbug: really?
            Initialize();

            ShowCount++;
        }

        private void Initialize()
        {
            // bugbug: can't find any way of initializing SelectedValue to make it have a default selection.
            _typeKindComboBox.SelectedIndex = (int)Manager.Settings.TypeKind;
            _memberKindComboBox.SelectedIndex = (int)Manager.Settings.MemberKind;
        }


        public string ResetButtonText
        {
            get
            {
                var present = new KeyboardCapabilities().KeyboardPresent;

                // bugbug: raise notification if a keyboard is attached/detached
                if (present == 0)
                    return "Reset";
                else
                    return "Reset (F3)";
            }
        }



        public IList<object> Namespaces { get { return App.Namespaces; } }

        public Settings Settings { get { return Manager.Settings; } }
        
        void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;


        // This is set if the filters are being shown in a flyout (as opposed to being a page that's navigated to)
        public bool IsFlyoutMode
        {
            get { return (bool)GetValue(IsFlyoutModeProperty); }
            set { SetValue(IsFlyoutModeProperty, value); }
        }
        public static readonly DependencyProperty IsFlyoutModeProperty =
            DependencyProperty.Register("IsFlyoutMode", typeof(bool), typeof(Filters), new PropertyMetadata(false));


        public string SelectedNamespace
        {
            get { return (string)GetValue(SelectedNamespaceProperty); }
            set { SetValue(SelectedNamespaceProperty, value); }
        }
        public static readonly DependencyProperty SelectedNamespaceProperty =
            DependencyProperty.Register("SelectedNamespace", typeof(string), typeof(HomePage),
                new PropertyMetadata("", (s, e) => (s as Filters).SelectedNamespaceChanged()));

        // bugbug:  Bind directly to Manager?
        private void SelectedNamespaceChanged()
        {
            HomePage.AdaptiveSettings.UpdateFilterBackground();
        }

        // bugbug: Can't figure out how to make x:Bind work if this is typed as TypeKind
        public object SelectedTypeKind
        {
            get { return (object)GetValue(SelectedTypeKindProperty); }
            set { SetValue(SelectedTypeKindProperty, value); }
        }
        public static readonly DependencyProperty SelectedTypeKindProperty =
            DependencyProperty.Register("SelectedTypeKind", typeof(object), typeof(HomePage),
                new PropertyMetadata(TypeKind.Any, (d, e) => (d as Filters).SelectedTypeKindChanged()));

        private void SelectedTypeKindChanged()
        {
            if(SelectedTypeKind == null)
            {
                Manager.Settings.TypeKind = TypeKind.Any;
            }
            else
            {
                Manager.Settings.TypeKind = (TypeKind)SelectedTypeKind;
            }
            HomePage.AdaptiveSettings.UpdateFilterBackground();
        }

        public object SelectedMemberKind
        {
            get { return (object)GetValue(SelectedMemberKindProperty); }
            set { SetValue(SelectedMemberKindProperty, value); }
        }
        public static readonly DependencyProperty SelectedMemberKindProperty =
            DependencyProperty.Register("SelectedMemberKind", typeof(object), typeof(HomePage),
                new PropertyMetadata(MemberKind.Any, (d, e) => (d as Filters).SelectedMemberKindChanged()));

        private void SelectedMemberKindChanged()
        {
            // Have to propagate this because the x:Bind can't bind to a strong type here.
            if (SelectedMemberKind == null)
            {
                Settings.MemberKind = MemberKind.Any;
            }
            else
            {
                Settings.MemberKind = (MemberKind)SelectedMemberKind;
            }
            HomePage.AdaptiveSettings.UpdateFilterBackground();
        }

        private void _memberKindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("");
        }

        private void Reset2(object sender, RoutedEventArgs e)
        {
            Manager.Settings = new Settings();
        }
    }

    public class List : List<object>
    {
        public List() { }
    }

    public class Team
    {
        public Team() { }
        public string Name { get; set; }
        public string League { get; set; }
    }

    public class GroupT : List<Team>, IGrouping<string, Team>
    {
        public string League { get; set; }
        string IGrouping<string, Team>.Key => League;

    }

}
