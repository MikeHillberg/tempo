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

        private void AssemblyClick(object sender, RoutedEventArgs e)
        {
            if (TypeVM?.Assembly != null)
            {
                App.GotoAssembly(TypeVM.Assembly);
            }
        }

        string ToFileOrDirectory(string path, bool toFile)
        {
            // Nupkg paths are of the form:
            // Path/to/package.nupkg!path/to/assembly.dll
            // So first split out the nupkg prefix
            var parts = path.Split("!");
            bool isNupkg = parts.Length > 1;
            if(isNupkg)
            {
                path = parts[1];
            }

            var i = path.LastIndexOf('\\');
            if (i == -1)
            {
                i = path.LastIndexOf('/');
            }
            if (i == -1)
            {
                if (!toFile && isNupkg)
                {
                    return "/" + path;
                }
                return path;
            }

            if(toFile)
                return path.Substring(i + 1);
            else
            {
                var dir = path.Substring(0, i);
                return isNupkg ? "/" + dir : dir;
            }
        }

        string ToFile(string path)
        {
            return ToFileOrDirectory(path, toFile: true);
        }

        string ToDirectory(string path)
        {
            return ToFileOrDirectory(path, toFile: false);
        }

        string ToContainer(string path)
        {
            // Nupkg paths are of the form:
            // Path/to/package.nupkg!path/to/assembly.dll
            var parts = path.Split("!");
            if (parts.Length > 1)
            {
                path = parts[0];
                return Path.GetFileName(path);
            }
            return null;
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

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            App.NavigateToReferencingTypes(this.TypeVM);
        }

        Visibility HasAssemblyViewModel(TypeViewModel typeVM)
        {
            return typeVM?.Assembly != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
