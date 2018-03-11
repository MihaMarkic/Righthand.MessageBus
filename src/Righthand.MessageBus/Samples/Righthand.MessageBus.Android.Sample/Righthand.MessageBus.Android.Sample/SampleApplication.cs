using Android.App;
using Android.Runtime;
using System;

namespace Righthand.MessageBus.Android.Sample
{
    [Application]
    public class SampleApplication : Application
    {
        public SampleApplication()
        {
        }

        protected SampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
        /// <summary>
        /// Makes sure dispatcher gets disposed.
        /// </summary>
        public override void OnTerminate()
        {
            base.OnTerminate();
            Globals.Dispatcher.Dispose();
        }
    }
}