using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tempo
{
    /// <summary>
    /// Show custom attributes in a list
    /// </summary>
    public sealed partial class AttributesView2 : UserControl, ICanBeEmpty
    {
        public AttributesView2()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// An AttributeTypeInfo is a wrapper around an AttributeViewModel
        /// </summary>
        public IEnumerable<AttributeTypeInfo> AttributeTypeInfos
        {
            get { return (IEnumerable<AttributeTypeInfo>)GetValue(AttributeTypeInfosProperty); }
            set { SetValue(AttributeTypeInfosProperty, value); }
        }
        public static readonly DependencyProperty AttributeTypeInfosProperty =
            DependencyProperty.Register("AttributeTypeInfos", typeof(IEnumerable<AttributeTypeInfo>), typeof(AttributesView2),
                new PropertyMetadata(null, (d, dp) => (d as AttributesView2).IsEmptyChanged?.Invoke(d, null)));

        // ICanBeEmpty.IsEmpty
        public bool IsEmpty
        {
            get
            {
                return AttributeTypeInfos == null || AttributeTypeInfos.FirstOrDefault() == null;
            }
        }

        // ICanBeEmpty.Changed
        public event EventHandler<EventArgs> IsEmptyChanged;
    }
}
