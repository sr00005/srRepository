//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT.Interop;


namespace App1.Helper
{
    //辅助类，允许应用程序找到包含任意UIElement的窗口（GetWindowForElement）。
    //为此，我们跟踪所有活动窗口。应用程序代码必须调用WindowHelper.CreateWindow而不是"new Window"，
    //这样我们才能跟踪所有相关的窗口。将来，我们希望能在平台API中支持这一功能。
    public class WindowHelper
    {
        static public List<Window> ActiveWindows { get { return _activeWindows; } }

        //存货窗口集合
        static private List<Window> _activeWindows = new List<Window>();

        /// <summary>
        /// 创建一个窗口，这个窗口具有Mica风格（windows11）
        /// </summary>
        /// <returns></returns>
        static public Window CreateWindow()
        {
            Window newWindow = new Window
            {
                //设置窗口风格为miac风格
                SystemBackdrop = new MicaBackdrop()
            };
            TrackWindow(newWindow);
            return newWindow;
        }

        static public void TrackWindow(Window window)
        {
            window.Closed += (sender,args) => {
                //窗口被关闭的时候删除
                _activeWindows.Remove(window);
            };
            //窗口被创建的时候加入_activeWindows
            _activeWindows.Add(window);
        }

        static public AppWindow GetAppWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        static public Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null;
        }
        // get dpi for an element
        static public double GetRasterizationScaleForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return element.XamlRoot.RasterizationScale;
                    }
                }
            }
            return 0.0;
        }



        static public StorageFolder GetAppLocalFolder()
        {
            StorageFolder localFolder;
            if (!NativeHelper.IsAppPackaged)
            {
                localFolder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory)).Result;
            }
            else
            {
                localFolder = ApplicationData.Current.LocalFolder;
            }
            return localFolder;
        }
    }
}
