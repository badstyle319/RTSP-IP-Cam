namespace RTSP_IPCam
{
    using System;

    public delegate void PacketEventHandler(object sender, PacketEventArgs e);

    public class PacketEventArgs : EventArgs
    {
        public PacketEventArgs()
        {
            
        }
    }
}
