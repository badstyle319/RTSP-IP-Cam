namespace RTSP
{
    using System;
    using System.Net;
    using Extensions;

    internal class RTSPInterleavedFrame
    {
        byte magic;
        byte channel;
        UInt16 length;

        public RTSPInterleavedFrame(byte[] bytes)
        {
            magic = bytes[0];
            channel = bytes[1];
            length = (UInt16)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes.SubArray(2, 2), 0));
        }

        public UInt16 Length
        {
            get { return length; }
        }

        public override string ToString()
        {
            string temp = "";
            temp += "Magic:" + magic.ToString("X2") + "\n";
            temp += "Channel:" + channel.ToString("X2") + "\n";
            temp += "Length:" + length.ToString() + "\n";

            return temp;
        }
    }
}
