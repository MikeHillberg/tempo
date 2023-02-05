// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    public sealed partial class Smokey : UserControl
    {
        public Smokey()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When set, show a progress ring
        /// </summary>
        public bool InProgress
        {
            get { return (bool)GetValue(InProgressProperty); }
            set { SetValue(InProgressProperty, value); }
        }
        public static readonly DependencyProperty InProgressProperty =
            DependencyProperty.Register("InProgress", typeof(bool), typeof(Smokey), 
                new PropertyMetadata(true));

        /// <summary>
        /// When set, show a semi-transparent gray box
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(Smokey), 
                new PropertyMetadata(false));

    }
}
