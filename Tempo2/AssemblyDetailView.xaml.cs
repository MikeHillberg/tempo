using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Tempo
{
    public sealed partial class AssemblyDetailView : MySerializableControl
    {
        public AssemblyDetailView()
        {
            this.InitializeComponent();
        }

        protected override void OnActivated(object parameter)
        {
            AssemblyVM = parameter as AssemblyViewModel;
            if (AssemblyVM == null) return;

            try
            {
                ReferenceNames = AssemblyVM.ReferencedAssemblies
                    .Select(r => $"{r.Name}, Version={r.Version}")
                    .ToList();
            }
            catch (Exception ex)
            {
                UnhandledExceptionManager.ProcessException(ex);
                ReferenceNames = new List<string>();
            }

            try
            {
                AttributeInfos = AttributeTypeInfo.WrapCustomAttributes(AssemblyVM.CustomAttributes).ToList();
            }
            catch (Exception ex)
            {
                UnhandledExceptionManager.ProcessException(ex);
                AttributeInfos = new List<AttributeTypeInfo>();
            }
        }

        protected override object OnSuspending()
        {
            return null;
        }

        protected override void OnReactivated(object parameter, object state)
        {
            OnActivated(parameter);
        }

        public AssemblyViewModel AssemblyVM
        {
            get { return (AssemblyViewModel)GetValue(AssemblyVMProperty); }
            set { SetValue(AssemblyVMProperty, value); }
        }
        public static readonly DependencyProperty AssemblyVMProperty =
            DependencyProperty.Register("AssemblyVM", typeof(AssemblyViewModel), typeof(AssemblyDetailView), new PropertyMetadata(null));

        public List<string> ReferenceNames
        {
            get { return (List<string>)GetValue(ReferenceNamesProperty); }
            set { SetValue(ReferenceNamesProperty, value); }
        }
        public static readonly DependencyProperty ReferenceNamesProperty =
            DependencyProperty.Register("ReferenceNames", typeof(List<string>), typeof(AssemblyDetailView), new PropertyMetadata(null));

        public List<AttributeTypeInfo> AttributeInfos
        {
            get { return (List<AttributeTypeInfo>)GetValue(AttributeInfosProperty); }
            set { SetValue(AttributeInfosProperty, value); }
        }
        public static readonly DependencyProperty AttributeInfosProperty =
            DependencyProperty.Register("AttributeInfos", typeof(List<AttributeTypeInfo>), typeof(AssemblyDetailView), new PropertyMetadata(null));

        string FormatFullName() => AssemblyVM?.FullName ?? "";
        string FormatVersion() => AssemblyVM?.VersionString ?? "";
        Visibility HasVersion()
        {
            return !string.IsNullOrEmpty(AssemblyVM?.VersionString) ? Visibility.Visible : Visibility.Collapsed;
        }
        string FormatCulture() => AssemblyVM?.CultureDisplay ?? "";
        Visibility HasCulture()
        {
            return !string.IsNullOrEmpty(AssemblyVM?.CultureDisplay) ? Visibility.Visible : Visibility.Collapsed;
        }
        string FormatPublicKeyToken()
        {
            var token = AssemblyVM?.PublicKeyToken;
            return !string.IsNullOrEmpty(token) ? token : "";
        }
        Visibility HasPublicKeyToken()
        {
            return !string.IsNullOrEmpty(AssemblyVM?.PublicKeyToken) ? Visibility.Visible : Visibility.Collapsed;
        }
        string FormatTargetFramework()
        {
            var tf = AssemblyVM?.TargetFramework;
            return !string.IsNullOrEmpty(tf) ? tf : "";
        }
        Visibility HasTargetFramework()
        {
            return !string.IsNullOrEmpty(AssemblyVM?.TargetFramework) ? Visibility.Visible : Visibility.Collapsed;
        }
        string FormatLocation() => AssemblyVM?.Location ?? "";
        Visibility HasLocation()
        {
            return !string.IsNullOrEmpty(AssemblyVM?.Location) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
