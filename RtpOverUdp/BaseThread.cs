using System;
using System.Threading;

namespace RtpOverUdp
{
    abstract class BaseThread
    {
        Thread _thread;

        public BaseThread() { _thread = new Thread(new ThreadStart(this.RunThread)); }

        // Thread methods / properties
        public void Start()
        {
            if (!IsAlive)
                _thread.Start();
        }
        public void Join() { _thread.Join(); }
        public bool IsAlive { get { return _thread.IsAlive; } }

        // Override in base class
        public abstract void RunThread();
        public abstract void StopThread();
    }
}
