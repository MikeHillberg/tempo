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
using System.Diagnostics;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class TypeDetailViewTypeInfo : UserControl
    {
        public TypeDetailViewTypeInfo()
        {
            this.InitializeComponent();
        }



        public TypeViewModel TypeVM
        {
            get { return (TypeViewModel)GetValue(TypeVMProperty); }
            set { SetValue(TypeVMProperty, value); }
        }
        public static readonly DependencyProperty TypeVMProperty =
            DependencyProperty.Register("TypeVM", typeof(TypeViewModel), typeof(TypeDetailViewTypeInfo),
                new PropertyMetadata(null, (d, dp) => (d as TypeDetailViewTypeInfo).TypeVMChanged()));

        void TypeVMChanged()
        {
        }

        private void NamespaceClick(object sender, RoutedEventArgs e)
        {
            App.GotoNamespaces(TypeVM.Namespace);
        }

        string ToFileOrDirectory(string path, bool toFile)
        {

            var i = path.LastIndexOf('\\');
            if (i == -1)
            {
                i = path.LastIndexOf('/');
            }
            if (i == -1)
            {
                return path;
            }

            if(toFile)
                return path.Substring(i + 1);
            else
                return path.Substring(0, i);
        }

        string ToFile(string path)
        {
            return ToFileOrDirectory(path, toFile: true);
        }

        string ToDirectory(string path)
        {
            return ToFileOrDirectory(path, toFile: false);
        }

        private void ShowAllModelProperties_Click(object sender, RoutedEventArgs e)
        {
            AllModelPropertiesPage.ShowWindow(TypeVM);
        }

        /// <summary>
        /// Be visible if the type VM has some kind of version
        /// </summary>
        Visibility HasVersion(TypeViewModel typeVM)
        {
            if(typeVM == null)
            {
                return Visibility.Collapsed;
            }

            if(!string.IsNullOrEmpty(TypeVM.Contract)
                || !string.IsNullOrEmpty(TypeVM.UwpBuild)
                || !string.IsNullOrEmpty(TypeVM.VersionFriendlyName))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }
    }
}
