using Microsoft.UI.Xaml;
using WinRT;

using System.Runtime.InteropServices; // For DllImport
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using System.IO.Packaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        static internal MainWindow Instance { get; private set; }

        public MainWindow()
        {
            Instance = this;
            this.InitializeComponent();

            SetWindowIcon();

            App.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(App.ApiScopeName))
                {
                    UpdateWindowTitle();
                }
            };

            ApiScopeLoader.ApiScopeInfoChanged += (s, e) =>
            {
                // Something about the ApiScope changed, e.g. it finished loading
                UpdateWindowTitle();
            };
        }

        string _localVersion = null;

        /// <summary>
        /// Update the window title with the current selection info
        /// </summary>
        void UpdateWindowTitle()
        {
            var title = $"Tempo - {App.Instance.ApiScopeName}";

            if (App.Instance.IsWinPlatformScope)
            {
                _localVersion = _localVersion ?? GetLocalVersion();
                title += $" ({_localVersion})";
            }
            else if (App.Instance.IsWinAppScope && Manager.WindowsAppTypeSet != null)
            {
                var version = Manager.WindowsAppTypeSet.Version.Split(",")[1];
                title += $" ({version})";
            }
            else if (App.Instance.IsCustomApiScope && Manager.CustomMRTypeSet != null)
            {
                title = $"Tempo - {Manager.CustomMRTypeSet.Version}";
            }
            else if (App.Instance.IsWin32Scope && Manager.Win32TypeSet != null)
            {
                title += $" ({Manager.Win32TypeSet.Version})";
            }
            else if (App.Instance.IsWebView2Scope && Manager.WebView2TypeSet != null)
            {
                title += $" ({Manager.WebView2TypeSet.Version})";
            }
            else if (App.Instance.IsDotNetScope && Manager.DotNetTypeSet != null
                || App.Instance.IsDotNetWindowsScope && Manager.DotNetWindowsTypeSet != null)
            {
                title += $" ({DotNetTypeSet.DotNetCoreVersion})";
            }

            Title = title;
        }

        string GetLocalVersion()
        {
            DesktopManager2.GetLocalWindowsVersion(out var displayVersion, out var currentBuildNumber, out var ubr);
            return $"{displayVersion}, {currentBuildNumber}.{ubr}";
        }

        // Helpers for SetMicaBackrop
        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        bool _isMicaSet = false;


        /// <summary>
        /// Set an ICO to the AppWindow
        /// </summary>
        async void SetWindowIcon()
        {
            // This call is really slow, so don't wait on it
            var installedPath = await Task.Run<string>(() => Windows.ApplicationModel.Package.Current.InstalledLocation.Path);

            // Get the AppWindow
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Set the icon
            // Used https://www.freeconvert.com/png-to-ico
            appWindow.SetIcon(Path.Combine(installedPath, "Assets/Icon.ico"));
        }

        /// <summary>
        /// Set Mica as the Window backdrop, if possible
        /// </summary>
        internal void SetMicaBackdrop()
        {
            // With this set, portion of the Window content that isn't opaque will see
            // Mica. So the search results pane is transparent, allowing this to show through.
            // On Win10 this isn't supported, so the background will just be the default backstop

            // Gets called by Loaded, running twice isn't good
            if (_isMicaSet)
            {
                return;
            }
            _isMicaSet = true;

            // Not supported on Win10
            if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                return;
            }


            // Rest of the code is copied from WinUI Gallery
            // https://github.com/microsoft/WinUI-Gallery/blob/260cb720ef83b3d134bc4805cffcfac9461dce33/WinUIGallery/SamplePages/SampleSystemBackdropsWindow.xaml.cs


            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
            this.Activated += Window_Activated;
            this.Closed += Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

            // Enable the system backdrop.
            m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }
    }


    /// <summary>
    /// Helper for use with SetMicaBackrop
    /// </summary>
    /// 
    class WindowsSystemDispatcherQueueHelper
    {
        // This class opied from WinUI Gallery
        // https://github.com/microsoft/WinUI-Gallery/blob/260cb720ef83b3d134bc4805cffcfac9461dce33/WinUIGallery/SamplePages/SampleSystemBackdropsWindow.xaml.cs


        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }

}
