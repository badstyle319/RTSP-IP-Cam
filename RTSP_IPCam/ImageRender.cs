namespace RTSP_IPCam
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.IO;

    public class ImageRender : IWorkingThread
    {
        private Graphics graphics = null;

        private Thread thread = null;
        private ManualResetEvent pauseEvent = null;
        private ManualResetEvent stopEvent = null;

        public int bufferTime;
        
        public ImageRender()
        {
            bufferTime = 0;
        }
        ~ImageRender()
        {
            if (graphics != null)
                graphics.Dispose();
        }

        public void AttachWindow(Graphics graphics)
        {
            this.graphics = graphics;
        }

        public bool Running
        {
            get
            {
                if (thread != null)
                {
                    if (thread.Join(0) == false)
                        return true;

                    // the thread is not running, so free resources
                    Free();
                }
                return false;
            }
        }

        public void Start()
        {
            if (thread == null)
            {
                // create events
                stopEvent = new ManualResetEvent(false);
                pauseEvent = new ManualResetEvent(true);

                // create and start new thread
                thread = new Thread(new ThreadStart(WorkerThread));
                thread.Start();
            }
            else
                pauseEvent.Set();
        }

        public void Pause()
        {
            if (thread != null)
                pauseEvent.Reset();
        }

        public void SignalToStop()
        {
            if (thread != null)
                stopEvent.Set();
        }

        public void WaitForStop()
        {
            if (thread != null)
            {
                // wait for thread stop
                thread.Join();

                Free();
            }
        }

        public void Stop()
        {
            if (this.Running)
            {
                SignalToStop();
                WaitForStop();
            }
        }

        private void Free()
        {
            thread = null;
            // release events
            stopEvent.Close();
            stopEvent = null;
            pauseEvent.Close();
            pauseEvent = null;
        }

        public void WorkerThread()
        {
            while (true)
            {
                pauseEvent.WaitOne(Timeout.Infinite);

                if (stopEvent.WaitOne(0))
                    break;
            }
        }
    }
}
