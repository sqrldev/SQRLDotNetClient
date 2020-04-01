using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.Foundation;
using SQRLDotNetClientUI.Models;
using SQRLDotNetClientUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using Serilog;
using SQRLDotNetClientUI.Views;

namespace SQRLDotNetClientUI.Platform.OSX
{
    /// <summary>
    /// This class is an AppDelegate helper specifically for Mac OSX
    /// Int it's infinite wisdom and unlike Linux and or Windows Mac does not pass in the URL from a sqrl:// invokation 
    /// directly as a startup app paramter, instead it uses a System Event to do this which has to be registered
    /// and listed to.
    /// This requires us to use MonoMac to make it work with .net core
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public bool IsFinishedLaunching = false;
        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOServiceGetMatchingServices(uint masterPort, IntPtr matching, ref int existing);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        static extern IntPtr IOServiceMatching(string s);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOObjectRelease(int o);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOIteratorNext(int o);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IORegistryEntryCreateCFProperties(int entry, out IntPtr eproperties, IntPtr allocator, uint options);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        static extern bool CFNumberGetValue(IntPtr number, int theType, out long value);

        // Instance Window of our App
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

        /// <summary>
        /// Registers an event for handling URL Invokation
        /// </summary>
        private void Init()
        {
            Log.Information("Initializing Mac App Delegate");
            //Register this Apple Delegate globablly with Avalonia for Later Use
            AvaloniaLocator.CurrentMutable.Bind<AppDelegate>().ToConstant(this);
            NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this, new MonoMac.ObjCRuntime.Selector("handleGetURLEvent:withReplyEvent:"), AEEventClass.Internet, AEEventID.GetUrl);
        }
        /// <summary>
        /// This handles the URL invokation System Event when an App is launched  with the
        /// sqrl:// schema from a URL
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="replyEvent"></param>
        [Export("handleGetURLEvent:withReplyEvent:")]
        private void HandleOpenURL(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvent)
        {
            Log.Information("Handling Open URL Event");
            for (int i = 1; i <= evt.NumberOfItems; i++)
            {
                var innerDesc = evt.DescriptorAtIndex(i);

                //Grab the URL
                if (!string.IsNullOrEmpty(innerDesc.StringValue))
                {
                    //Get a hold of the Main Application View Model
                    Log.Information($"Got URL:{innerDesc.StringValue}");
                    this.mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
                    var mwvm = (MainWindowViewModel)this.mainWindow.DataContext;

                    //Get a hold of the currently loaded Model (main menu)
                    if (mwvm.Content.GetType() == typeof(MainMenuViewModel))
                    {
                        var mmvm = mwvm.Content as MainMenuViewModel;
                        //If there is a Loaded Identity then Invoke the Authentication Dialog
                        if (mmvm.CurrentIdentity != null)
                        {
                            Log.Information($"Open URL Data: {innerDesc.StringValue}");
                            mmvm.AuthVM = new AuthenticationViewModel(new Uri(innerDesc.StringValue));
                            mwvm.PriorContent = mwvm.Content;
                            mwvm.Content = mmvm.AuthVM;
                        }

                    }

                }
            }

        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            IsFinishedLaunching = true;
        }


        /// <summary>
        /// Checks the System's Environment Variable HIDIdleTime which is maintained by apple to register last Keyboard or Mouse Input
        /// </summary>
        /// <returns></returns>
        public static TimeSpan CheckIdleTime()
        {
            long idlesecs = 0;
            int iter = 0;
            TimeSpan idleTime = TimeSpan.Zero;
            if (IOServiceGetMatchingServices(0, IOServiceMatching("IOHIDSystem"), ref iter) == 0)
            {
                int entry = IOIteratorNext(iter);
                if (entry != 0)
                {
                    IntPtr dictHandle;
                    if (IORegistryEntryCreateCFProperties(entry, out dictHandle, IntPtr.Zero, 0) == 0)
                    {
                        NSDictionary dict = (NSDictionary)MonoMac.ObjCRuntime.Runtime.GetNSObject(dictHandle);
                        NSObject value;
                        dict.TryGetValue((NSString)"HIDIdleTime", out value);
                        if (value != null)
                        {
                            long nanoseconds = 0;
                            if (CFNumberGetValue(value.Handle, 4 , out nanoseconds))
                            {
                                idlesecs = nanoseconds >> 30; // Shift To Convert from nanoseconds to seconds.
                                idleTime = DateTime.Now - DateTime.Now.AddSeconds(-idlesecs);
                            }
                        }
                    }
                    IOObjectRelease(entry);
                }
                IOObjectRelease(iter);
            }

            return idleTime;
        }
    }

    


}
