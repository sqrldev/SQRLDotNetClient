using Avalonia.Controls;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using MonoMac.AppKit;
using MonoMac.Foundation;
using SQRLDotNetClientUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.Utils
{
    [Register("AppDelegate")]
    class AppDelegate : NSApplicationDelegate
    {
        //TODO: technically, using an AppDelegate here is wrong. 
        //i should change it over at some point.
        //however, we might want to have a dedicated delegate for extended OS X functionalities.
        Window mainWindow = null;
        public AppDelegate(Window mainWindow)
        {
            this.mainWindow = mainWindow;
            Init();
        }
        public AppDelegate()
        {
            Init();
        }
        private void Init()
        {
            NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this, new MonoMac.ObjCRuntime.Selector("handleGetURLEvent:withReplyEvent:"), AEEventClass.Internet, AEEventID.GetUrl);
        }
        [Export("handleGetURLEvent:withReplyEvent:")]
        private void HandleOpenURL(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvent)
        {
            for (int i = 1; i <= evt.NumberOfItems; i++)
            {
                var innerDesc = evt.DescriptorAtIndex(i);

                if (!string.IsNullOrEmpty(innerDesc.StringValue))
                {
                    var mwvm = (MainWindowViewModel)this.mainWindow.DataContext;
                    if(mwvm.Content.GetType()==typeof(MainMenuViewModel))
                    {
                        var mmvm = mwvm.Content as MainMenuViewModel;
                        if(mmvm.CurrentIdentity !=null)
                        {
                            mmvm.AuthVM = new AuthenticationViewModel(mmvm.sqrlInstance, mmvm.CurrentIdentity, new Uri(innerDesc.StringValue));
                            mwvm.PriorContent = mwvm.Content;
                            mwvm.Content = mmvm.AuthVM;
                        }

                    }
                  
                }
            }

        }
    }
}
