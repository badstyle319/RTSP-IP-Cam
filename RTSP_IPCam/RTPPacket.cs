namespace RTSP_IPCam
{
    using System;
    using System.Net;
    using MyExtensions;

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

    internal class RTPHeader
    {
        string V, P, X, CC, M, PT;
        UInt16 SN;
        UInt32 T, SSRC;

        public RTPHeader(byte[] bytes)
        {
            string vpxcc = bytes[0].ToBits();
            this.V = vpxcc.Substring(0, 2);
            this.P = vpxcc[2].ToString();
            this.X = vpxcc[3].ToString();
            this.CC = vpxcc.Substring(4, 4);

            string mpt = bytes[1].ToBits();
            this.M = mpt[0].ToString();
            this.PT = mpt.Substring(1, 7);

            this.SN = (UInt16)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes.SubArray(2, 2), 0));
            this.T = (UInt32)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes.SubArray(4, 4), 0));
            this.SSRC = (UInt32)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes.SubArray(8, 4), 0));
        }

        public UInt16 SeqNO
        {
            get { return SN; }
        }

        public UInt32 Timestamp
        {
            get { return T; }
        }

        public override string ToString()
        {
            string temp = "";
            temp += "V:" + this.V + "\n";
            temp += "P:" + this.P + "\n";
            temp += "X:" + this.X + "\n";
            temp += "CC:" + this.CC + "\n";
            temp += "M:" + this.M + "\n";
            temp += "PT:" + Convert.ToUInt16(this.PT, 2) + "\n";
            temp += "Sequence Number:" + this.SN + "\n";
            temp += "Timestamp:" + this.T + "\n";
            temp += "SSRC:" + this.SSRC + "\n";
            return temp;
        }
    }

    internal class RTPPayload
    {
        byte[] data;
        public RTPPayload(byte[] bytes)
        {
            data = new byte[bytes.Length];
            Array.Copy(bytes, 0, data, 0, bytes.Length);
        }
        public byte[] Data
        {
            get { return data; }
        }
        public override string ToString()
        {
            string temp = "";
            for (int i = 0; i < 10; i++)
                temp += data[i].ToString("X2") + " ";
            temp += "\n";
            return temp;
        }
    }

    class RTPPacket
    {
        double tmark;
        byte[] data;

        public RTPPacket() { }
        ~RTPPacket() { }

        public double TimeMark
        {
            get { return tmark; }
            set { this.tmark = value; }
        }
        public byte[] Data
        {
            get { return data; }
            set 
            { 
                data = new byte[value.Length];
                Buffer.BlockCopy(value, 0, data, 0, value.Length);
            }
        }
        public override string ToString()
        {
            string temp = "";
            temp += "TimeMark:" + tmark.ToString();
            temp += "\nData:";
            for (int i = 0; i < 5; i++)
                temp += data[i].ToString("X2") + " ";
            temp += "\n";
            return temp;
        }
    }
}
