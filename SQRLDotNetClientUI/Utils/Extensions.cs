using MessageBox.Avalonia.BaseWindows;
using MessageBox.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SQRLDotNetClientUI.Utils
{
    public static class Extensions
    {
        public static void SetMessageStartupLocation(this MessageBox.Avalonia.BaseWindows.MsBoxStandardWindow stdMessage, Avalonia.Controls.WindowStartupLocation startupLoc)
        {
            var window = (MessageBox.Avalonia.Views.MsBoxStandardWindow)stdMessage.GetType().GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stdMessage);
            window.WindowStartupLocation = startupLoc;
        }

        public static void SetMaxWidth(this MessageBox.Avalonia.BaseWindows.MsBoxStandardWindow stdMessage, int maxWidth)
        {
            var window = (MessageBox.Avalonia.Views.MsBoxStandardWindow)stdMessage.GetType().GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stdMessage);
            window.MaxWidth = maxWidth;
        }
    }
}
