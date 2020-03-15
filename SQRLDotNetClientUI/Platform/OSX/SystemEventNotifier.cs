using MonoMac.Foundation;
using Serilog;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Foundation;
using AppKit;
namespace SQRLDotNetClientUI.Platform.OSX
{
    public class SystemEventNotifier : ISystemEventNotifier
    {

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOServiceGetMatchingServices(uint masterPort, IntPtr matching, ref int existing);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
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

        public event EventHandler<SystemEventArgs> Idle;
        public event EventHandler<SystemEventArgs> Screensaver;
        public event EventHandler<SystemEventArgs> Standby;
        public event EventHandler<SystemEventArgs> SessionLogoff;
        public event EventHandler<SystemEventArgs> SessionLock;
        public event EventHandler<SystemEventArgs> ShutdownOrRestart;

        public SystemEventNotifier()
        {
            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screenIsLocked"), (obj) =>
            {
                Log.Information("Detected Screen Saver");
                Screensaver?.Invoke(this, new SystemEventArgs("Screensaver"));
            });

            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screensaver.didstart"), (obj) =>
            {
                Log.Information("Detected session lock");
                Screensaver?.Invoke(this, new SystemEventArgs("Session Lock"));
                
            });
        }
    }
}
