using App1.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using App1.Helper;
using static App1.Win32;
using WASDK = Microsoft.WindowsAppSDK;
using App1.Common;
using Microsoft.Windows.AppLifecycle;
using System.Threading.Tasks;
using App1.Data;
using WinUIGallery.DesktopWap.DataModel;
using WinUIGallery;
using System.Reflection;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{


    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window m_window;
        private static Window startupWindow;

        private static Win32WindowHelper win32WindowHelper;
        private static int registeredKeyPressedHook = 0;
        private HookProc keyEventHook;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            

            //初始化页面窗口
            IdleSynchronizer.Init();
            //创建活动窗口页面
            startupWindow = WindowHelper.CreateWindow();

            //取消应用程序的标题栏
            startupWindow.ExtendsContentIntoTitleBar = true;

            //将创建的窗口交给窗口辅助程序
            win32WindowHelper = new Win32WindowHelper(startupWindow);

            //设置窗口初始
            win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT() { x = 500, y = 500 });

            #if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.BindingFailed += DebugSettings_BindingFailed;
            }
            #endif

            //
            keyEventHook = new HookProc(KeyEventHook);

            //挂载windows案件功能
            registeredKeyPressedHook = SetWindowKeyHook(keyEventHook);

            //呈现自定义窗口
            EnsureWindow();
        }


        public static string WinAppSdkDetails
        {
            // TODO: restore patch number and version tag when WinAppSDK supports them both
            get => string.Format("Windows App SDK {0}.{1}",
            WASDK.Release.Major, WASDK.Release.Minor);

        }

        private int KeyEventHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsKeyDownHook(lParam))
            {
                RootFrameNavigationHelper.RaiseKeyPressed((uint)wParam);
            }

            return CallNextHookEx(registeredKeyPressedHook, nCode, wParam, lParam);
        }


        private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
        {

        }

        private async void EnsureWindow(IActivatedEventArgs args = null)
        {
            // 加载程序数据
            await ControlInfoDataSource.Instance.GetGroupsAsync();
            await IconsDataSource.Instance.LoadIcons();

            //将预设窗口设置成MainTest，并获取他的菜单选项
            Frame rootFrame = GetRootFrame();

            //设置界面风格
            ThemeHelper.Initialize();

            //获取主界面
            Type targetPageType = typeof(HomePage);

            string targetPageArguments = string.Empty;

            if (args != null)
            {
                if (args.Kind == ActivationKind.Launch)
                {
                    if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        try
                        {
                            await SuspensionManager.RestoreAsync();
                        }
                        catch (SuspensionManagerException)
                        {
                            //Something went wrong restoring state.
                            //Assume there is no state and continue
                        }
                    }

                    targetPageArguments = ((Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)args).Arguments;
                }
            }

            var eventargs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            //处理事件激活
            if (eventargs != null && eventargs.Kind is ExtendedActivationKind.Protocol && eventargs.Data is ProtocolActivatedEventArgs)
            {
                //通过http激活程序
                ProtocolActivatedEventArgs ProtocolArgs = eventargs.Data as ProtocolActivatedEventArgs;
                var uri = ProtocolArgs.Uri.LocalPath.Replace("/", "");

                targetPageArguments = uri;
                string targetId = string.Empty;

                if (uri == "AllControls")
                {
                    targetPageType = typeof(AllControlsPage);
                }
                else if (uri == "NewControls")
                {
                    targetPageType = typeof(HomePage);
                }
                else if (ControlInfoDataSource.Instance.Groups.Any(g => g.UniqueId == uri))
                {
                    targetPageType = typeof(SectionPage);
                }
                else if (ControlInfoDataSource.Instance.Groups.Any(g => g.Items.Any(i => i.UniqueId == uri)))
                {
                    targetPageType = typeof(ItemPage);
                }
            }


            MainTest rootPage = startupWindow.Content as MainTest;

            //将菜单栏位加载为homepage
            rootPage.Navigate(targetPageType, targetPageArguments);

            if (targetPageType == typeof(HomePage))
            {
                ((Microsoft.UI.Xaml.Controls.NavigationViewItem)((MainTest)App.startupWindow.Content).NavigationView.MenuItems[0]).IsSelected = true;
            }

            // Ensure the current window is active
            startupWindow.Activate();
        }


        public Frame GetRootFrame()
        {
            Frame rootFrame;

            //将预设窗口转换成主窗口
            MainTest rootPage = startupWindow.Content as MainTest;

            if (rootPage == null)
            {
                //实例化主窗口
                rootPage = new MainTest();

                //菜单选项根节点
                rootFrame = (Frame)rootPage.FindName("rootFrame");
                if (rootFrame == null)
                {
                    throw new Exception("Root frame not found");
                }

                //注册菜单
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                //语言选项
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];


                rootFrame.NavigationFailed += OnNavigationFailed;

                //设置窗口的界面
                startupWindow.Content = rootPage;
            }
            else
            {
                rootFrame = (Frame)rootPage.FindName("rootFrame");
            }

            return rootFrame;
        }



        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }


        public static Window StartupWindow
        {
            get
            {
                return startupWindow;
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        public static string WinAppSdkRuntimeDetails
        {
            get
            {
                try
                {
                    // Retrieve Windows App Runtime version info dynamically
                    var windowsAppRuntimeVersion =
                        from module in Process.GetCurrentProcess().Modules.OfType<ProcessModule>()
                        where module.FileName.EndsWith("Microsoft.WindowsAppRuntime.Insights.Resource.dll")
                        select FileVersionInfo.GetVersionInfo(module.FileName);
                    return WinAppSdkDetails + ", Windows App Runtime " + windowsAppRuntimeVersion.First().FileVersion;
                }
                catch
                {
                    return WinAppSdkDetails + $", Windows App Runtime {WASDK.Runtime.Version.Major}.{WASDK.Runtime.Version.Minor}";
                }
            }
        }
    }
}
