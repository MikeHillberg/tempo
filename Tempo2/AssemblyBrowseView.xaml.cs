using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Tempo
{
    public sealed partial class AssemblyBrowseView : MySerializableControl
    {
        public AssemblyBrowseView()
        {
            this.InitializeComponent();
        }

        protected override async void OnActivated(object parameter)
        {
            var shouldContinue = await App.EnsureApiScopeLoadedAsync();
            if (!shouldContinue) return;

            Assemblies = Manager.CurrentTypeSet.Assemblies;
        }

        protected override object OnSuspending() => null;

        protected override void OnReactivated(object parameter, object state)
        {
            OnActivated(parameter);
        }

        public List<AssemblyViewModel> Assemblies
        {
            get { return (List<AssemblyViewModel>)GetValue(AssembliesProperty); }
            set { SetValue(AssembliesProperty, value); }
        }
        public static readonly DependencyProperty AssembliesProperty =
            DependencyProperty.Register("Assemblies", typeof(List<AssemblyViewModel>), typeof(AssemblyBrowseView), new PropertyMetadata(null));

        public AssemblyViewModel SelectedAssembly
        {
            get { return (AssemblyViewModel)GetValue(SelectedAssemblyProperty); }
            set { SetValue(SelectedAssemblyProperty, value); }
        }
        public static readonly DependencyProperty SelectedAssemblyProperty =
            DependencyProperty.Register("SelectedAssembly", typeof(AssemblyViewModel), typeof(AssemblyBrowseView), 
                new PropertyMetadata(null, OnSelectedAssemblyChanged));

        private static void OnSelectedAssemblyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AssemblyBrowseView)d;
            var selected = e.NewValue as AssemblyViewModel;
            view.UpdateDerivedProperties(selected);
        }

        private void UpdateDerivedProperties(AssemblyViewModel selected)
        {
            if (selected == null)
            {
                ReferenceNames = null;
                AttributeInfos = null;
                return;
            }

            ReferenceNames = selected.ReferencedAssemblies
                .Select(r => $"{r.Name}, Version={r.Version}")
                .ToList();

            AttributeInfos = AttributeTypeInfo.WrapCustomAttributes(selected.CustomAttributes).ToList();
        }

        public List<string> ReferenceNames
        {
            get { return (List<string>)GetValue(ReferenceNamesProperty); }
            set { SetValue(ReferenceNamesProperty, value); }
        }
        public static readonly DependencyProperty ReferenceNamesProperty =
            DependencyProperty.Register("ReferenceNames", typeof(List<string>), typeof(AssemblyBrowseView), new PropertyMetadata(null));

        public List<AttributeTypeInfo> AttributeInfos
        {
            get { return (List<AttributeTypeInfo>)GetValue(AttributeInfosProperty); }
            set { SetValue(AttributeInfosProperty, value); }
        }
        public static readonly DependencyProperty AttributeInfosProperty =
            DependencyProperty.Register("AttributeInfos", typeof(List<AttributeTypeInfo>), typeof(AssemblyBrowseView), new PropertyMetadata(null));
    }
}
