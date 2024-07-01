using App1.Helper;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static App1.MainTest;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public string Version
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }

        public string WinAppSdkRuntimeDetails => App.WinAppSdkRuntimeDetails;
        private int lastNavigationSelectionMode = 0;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += OnSettingsPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
        {
            var currentTheme = ThemeHelper.RootTheme;
            switch (currentTheme)
            {
                case ElementTheme.Light:
                    themeMode.SelectedIndex = 0;
                    break;
                case ElementTheme.Dark:
                    themeMode.SelectedIndex = 1;
                    break;
                case ElementTheme.Default:
                    themeMode.SelectedIndex = 2;
                    break;
            }

            MainTest navigationRootPage = MainTest.GetForElement(this);
            if (navigationRootPage != null)
            {
                if (navigationRootPage.NavigationView.PaneDisplayMode == NavigationViewPaneDisplayMode.Auto)
                {
                    navigationLocation.SelectedIndex = 0;
                }
                else
                {
                    navigationLocation.SelectedIndex = 1;
                }
                lastNavigationSelectionMode = navigationLocation.SelectedIndex;
            }

            if (ElementSoundPlayer.State == ElementSoundPlayerState.On)
                soundToggle.IsOn = true;
            if (ElementSoundPlayer.SpatialAudioMode == ElementSpatialAudioMode.On)
                spatialSoundBox.IsOn = true;
#if DEBUG
            ScreenshotCard.Visibility = Visibility.Visible;
#endif
        }

        private void themeMode_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var selectedTheme = ((ComboBoxItem)themeMode.SelectedItem)?.Tag?.ToString();
            var window = WindowHelper.GetWindowForElement(this);
            string color;
            if (selectedTheme != null)
            {
                ThemeHelper.RootTheme = App.GetEnum<ElementTheme>(selectedTheme);
                if (selectedTheme == "Dark")
                {
                    TitleBarHelper.SetCaptionButtonColors(window, Colors.White);
                    color = selectedTheme;
                }
                else if (selectedTheme == "Light")
                {
                    TitleBarHelper.SetCaptionButtonColors(window, Colors.Black);
                    color = selectedTheme;
                }
                else
                {
                    color = TitleBarHelper.ApplySystemThemeToCaptionButtons(window) == Colors.White ? "Dark" : "Light";
                }
                // announce visual change to automation
                UIHelper.AnnounceActionForAccessibility(sender as UIElement, $"Theme changed to {color}",
                                                                                "ThemeChangedNotificationActivityId");
            }
        }

        private void soundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (soundToggle.IsOn == true)
            {
                SpatialAudioCard.IsEnabled = true;
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
            }
            else
            {
                SpatialAudioCard.IsEnabled = false;
                spatialSoundBox.IsOn = false;

                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
                ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.Off;
            }
        }

        private void screenshotModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UIHelper.IsScreenshotMode = screenshotModeToggle.IsOn;
        }


        private void navigationLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Since setting the left mode does not look at the old setting we 
            // need to check if this is an actual update
            if (navigationLocation.SelectedIndex != lastNavigationSelectionMode)
            {
                NavigationOrientationHelper.IsLeftModeForElement(navigationLocation.SelectedIndex == 0, this);
                lastNavigationSelectionMode = navigationLocation.SelectedIndex;
            }
        }

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add(".png"); // meaningless, but you have to have something
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                UIHelper.ScreenshotStorageFolder = folder;
                screenshotFolderLink.Content = UIHelper.ScreenshotStorageFolder.Path;
            }
        }

        private async void screenshotFolderLink_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync(UIHelper.ScreenshotStorageFolder);
        }

        private void spatialSoundBox_Toggled(object sender, RoutedEventArgs e)
        {
            if (soundToggle.IsOn == true)
            {
                ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.Off;
            }
            else
            {
                ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.On;
            }
        }

        private void soundPageHyperlink_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ItemPage), new NavigationRootPageArgs() { Parameter = "Sound", NavigationRootPage = MainTest.GetForElement(this) });
        }

        private async void bugRequestCard_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/microsoft/WinUI-Gallery/issues/new/choose"));

        }
    }
}
