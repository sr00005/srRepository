using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using App1;
using static App1.Win32;

namespace App1.Helper
{
    internal class Win32WindowHelper
    {
        private static WinProc newWndProc = null;
        private static nint oldWndProc = nint.Zero;

        private POINT? minWindowSize = null;
        private POINT? maxWindowSize = null;

        private readonly Window window;

        public Win32WindowHelper(Window window)
        {
            this.window = window;
        }

        public void SetWindowMinMaxSize(POINT? minWindowSize = null, POINT? maxWindowSize = null)
        {
            this.minWindowSize = minWindowSize;
            this.maxWindowSize = maxWindowSize;

            //获取可以操作窗口的句柄
            var hwnd = GetWindowHandleForCurrentWindow(window);
            //委托执行
            newWndProc = new WinProc(WndProc);
            //干扰窗口生成过程，当窗口的大小发生更改时，重新生成窗口排序
            oldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        private static nint GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        private nint WndProc(nint hWnd, WindowMessage Msg, nint wParam, nint lParam)
        {
            //这里是动态修改窗口的大小，当窗口发生发小更改时调用
            switch (Msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    if (minWindowSize != null)
                    {
                        minMaxInfo.ptMinTrackSize.x = (int)(minWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMinTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                    }
                    if (maxWindowSize != null)
                    {
                        minMaxInfo.ptMaxTrackSize.x = (int)(maxWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMaxTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            //继续生成窗口
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd">窗口操作句柄</param>
        /// <param name="nIndex">-4</param>
        /// <param name="newProc">委托函数--Win32WindowHelper.WndProc</param>
        /// <returns></returns>
        private nint SetWindowLongPtr(nint hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (nint.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                //设置窗口的拓展风格
                return new nint(SetWindowLong32(hWnd, nIndex, newProc));
        }

        /// <summary>
        /// 设置窗口最大最小窗口
        /// </summary>
        internal struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
    }
}
