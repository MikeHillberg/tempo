// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
                var strings = new List<string>();
                var properties = _vm.GetType().GetProperties().OrderBy(p => p.Name);
                foreach (var property in properties)
                {
                    strings.Add(property.Name);
                    var val = property.GetValue(_vm);
                    if (val == null)
                    {
                        val = "";
                    }

                    // Convert the value into a presentable string (better than what ToString provides)
                    strings.Add(WhereCondition.ToWhereString(val));
                }

                Strings = strings;
            }
        }
    }
}
