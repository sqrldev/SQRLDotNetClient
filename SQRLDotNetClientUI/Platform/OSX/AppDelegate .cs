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

        //public NSStatusItem statusBarItem;
        
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
            for (int i = 1; i <= evt.NumberOfItems; i++)
            {
                var innerDesc = evt.DescriptorAtIndex(i);

                //Grab the URL
                if (!string.IsNullOrEmpty(innerDesc.StringValue))
                {
                    //Get a hold of the Main Application View Model
                    var mwvm = (MainWindowViewModel)this.mainWindow.DataContext;

                    //Get a hold of the currently loaded Model (main menu)
                    if (mwvm.Content.GetType() == typeof(MainMenuViewModel))
                    {
                        var mmvm = mwvm.Content as MainMenuViewModel;
                        //If there is a Loaded Identity then Invoke the Authentication Dialog
                        if (mmvm.CurrentIdentity != null)
                        {
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
            
            
            ((NSDistributedNotificationCenter)NSDistributedNotificationCenter.DefaultCenter).AddObserver(new NSString("com.apple.screenIsLocked"), (obj) =>
            {
                Console.WriteLine("Hello");
            });
        }


    }

    public class Observer : NSObject
    {
        // Fields
        private Action<NSObservedChange> cback;
        private NSString key;
        private WeakReference obj;

        // Methods
        public Observer(NSObject obj, NSString key, Action<NSObservedChange> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }
            this.obj = new WeakReference(obj);
            this.key = key;
            this.cback = observer;
            base.IsDirectBinding = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.obj != null)
                {
                    NSObject target = (NSObject)this.obj.Target;
                    if (target != null)
                    {
                        target.RemoveObserver(this, this.key);
                    }
                }
                this.obj = null;
                this.cback = null;
            }
            else
            {
                Console.WriteLine("Warning: observer object was not disposed manually with Dispose()");
            }
            base.Dispose(disposing);
        }

        [Preserve(Conditional = true)]
        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            if ((keyPath == this.key) && (context == base.Handle))
            {
                this.cback(new NSObservedChange(change));
            }
            else
            {
                base.ObserveValue(keyPath, ofObject, change, context);
            }
        }
    }


    public class NSObservedChange
    {
        // Fields
        private NSDictionary dict;

        // Methods
        public NSObservedChange(NSDictionary source)
        {
            this.dict = source;
        }

        // Properties
        public NSKeyValueChange Change
        {
            get
            {
                NSNumber number = (NSNumber)this.dict[NSObject.ChangeKindKey];
                return (NSKeyValueChange)number.Int32Value;
            }
        }

        public NSIndexSet Indexes =>
            ((NSIndexSet)this.dict[NSObject.ChangeIndexesKey]);

        public bool IsPrior
        {
            get
            {
                NSNumber number = this.dict[NSObject.ChangeNotificationIsPriorKey] as NSNumber;
                if (number == null)
                    return false;
                return number.BoolValue;
            }
        }

        public NSObject NewValue =>
            this.dict[NSObject.ChangeNewKey];

        public NSObject OldValue =>
            this.dict[NSObject.ChangeOldKey];
    }



}
