using App1.Data;
using App1.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.ApplicationSettings;
using Windows.UI.ViewManagement;
using App1;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainTest : Page
    {
        //当前系统线程
        public Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

        private RootFrameNavigationHelper _navHelper;

        public DeviceType DeviceFamily { get; set; }

        private UISettings _settings;

        public Action NavigationViewLoaded { get; set; }

        public MainTest()
        {
            this.InitializeComponent();

            //获取当当前程序工作线程
            dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _navHelper = new RootFrameNavigationHelper(rootFrame, NavigationViewControl);

            //检查当前设备，看是否是windows
            SetDeviceFamily();

            AddNavigationMenuItems();

            this.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                // 对于使用键盘和游戏手柄调试焦点问题很有帮助。
                if (FocusManager.GetFocusedElement() is FrameworkElement focus)
                {
                    Debug.WriteLine("got focus: " + focus.Name + " (" + focus.GetType().ToString() + ")");
                }
            };

            //这句话的意思是，对于使用键盘和游戏手柄进行调试的焦点问题，这个信息或方法是有帮助的
            //当开发应用程序时，确保用户可以通过键盘和游戏手柄轻松导航和操作是很重要的。通过关注这些焦点问题，您可以提高应用程序的可访问性和用户体验。
            Loaded += delegate (object sender, RoutedEventArgs e)
            {
                NavigationOrientationHelper.UpdateNavigationViewForElement(NavigationOrientationHelper.IsLeftMode(), this);

                Window window = WindowHelper.GetWindowForElement(sender as UIElement);
                window.Title = AppTitleText;
                window.ExtendsContentIntoTitleBar = true;
                window.Activated += Window_Activated;
                window.SetTitleBar(this.AppTitleBar);

                Microsoft.UI.Windowing.AppWindow appWindow = WindowHelper.GetAppWindow(window);
                appWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
                _settings = new UISettings();
                _settings.ColorValuesChanged += _settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event because the triggerTitleBarRepaint workaround no longer works
            };

        }

        //检查当前设备，看是否是windows
        private void SetDeviceFamily()
        {
            var familyName = AnalyticsInfo.VersionInfo.DeviceFamily;

            if (!Enum.TryParse(familyName.Replace("Windows.", string.Empty), out DeviceType parsedDeviceType))
            {
                parsedDeviceType = DeviceType.Other;
            }

            DeviceFamily = parsedDeviceType;
        }

        /// <summary>
        /// 添加菜单选项
        /// </summary>
        private void AddNavigationMenuItems()
        {
            //之前获取到的菜单数据
            IEnumerable<ControlInfoDataGroup> controlInfoDataGroups = 
                ControlInfoDataSource.Instance.Groups.OrderBy(i => i.Title).Where(i => !i.IsSpecialSection);

            foreach (var group in controlInfoDataGroups)
            {

                var itemGroup = new NavigationViewItem()
                {
                    Content = group.Title,
                    Tag = group.UniqueId,
                    DataContext = group,
                    Icon = GetIcon(group.IconGlyph)
                };

                var groupMenuFlyoutItem = new MenuFlyoutItem()
                {
                    Text = $"Copy Link to {group.Title} samples",
                    Icon = new FontIcon() { Glyph = "\uE8C8" },
                    Tag = group
                };

                groupMenuFlyoutItem.Click += this.OnMenuFlyoutItemClick;


                itemGroup.ContextFlyout = new MenuFlyout()
                {
                    Items = { groupMenuFlyoutItem }
                };

                AutomationProperties.SetName(itemGroup, group.Title);

                AutomationProperties.SetAutomationId(itemGroup, group.UniqueId);

                foreach (var item in group.Items)
                {
                    var itemInGroup = new NavigationViewItem()
                    {
                        IsEnabled = item.IncludedInBuild,
                        Content = item.Title,
                        Tag = item.UniqueId,
                        DataContext = item
                    };

                    var itemInGroupMenuFlyoutItem = new MenuFlyoutItem()
                    {
                        Text = $"Copy Link to {item.Title} sample",
                        Icon = new FontIcon() { Glyph = "\uE8C8" },
                        Tag = item
                    };

                    itemInGroupMenuFlyoutItem.Click += this.OnMenuFlyoutItemClick;

                    itemInGroup.ContextFlyout = new MenuFlyout() { Items = { itemInGroupMenuFlyoutItem } };

                    itemGroup.MenuItems.Add(itemInGroup);

                    AutomationProperties.SetName(itemInGroup, item.Title);

                    AutomationProperties.SetAutomationId(itemInGroup, item.UniqueId);
                }

                NavigationViewControl.MenuItems.Add(itemGroup);
            }

            Home.Loaded += OnHomeMenuItemLoaded;
        }


        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                VisualStateManager.GoToState(this, "Deactivated", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Activated", true);
            }
        }

        private void OnHomeMenuItemLoaded(object sender, RoutedEventArgs e)
        {
            if (NavigationViewControl.DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                controlsSearchBox.Focus(FocusState.Keyboard);
            }
        }

        private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            switch ((sender as MenuFlyoutItem).Tag)
            {
                case ControlInfoDataItem item:
                    ProtocolActivationClipboardHelper.Copy(item);
                    return;
                case ControlInfoDataGroup group:
                    ProtocolActivationClipboardHelper.Copy(group);
                    return;
            }
        }

        private void OnNavigationViewControlLoaded(object sender, RoutedEventArgs e)
        {
            // 确保NavigationView视觉状态可以与导航匹配所需的延迟
            Task.Delay(500).ContinueWith(_ => this.NavigationViewLoaded?.Invoke(), TaskScheduler.FromCurrentSynchronizationContext());

            var navigationView = sender as NavigationView;
            navigationView.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, OnIsPaneOpenChanged);
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                if (rootFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    Navigate(typeof(SettingsPage));
                }
            }
            else
            {
                var selectedItem = args.SelectedItemContainer;
                if (selectedItem == AllControlsItem)
                {
                    if (rootFrame.CurrentSourcePageType != typeof(AllControlsPage))
                    {
                        Navigate(typeof(AllControlsPage));
                    }
                }
                else if (selectedItem == Home)
                {
                    if (rootFrame.CurrentSourcePageType != typeof(HomePage))
                    {
                        Navigate(typeof(HomePage));
                    }
                }
                else if (selectedItem == DesignGuidanceItem || selectedItem == AccessibilityItem)
                {
                    //Navigate(typeof(SectionPage), "Design_Guidance");
                }
                else if (selectedItem == ColorItem)
                {
                    Navigate(typeof(ItemPage), "Color");
                }
                else if (selectedItem == GeometryItem)
                {
                    Navigate(typeof(ItemPage), "Geometry");
                }
                else if (selectedItem == IconographyItem)
                {
                    Navigate(typeof(ItemPage), "Iconography");
                }
                else if (selectedItem == SpacingItem)
                {
                    Navigate(typeof(ItemPage), "Spacing");
                }
                else if (selectedItem == TypographyItem)
                {
                    Navigate(typeof(ItemPage), "Typography");
                }
                else if (selectedItem == AccessibilityScreenReaderPage)
                {
                    Navigate(typeof(ItemPage), "AccessibilityScreenReader");
                }
                else if (selectedItem == AccessibilityKeyboardPage)
                {
                    Navigate(typeof(ItemPage), "AccessibilityKeyboard");
                }
                else if (selectedItem == AccessibilityContrastPage)
                {
                    Navigate(typeof(ItemPage), "AccessibilityColorContrast");
                }
                else
                {
                    if (selectedItem.DataContext is ControlInfoDataGroup)
                    {
                        var itemId = ((ControlInfoDataGroup)selectedItem.DataContext).UniqueId;
                        Navigate(typeof(SectionPage), itemId);
                    }
                    else if (selectedItem.DataContext is ControlInfoDataItem)
                    {
                        var item = (ControlInfoDataItem)selectedItem.DataContext;
                        Navigate(typeof(ItemPage), item.UniqueId);
                    }
                }
            }
        }



        private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
        {
            TestContentLoadedCheckBox.IsChecked = true;
        }

        private void OnRootFrameNavigating(object sender, NavigatingCancelEventArgs e)
        {
            TestContentLoadedCheckBox.IsChecked = false;
        }


        //获取菜单前缀图标
        private static IconElement GetIcon(string imagePath)
        {
            return imagePath.ToLowerInvariant().EndsWith(".png") ?
                        (IconElement)new BitmapIcon() { 
                            UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute), 
                            ShowAsMonochrome = false 
                        } :
                        (IconElement)new FontIcon()
                        {
                            Glyph = imagePath
                        };
        }




        public static MainTest GetForElement(object obj)
        {
            UIElement element = (UIElement)obj;
            Window window = WindowHelper.GetWindowForElement(element);
            if (window != null)
            {
                return (MainTest)window.Content;
            }
            return null;
        }

        private void _settings_ColorValuesChanged(UISettings sender, object args)
        {
            // This calls comes off-thread, hence we will need to dispatch it to current app's thread
            dispatcherQueue.TryEnqueue(() =>
            {
                _ = TitleBarHelper.ApplySystemThemeToCaptionButtons(App.StartupWindow);
            });
        }

        public void Navigate( Type pageType, object targetPageArguments = null, NavigationTransitionInfo navigationTransitionInfo = null)
        {
            NavigationRootPageArgs args = new NavigationRootPageArgs();
            args.NavigationRootPage = this;
            args.Parameter = targetPageArguments;
            rootFrame.Navigate(pageType, args, navigationTransitionInfo);
        }

        public Microsoft.UI.Xaml.Controls.NavigationView NavigationView
        {
            get { return NavigationViewControl; }
        }

        private void OnIsPaneOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            var navigationView = sender as NavigationView;
            var announcementText = navigationView.IsPaneOpen ? "Navigation Pane Opened" : "Navigation Pane Closed";

            UIHelper.AnnounceActionForAccessibility(navigationView, announcementText, "NavigationViewPaneIsOpenChangeNotificationId");
        }


        public class NavigationRootPageArgs
        {
            public MainTest NavigationRootPage;
            public object Parameter;
        }


        public enum DeviceType
        {
            Desktop,
            Mobile,
            Other,
            Xbox
        }

        public void EnsureNavigationSelection(string id)
        {
            foreach (object rawGroup in this.NavigationView.MenuItems)
            {
                if (rawGroup is NavigationViewItem group)
                {
                    foreach (object rawItem in group.MenuItems)
                    {
                        if (rawItem is NavigationViewItem item)
                        {
                            if ((string)item.Tag == id)
                            {
                                group.IsExpanded = true;
                                NavigationView.SelectedItem = item;
                                item.IsSelected = true;
                                return;
                            }
                            else if (item.MenuItems.Count > 0)
                            {
                                foreach (var rawInnerItem in item.MenuItems)
                                {
                                    if (rawInnerItem is NavigationViewItem innerItem)
                                    {
                                        if ((string)innerItem.Tag == id)
                                        {
                                            group.IsExpanded = true;
                                            item.IsExpanded = true;
                                            NavigationView.SelectedItem = innerItem;
                                            innerItem.IsSelected = true;
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 程序提示，界面上调用
        /// </summary>
        public string AppTitleText
        {
            get
            {
                return "WinUI 3 Gallery Dev";
            }
        }
    }
}
