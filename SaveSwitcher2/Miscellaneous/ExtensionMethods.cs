using System;
using System.Windows;
using System.Windows.Threading;

namespace SaveSwitcher2.Miscellaneous
{
    public static class ExtensionMethods
    {
        private static readonly Action EmptyDelegate = delegate { };
        public static void Refresh(UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}
