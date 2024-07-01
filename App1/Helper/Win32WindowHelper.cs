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

            //��ȡ���Բ������ڵľ��
            var hwnd = GetWindowHandleForCurrentWindow(window);
            //ί��ִ��
            newWndProc = new WinProc(WndProc);
            //���Ŵ������ɹ��̣������ڵĴ�С��������ʱ���������ɴ�������
            oldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        private static nint GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        private nint WndProc(nint hWnd, WindowMessage Msg, nint wParam, nint lParam)
        {
            //�����Ƕ�̬�޸Ĵ��ڵĴ�С�������ڷ�����С����ʱ����
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
            //�������ɴ���
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd">���ڲ������</param>
        /// <param name="nIndex">-4</param>
        /// <param name="newProc">ί�к���--Win32WindowHelper.WndProc</param>
        /// <returns></returns>
        private nint SetWindowLongPtr(nint hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (nint.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                //���ô��ڵ���չ���
                return new nint(SetWindowLong32(hWnd, nIndex, newProc));
        }

        /// <summary>
        /// ���ô��������С����
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
