using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Core;
using Microsoft.UI.Input;
using App1.Common;


namespace App1.Helper
{
    ///<summary>
    /// NavigationHelper ��������ҳ��֮����е�����������
    /// ���˶�ջ���� SuspensionManager ������һ���Դ������
    /// ��ҳ��䵼��ʱ���������ڹ����״̬����
    /// </summary>
    ///<example>
    /// Ҫʹ�� NavigationHelper���밴����������������������ߴӻ���ҳ�������ҳ��ģ�壨�ǿհ�ҳ����ʼ��
    /// 1) ��ĳ���ط����� NavigationHelper ��ʵ����������ҳ��Ĺ��캯���У���Ϊ LoadState �� SaveState �¼�ע��ص���
    ///
    [Windows.Foundation.Metadata.WebHostHidden]
    public class NavigationHelper : DependencyObject
    {
        private Page Page { get; set; }
        private Frame Frame { get { return this.Page.Frame; } }

        ///<summary>
        /// ��ʼ��һ���µ� <see cref="NavigationHelper"/> ���ʵ����
        /// </summary>
        ///<param name="page">һ��ָ��ǰҳ������ã����ڵ�����������������п�ܲ�����</param>
        public NavigationHelper(Page page)
        {
            this.Page = page;
        }

        #region Process lifetime management

        private string _pageKey;

        /// <summary>
        /// ������¼���ʹ���ڵ��������д��ݵ��������ҳ�棬�Լ��� SaveState �¼�������򱣴���κ�״̬��
        /// </summary>
        public event LoadStateEventHandler LoadState;
        /// <summary>
        /// Handle this event to save state that can be used by
        /// the LoadState event handler. Save the state in case
        /// the application is suspended or the page is discarded
        /// from the navigation cache.
        /// </summary>
        public event SaveStateEventHandler SaveState;

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// This method calls <see cref="LoadState"/>, where all page specific
        /// navigation and process lifetime management logic should be placed.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.</param>
        public void OnNavigatedTo(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Clear existing state for forward navigation when adding a new page to the
                // navigation stack
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Pass the navigation parameter to the new page
                this.LoadState?.Invoke(this, new LoadStateEventArgs(e.Parameter, null));
            }
            else
            {
                // Pass the navigation parameter and preserved page state to the page, using
                // the same strategy for loading suspended state and recreating pages discarded
                // from cache
                this.LoadState?.Invoke(this, new LoadStateEventArgs(e.Parameter, (Dictionary<string, object>)frameState[this._pageKey]));
            }
        }

        /// <summary>
        /// Invoked when this page will no longer be displayed in a Frame.
        /// This method calls <see cref="SaveState"/>, where all page specific
        /// navigation and process lifetime management logic should be placed.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.</param>
        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<string, object>();
            this.SaveState?.Invoke(this, new SaveStateEventArgs(pageState));
            frameState[_pageKey] = pageState;
        }

        #endregion
    }

    /// <summary>
    /// RootFrameNavigationHelper registers for standard mouse and keyboard
    /// shortcuts used to go back and forward. There should be only one
    /// RootFrameNavigationHelper per view, and it should be associated with the
    /// root frame.
    /// </summary>
    /// <example>
    /// To make use of RootFrameNavigationHelper, create an instance of the
    /// RootNavigationHelper such as in the constructor of your root page.
    /// <code>
    ///     public MyRootPage()
    ///     {
    ///         this.InitializeComponent();
    ///         this.rootNavigationHelper = new RootNavigationHelper(MyFrame);
    ///     }
    /// </code>
    /// </example>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class RootFrameNavigationHelper
    {
        private Frame Frame { get; set; }
        private NavigationView CurrentNavView { get; set; }

#nullable enable
        private static RootFrameNavigationHelper? instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootNavigationHelper"/> class.
        /// </summary>
        /// <param name="rootFrame">A reference to the top-level frame.
        /// This reference allows for frame manipulation and to register navigation handlers.</param>
        public RootFrameNavigationHelper(Frame rootFrame, NavigationView currentNavView)
        {
            if (instance != null)
            {
                return;
            }

            this.Frame = rootFrame;

            //���ؼ���ɵ���ʱ��Ӽ�¼��ǰ��Ϣ���ں��˰�ť
            this.Frame.Navigated += (s, e) =>
            {
                // ÿ����������ʱ�����¡����ˡ���ť��
                UpdateBackButton();
            };

            this.CurrentNavView = currentNavView;
            //���˰�ť����
            CurrentNavView.BackRequested += NavView_BackRequested;

            //�˵���ť����
            CurrentNavView.PointerPressed += CurrentNavView_PointerPressed;

            instance = this;
        }

        /// <summary>
        /// Invoked on every keystroke, including system keys such as Alt key combinations.
        /// Used to detect keyboard navigation between pages even when the page itself
        /// doesn't have focus.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        public static void RaiseKeyPressed(uint keyCode)
        {
            if (instance == null) return;

            // Only investigate further when Left, Right, or the dedicated
            // Previous or Next keys are pressed.
            if (keyCode == (int)VirtualKey.Left ||
                keyCode == (int)VirtualKey.Right ||
                keyCode == 166 ||
                keyCode == 167 ||
                keyCode == (int)VirtualKey.Back)
            {
                var downState = CoreVirtualKeyStates.Down;
                // VirtualKeys 'Menu' key is also the 'Alt' key on the keyboard.
                bool isMenuKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & downState) == downState;
                bool isControlKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & downState) == downState;
                bool isShiftKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & downState) == downState;
                bool isWindowsKeyPressed = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) & downState) == downState;
                bool isModifierKeyPressed = !isMenuKeyPressed && !isControlKeyPressed && !isShiftKeyPressed;
                bool isOnlyAltPressed = isMenuKeyPressed && !isControlKeyPressed && !isShiftKeyPressed;

                if (((int)keyCode == 166 && isModifierKeyPressed) ||
                    (keyCode == (int)VirtualKey.Left && isOnlyAltPressed) ||
                    (keyCode == (int)VirtualKey.Back && isWindowsKeyPressed))
                {
                    // When the previous key or Alt+Left are pressed navigate back.
                    instance.TryGoBack();
                }
                else if (((int)keyCode == 167 && isModifierKeyPressed) ||
                    (keyCode == (int)VirtualKey.Right && isOnlyAltPressed))
                {
                    // When the next key or Alt+Right are pressed navigate forward.
                    instance.TryGoForward();
                }
            }
        }

        private void CurrentNavView_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(CurrentNavView).Properties;

            // ����������Ҽ����м�����ϼ�
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed)
                return;

            // ������º��˻�ǰ���������������߶�����������Ӧ�ص�����
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                e.Handled = true;
                if (backPressed) TryGoBack();
                if (forwardPressed) TryGoForward();
            }
        }

        //���˰���
        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            bool navigated = false;
            // ������������ǵ��ӵģ��򲻷��ء�
            if (this.CurrentNavView.IsPaneOpen && (this.CurrentNavView.DisplayMode == NavigationViewDisplayMode.Compact || this.CurrentNavView.DisplayMode == NavigationViewDisplayMode.Minimal))
            {
                return navigated;
            }

            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                navigated = true;
            }

            return navigated;
        }

        private bool TryGoForward()
        {
            bool navigated = false;
            if (this.Frame.CanGoForward)
            {
                this.Frame.GoForward();
                navigated = true;
            }
            return navigated;
        }

        private void UpdateBackButton()
        {
            //�����Ƿ���Է���
            this.CurrentNavView.IsBackEnabled = this.Frame.CanGoBack ? true : false;
        }
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="NavigationHelper.LoadState"/>event
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);
    /// <summary>
    /// Represents the method that will handle the <see cref="NavigationHelper.SaveState"/>event
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);

    /// <summary>
    /// Class used to hold the event data required when a page attempts to load state.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <summary>
        /// The parameter value passed to <see cref="Frame.Navigate(Type, object)"/>
        /// when this page was initially requested.
        /// </summary>
        public object NavigationParameter { get; private set; }
        /// <summary>
        /// A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.
        /// </summary>
        public Dictionary<string, object> PageState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadStateEventArgs"/> class.
        /// </summary>
        /// <param name="navigationParameter">
        /// The parameter value passed to <see cref="Frame.Navigate(Type, object)"/>
        /// when this page was initially requested.
        /// </param>
        /// <param name="pageState">
        /// A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.
        /// </param>
        public LoadStateEventArgs(object navigationParameter, Dictionary<string, object> pageState)
            : base()
        {
            this.NavigationParameter = navigationParameter;
            this.PageState = pageState;
        }
    }
    /// <summary>
    /// Class used to hold the event data required when a page attempts to save state.
    /// </summary>
    public class SaveStateEventArgs : EventArgs
    {
        /// <summary>
        /// An empty dictionary to be populated with serializable state.
        /// </summary>
        public Dictionary<string, object> PageState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveStateEventArgs"/> class.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        public SaveStateEventArgs(Dictionary<string, object> pageState)
            : base()
        {
            this.PageState = pageState;
        }
    }
}
