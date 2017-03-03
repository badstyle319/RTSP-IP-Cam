namespace RTSP_IPCam
{
    interface IWorkingThread
    {
        bool Running { get; }

        void Start();

        void SignalToStop();

        void WaitForStop();

        void Stop();
    }
}
