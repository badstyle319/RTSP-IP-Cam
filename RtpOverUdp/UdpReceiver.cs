using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Extensions;
using RTP;

using System.IO;

namespace RtpOverUdp
{
    class UdpReceiver : BaseThread
    {
        volatile bool mRun;
        UdpClient mUdpClient = null;
        IPEndPoint mIPEndPoint = null;

        public event DataEventHandler NewData;

        public UdpReceiver(string ip_address, int port)
        {
            mRun = false;
            mUdpClient = new UdpClient(port);
            
            mIPEndPoint = new IPEndPoint(IPAddress.Parse(ip_address), port);
        }

        ~UdpReceiver()
        {
            mUdpClient.Close();
        }

        new public void Start()
        {
            mRun = true;
            base.Start();
        }

        public void Stop()
        {
            StopThread();
        }

        public override void RunThread()
        {
            while (mRun)
            {
                Byte[] receiveBytes = mUdpClient.Receive(ref mIPEndPoint);
                DebugFunctions.PrintChars("receive bytes:" + receiveBytes.Length.ToString() + "\n");
                NewData(this, new DataEventArgs(ref receiveBytes));
                
                using (var fileStream = new FileStream("data.bin", FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    using (var bw = new BinaryWriter(fileStream))
                    {
                        Byte[] len = BitConverter.GetBytes(receiveBytes.Length);
                        bw.Write(len, 0, 2);
                        bw.Write(receiveBytes, 0, receiveBytes.Length);
                    }
                }
                    /*
                    int fragment_type = receiveBytes[12] & 0x1f;
                    int nal_type = receiveBytes[13] & 0x1f;
                    int start_bit = Convert.ToInt32((receiveBytes[13] & 0x80) != 0);
                    int end_bit = Convert.ToInt32((receiveBytes[13] & 0x20) != 0);
                    */
            }
        }

        public override void StopThread()
        {
            mRun = false;
        }
    }
}
