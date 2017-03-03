namespace RtpOverUdp
{
    using System;

    public delegate void DataEventHandler(object sender, DataEventArgs e);

    public class DataEventArgs : EventArgs
    {
        byte[] data;

        public DataEventArgs(ref byte[] data)
        {
            this.data = data;
        }
        public byte[] ReceivedData
        {
            get { return this.data; }
        }
    }
}
