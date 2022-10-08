using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tempo
{
    public sealed partial class AttributesView2 : UserControl, ICanBeEmpty
    {
        public AttributesView2()
        {
            this.InitializeComponent();
        }

        public IEnumerable<AttributeTypeInfo> AttributeVMs
        {
            get { return (IEnumerable<AttributeTypeInfo>)GetValue(AttributeVMsProperty); }
            set { SetValue(AttributeVMsProperty, value); }
        }
        public static readonly DependencyProperty AttributeVMsProperty =
            DependencyProperty.Register("AttributeVMs", typeof(IEnumerable<CustomAttributeViewModel>), typeof(AttributesView2),
                new PropertyMetadata(null, (d, dp) => (d as AttributesView2).Changed?.Invoke(d, null)));

        public bool IsEmpty
        {
            get
            {
                return AttributeVMs == null || AttributeVMs.FirstOrDefault() == null;
            }
        }

        public event EventHandler<EventArgs> Changed;
    }
}
