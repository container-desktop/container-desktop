//using Microsoft.Windows.ApplicationModel;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ContainerDesktop
//{
//    static class MyProgram
//    {
//        [global::System.STAThreadAttribute]
//        static void Main(string[] args)
//        {
//            // Take a dependency on Windows App SDK 1.0 Preview 1.
//            // If using version 1.0 Experimental, replace with MddBootstrap.Initialize(0x00010000, "experimental1").
//            // If using version 0.8 Preview, replace with MddBootstrap.Initialize(8, "preview").
//            var ret = MddBootstrap.Initialize(0x00010000, "preview1");

//            global::WinRT.ComWrappersSupport.InitializeComWrappers();
//            global::Microsoft.UI.Xaml.Application.Start((p) => {
//                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
//                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
//                new App();
//            });

//            // Release the DDLM and clean up.
//            MddBootstrap.Shutdown();
//        }
//    }
//}
