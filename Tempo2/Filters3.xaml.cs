using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tempo
{
    /// <summary>
    /// Show all the different search settings and let them be configured
    /// </summary>
    public sealed partial class Filters3 : UserControl
    {
        public Filters3()
        {
            this.InitializeComponent();

            // _tabbedSettings has all the settings organized into groups and the
            // groups into tabs. Get a flat version of all the leaf settings to support the search view

            foreach (var tab in _tabbedSettings)
            {
                foreach (var group in tab)
                {
                    foreach (var setting in group)
                    {
                        _flatSettingViewList.Add(new FlatSettingView()
                        {
                            Tab = tab.Name,
                            Group = group.Section,
                            SettingView = setting
                        });
                    }
                }
            }

            // Don't need this anymore, but saving it because it took a minute to figure out
            // This puts the flat list back into groups and tabs, the idea is that there'd be a where
            // clause in here to create a new hierarchy from a subsetted list.
            //
            //FilteredTabs = from setting in _flatSettingViewList
            //               group setting by setting.Tab into tab
            //               select new FilteredTab()
            //               {
            //                   TabName = tab.Key,
            //                   Groups = from setting in tab
            //                            group setting by setting.Group into grp
            //                            select new FilteredGroup
            //                            {
            //                                GroupName = grp.Key,
            //                                Settings = from g in grp select g.SettingView
            //                            }

            //               };

        }


        // The SettingsViews are defined in a hierarchy. This is a flat version of it, used for searching.
        List<FlatSettingView> _flatSettingViewList = new List<FlatSettingView>();

        // Forwarder to Manager.Settings to make x:Bind easier to type out
        public Settings Settings { get { return Manager.Settings; } }

        private void _groupListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure there's always something selected
            var listView = sender as ListView;
            if (listView.SelectedIndex == -1)
            {
                listView.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// List of all the settings that match the search string
        /// </summary>
        public IEnumerable<IGrouping<string,SettingViewBase>> FilteredSettings
        {
            get { return (IEnumerable<IGrouping<string, SettingViewBase>>)GetValue(FilteredSettingsProperty); }
            set { SetValue(FilteredSettingsProperty, value); }
        }
        public static readonly DependencyProperty FilteredSettingsProperty =
            DependencyProperty.Register("FilteredSettings", typeof(IEnumerable<IGrouping<string, SettingViewBase>>), typeof(Filters3), new PropertyMetadata(null));



        /// <summary>
        /// When the filter search text box is invoked, update FilteredSettings property
        /// </summary>
        private void TimedTextBox_Invoked(object sender, EventArgs e)
        {
            var search = _search.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(search))
            {
                FilteredSettings = null;
            }

            FilteredSettings = from s in _flatSettingViewList
                                where s.SettingView.Label.ToUpper().Contains(search)
                                    || s.SettingView.Description.ToUpper().Contains(search)
                                    || s.Group.ToUpper().Contains(search)
                                    || s.Tab.ToUpper().Contains(search)
                                group s.SettingView by s.Group;
        }

        /// <summary>
        /// When the selection changes on a setting combobox, write the new selected
        /// value back to the ChoiceSettingView
        /// </summary>
        // bugbug (cbsvp): Should be able to use SelectedViewPath and a two-way bind
        // on SelectedView, but I've not been able to get that to work
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var setting = comboBox.Tag as ChoiceSettingView;

            // The selection is set during the Loaded event, no need to do anything in that case
            if (_isInLoadedEvent)
            {
                return;
            }

            // If DisplayMemberPath isn't set, it means there's no SelectedValuePath either,
            // so just write the value back to the data source
            if (string.IsNullOrEmpty(setting.DisplayMemberPath))
            {
                setting.SelectedValue = comboBox.SelectedValue;
            }
            else
            {
                // DisplayMemberPath is set. That means that the items are
                // the NameValue type
                setting.SelectedValue = (comboBox.SelectedItem as NameValue).Value;
            }
        }

        bool _isInLoadedEvent = false;

        /// <summary>
        /// When a ChoiceSettingView combobox is loaded, update the selected value
        /// </summary>
        // bugbug (cbsvp)
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Track that the next selection event will be from this initialization
            _isInLoadedEvent = true;
            try
            {
                var comboBox = sender as ComboBox;
                var setting = comboBox.Tag as ChoiceSettingView;

                // If there's no DisplayMemberPath, there's no SelectedValuePath,
                // so we can just select the value and be done
                if (string.IsNullOrEmpty(setting.DisplayMemberPath))
                {
                    comboBox.SelectedValue = setting.SelectedValue;
                    return;
                }

                // Find the item and select it
                foreach (var item in comboBox.Items)
                {
                    var value = (item as NameValue).Value;
                    if ((value as IComparable).CompareTo(setting.SelectedValue) == 0)
                    {
                        comboBox.SelectedItem = item;
                        return;
                    }
                }
            }
            finally
            {
                _isInLoadedEvent = false;
            }
        }

        private void BooleanSettingView_Setted(object sender, EventArgs e)
        {
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ((sender as FrameworkElement).DataContext as BooleanSettingView).RaiseClick();
        }

        private void ToggleTypeFilter(object sender, BooleanSettingView e)
        {
            App.ToggleTypeFilter();
        }
        private void TogglePropertyFilter(object sender, BooleanSettingView e)
        {
            App.TogglePropertyFilter();
        }
        private void ToggleMethodFilter(object sender, BooleanSettingView e)
        {
            App.ToggleMethodFilter();
        }
        private void ToggleEventFilter(object sender, BooleanSettingView e)
        {
            App.ToggleEventFilter();
        }
        private void ToggleFieldFilter(object sender, BooleanSettingView e)
        {
            App.ToggleFieldFilter();
        }
        private void ToggleConstructorFilter(object sender, BooleanSettingView e)
        {
            App.ToggleConstructorFilter();
        }

    }

    // bugbug: has to be a DO for two-way data bindings
    public class SettingViewBase : DependencyObject
    {
        public string Label { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string GroupName { get; set; } = String.Empty;
    }

    public class ChoiceSettingView : SettingViewBase
    {
        //Label="Trust level"
        //Description="Restrict to one trust level"
        //SelectedValue="{Binding Settings.TrustLevel, Mode=TwoWay}" 
        //SelectedValuePath="Value"
        //ItemsSource="{Binding Settings.TrustLevelValues}"  
        //DisplayMemberPath="Name"/>



        public object SelectedValue
        {
            get { return (object)GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(object), typeof(ChoiceSettingView), new PropertyMetadata(null));

        public string SelectedValuePath { get; set; }
        public string DisplayMemberPath { get; set; }
        public object ItemsSource { get; set; }


    }

    public class BooleanSettingView : SettingViewBase
    {
        // bugbug
        // Error WMC1118 TwoWay binding target 'Setting' must be a dependency property 
        public bool? Setting
        {
            get { return (bool?)GetValue(SettingProperty); }
            set
            {
                // Bugbug
                // This goes into an infinite recursion of change notifications
                // Tried putting a workaround here of comparing current value to 'value', but
                // current value is unchanged from lower on the stack. It's like
                // in the SetValue call it's sending the change notification before updating the value
                if (_currentSettingHack != value)
                {
                    _currentSettingHack = value;
                    SetValue(SettingProperty, value);
                }
            }
        }
        public static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register("Setting", typeof(bool?), typeof(SettingViewBase), 
                new PropertyMetadata(null, (d,dp) => (d as BooleanSettingView).SettingChanged()));

        public event EventHandler<BooleanSettingView> Click;
        public void RaiseClick()
        {
            Click?.Invoke(this, null);
        }

        void SettingChanged()
        {
            if(Setting == true)
            {
                Setted?.Invoke(this, null);
            }
        }
        internal event EventHandler Setted;

        bool? _currentSettingHack = null;

        public bool IsThreeState { get; set; }
    }

    public class FilteredSettingsTab
    {
        public string TabName { get; set; }
        internal IEnumerable<FilteredSettingsGroup> Groups { get; set; }
    }

    public class FilteredSettingsGroup
    {
        public string GroupName { get; set; }
        public IEnumerable<SettingViewBase> Settings { get; set; }
    }

    /// <summary>
    /// A SettingViewBase plus its Tab and Group name
    /// </summary>
    public class FlatSettingView
    {
        public string Tab { get; set; }
        public string Group { get; set; }
        public SettingViewBase SettingView { get; set; }
    }

    public class MyRibbonTabs : List<MyRibbonTab>
    {

    }

    public class MyRibbonGroup : List<object>, IGrouping<string, object>
    {
        public string Section { get; set; }
        string IGrouping<string, object>.Key => Section;

    }


    public class MyRibbonTab : List<MyRibbonGroup2>
    {
        public string Name { get; set; }
    }

    public class MyRibbonGroup2 : List<SettingViewBase>
    {
        public string Section { get; set; }

    }


    public class SettingVewTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is BooleanSettingView)
            {
                return BooleanTemplate;
            }
            else
            {
                Debug.Assert(item is ChoiceSettingView);
                return ChoiceTemplate;
            }
        }

        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate ChoiceTemplate { get; set; }

    }





}
