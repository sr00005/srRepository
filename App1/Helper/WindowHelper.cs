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
    //�����࣬����Ӧ�ó����ҵ���������UIElement�Ĵ��ڣ�GetWindowForElement����
    //Ϊ�ˣ����Ǹ������л���ڡ�Ӧ�ó������������WindowHelper.CreateWindow������"new Window"��
    //�������ǲ��ܸ���������صĴ��ڡ�����������ϣ������ƽ̨API��֧����һ���ܡ�
    public class WindowHelper
    {
        static public List<Window> ActiveWindows { get { return _activeWindows; } }

        //������ڼ���
        static private List<Window> _activeWindows = new List<Window>();

        /// <summary>
        /// ����һ�����ڣ�������ھ���Mica���windows11��
        /// </summary>
        /// <returns></returns>
        static public Window CreateWindow()
        {
            Window newWindow = new Window
            {
                //���ô��ڷ��Ϊmiac���
                SystemBackdrop = new MicaBackdrop()
            };
            TrackWindow(newWindow);
            return newWindow;
        }

        static public void TrackWindow(Window window)
        {
            window.Closed += (sender,args) => {
                //���ڱ��رյ�ʱ��ɾ��
                _activeWindows.Remove(window);
            };
            //���ڱ�������ʱ�����_activeWindows
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
