// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Tempo
{
    /// <summary>
    /// Control to display all properties of a MemberOrTypeViewModelBase
    /// </summary>
    public sealed partial class AllModelProperties : UserControl
    {
        public AllModelProperties()
        {
            this.InitializeComponent();
        }

        // All cell strings in a flat list
        public IEnumerable<string> Strings
        {
            get { return (IEnumerable<string>)GetValue(StringsProperty); }
            set { SetValue(StringsProperty, value); }
        }
        public static readonly DependencyProperty StringsProperty =
            DependencyProperty.Register("Strings", typeof(IEnumerable<string>), typeof(AllModelProperties),
                new PropertyMetadata(null));


        MemberOrTypeViewModelBase _vm;

        /// <summary>
        /// The ViewModel for which we'll display all properties
        /// </summary>
        public MemberOrTypeViewModelBase VM
        {
            get { return _vm; }
            set
            {
                _vm = value;
                Strings = null;

                if (_vm == null)
                {
                    return;
                }

                // Get all the properties and values, as strings, and put into a flat list
                var properties = _vm.GetType().GetProperties().OrderBy(p => p.Name);
                _propertyValues = new List<string>();
                _propertyNames = new List<string>();
                foreach (var property in properties)
                {
                    _propertyNames.Add(property.Name);
                    var val = property.GetValue(_vm);
                    if (val == null)
                    {
                        val = "";
                    }

                    // Convert the value into a presentable string (better than what ToString provides)
                    _propertyValues.Add(WhereCondition.ToWhereString(val));
                }

                // Set the Strings property
                UpdateStrings();

            }
        }

        IList<string> _propertyValues;
        IList<string> _propertyNames;

        /// <summary>
        /// Calculate the Strings property, taking Filters into account
        /// </summary>
        void UpdateStrings()
        {
            var filter = Filter;
            var strings = new List<string>();
            for(int i = 0; i < _propertyNames.Count; i++)
            {
                var name = _propertyNames[i];
                var val = _propertyValues[i];

                if(string.IsNullOrEmpty(filter) 
                    || name.Contains(filter, StringComparison.OrdinalIgnoreCase) 
                    || val.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    strings.Add(_propertyNames[i]);
                    strings.Add(_propertyValues[i]);
                }
            }

            Strings = strings;
        }


        /// <summary>
        /// Filter to be applied to Strings (to search the list)
        /// </summary>
        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(AllModelProperties), 
                new PropertyMetadata(null, (d,_) => (d as AllModelProperties).UpdateStrings()));
    }
}
