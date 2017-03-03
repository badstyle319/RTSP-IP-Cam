namespace RtpOverUdp
{
    using System;
    using System.Text;
    using System.Net.Sockets;
    using Extensions;
    using System.Threading;
    using System.Net;
    using System.IO;
    using RTP;

    enum RtspCommand
    {
        NONE = 0,
        OPTIONS = 1,
        DESCRIBE = 2,
        SETUP = 4,
        PLAY = 8,
        PAUSE = 16,
        TEARDOWN = 32
    }

    class RtpClient : BaseThread
    {
        const string CODEC = "h264";
        const string RESOLUTION = "352x240";
        const string USER = "admin";
        const string PASSWORD = "admin";
        const int PORT_ONE = 50000;
        const int PORT_TWO = 50001;

        TcpClient mTcpClient = null;
        NetworkStream mNetworkStream = null;
        volatile bool mRun;
        int mRequestState;

        string mIPAddress, session, sprop;
        int mPort, mSeq;

        UdpReceiver mUdpReceiver = null;
        NSVideo mNSVideo = null;


        public RtpClient(string ip_address, int port = 554)
        {
            mIPAddress = ip_address;
            mPort = port;

            mTcpClient = new TcpClient();
            mTcpClient.Connect(mIPAddress, mPort);
            mNetworkStream = mTcpClient.GetStream();
            mRun = false;

            mUdpReceiver = new UdpReceiver(ip_address, PORT_ONE);
            mUdpReceiver.NewData += new DataEventHandler(PushData);

            mNSVideo = new NSVideo();
            mNSVideo.SetDecoder("h264");
            File.Delete("data.bin");

            RtspInit();
        }

        void PushData(object sender, DataEventArgs args)
        {
            mNSVideo.PushMediaPacket(args.ReceivedData);
        }

        void RtspInit()
        {
            if (mRequestState != 0)
                mRequestState = 0;
            mSeq = 0;
            session = "";
            sprop = "";
        }

        ~RtpClient()
        {
            StopThread();
            mNetworkStream.Close();
            mTcpClient.Close();
        }

        new public void Start()
        {
            RtspInit();
            mRun = true;
            base.Start();

            SendCommand(RtspCommand.DESCRIBE);
            while (mRequestState != 1)
                Thread.Sleep(100);
            mNSVideo.SetDecodeParameter(sprop);

            SendCommand(RtspCommand.SETUP);
            while (mRequestState != 2)
                Thread.Sleep(100);

            SendCommand(RtspCommand.PLAY);
            while (mRequestState != 3)
                Thread.Sleep(100);

            mNSVideo.Start();
            mUdpReceiver.Start();
        }

        public void Stop()
        {
            if (mRequestState == 3)
            {
                SendCommand(RtspCommand.TEARDOWN);
                while (mRequestState != 4)
                    Thread.Sleep(100);
            }
            
            mUdpReceiver.Stop();
            mNSVideo.Stop();
            StopThread();
        }

        #region implements of BaseThread
        public override void RunThread()
        {
            while (mRun)
            {
                byte[] readBuffer = new byte[mTcpClient.ReceiveBufferSize];
                int numberOfBytesRead, pos;
                StringBuilder complegeMessage = new StringBuilder();

                if (mNetworkStream.DataAvailable)
                {
                    while (mNetworkStream.DataAvailable)
                    {
                        numberOfBytesRead = mNetworkStream.Read(readBuffer, 0, readBuffer.Length);
                        complegeMessage.Append(Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));
                    }

                    pos = parseData(complegeMessage.ToString());
                    /*using (var fileStream = new FileStream("rtsp.bin", FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        
                        using (var bw = new BinaryWriter(fileStream))
                        {
                            bw.Write(complegeMessage.ToString());
                        }
                    }*/
                    //DebugFunctions.PrintChars(complegeMessage.ToString());

                    if (pos == complegeMessage.Length)
                    {
                        switch (mRequestState)
                        {
                            case 0://DESCRIBE ok
                                mRequestState = 1;
                                break;
                            case 1://SETUP ok
                                mRequestState = 2;
                                break;
                            case 2://PLAY ok
                                mRequestState = 3;
                                break;
                            case 3://TEARDOWN ok
                                mRequestState = 4;
                                break;
                        }
                    }
                }
            }
        }
        public override void StopThread()
        {
            mUdpReceiver.Stop();
            mNSVideo.StopThread();
            mRun = false;
        }
        #endregion

        void SendCommand(RtspCommand cmd)
        {
            string request = PrepareRequest(cmd);
            byte[] requestBytes = Encoding.ASCII.GetBytes(request);
            mNetworkStream.Write(requestBytes, 0, requestBytes.Length);
            mNetworkStream.Flush();
        }

        string PrepareRequest(RtspCommand cmd)
        {
            string request = "";

            switch (cmd)
            {
                case RtspCommand.OPTIONS: request = "OPTIONS"; break;
                case RtspCommand.DESCRIBE: request = "DESCRIBE"; break;
                case RtspCommand.SETUP: request = "SETUP"; break;
                case RtspCommand.PLAY: request = "PLAY"; break;
                case RtspCommand.PAUSE: request = "PAUSE"; break;
                case RtspCommand.TEARDOWN: request = "TEARDOWN"; break;
            }
            if (request == "")
                return null;
            else
                request += " ";

            if (cmd == RtspCommand.OPTIONS)
                request += "*";
            else
            {
                request += (mPort != 554) ?
                    "rtsp://" + mIPAddress + ":" + mPort.ToString() + "/axis-media/media.amp?" :
                    "rtsp://" + mIPAddress + "/axis-media/media.amp?";

                if (!string.IsNullOrEmpty(CODEC))
                {
                    request += "videocodec=" + CODEC;
                    if (!string.IsNullOrEmpty(RESOLUTION))
                        request += "&resolution=" + RESOLUTION;
                }

                if (cmd == RtspCommand.DESCRIBE)
                {

                }
            }

            request += " RTSP/1.0\r\n";

            request += "CSeq: " + mSeq.ToString() + "\r\n";
            mSeq++;

            //request += "User-Agent: Axis AMC\r\n";

            switch (cmd)
            {
                case RtspCommand.DESCRIBE:
                    request += "Accept: application/sdp\r\n";
                    break;
                case RtspCommand.SETUP:
                    request += "Transport: RTP/AVP";
                    request += ";unicast;client_port=" + PORT_ONE + "-" + PORT_TWO;
                    /*
                    switch (transport_type)
                    {
                        case TransportType.RTP_AVP_UNICAST:
                            request += ";unicast;client_port=" + port1 + "-" + port2;
                            break;
                        case TransportType.RTP_AVP_MULTICAST:
                            request += ";multicast;client_port=" + port1 + "-" + port2;
                            break;
                        case TransportType.RTP_AVP_TCP_UNICAST:
                            request += "/TCP;unicast";
                            break;
                    }*/
                    request += "\r\n";
                    break;
                case RtspCommand.PLAY:
                case RtspCommand.PAUSE:
                case RtspCommand.OPTIONS:
                case RtspCommand.TEARDOWN:
                    if (!string.IsNullOrEmpty(session))
                        request += "Session: " + session + "\r\n";
                    break;
            }
            if (!string.IsNullOrEmpty(USER))
            {
                string encode = USER;
                if (!string.IsNullOrEmpty(PASSWORD))
                    encode += ":" + PASSWORD;
                request += "Authorization: Basic "
                    + Convert.ToBase64String(Encoding.Default.GetBytes(encode)) + "\r\n";
            }
            return request += "\r\n";
        }

        int parseData(string strData)
        {
            int ofst = 0, end = 0;
            string line;

            end = strData.IndexOf("\r\n");
            if (!strData.StartsWith("RTSP/1.0 200 OK\r\n"))
                return ofst;
            ofst = end + 2;
            while (ofst < strData.Length)
            {
                end = strData.IndexOf("\r\n", ofst);
                if (end < 0)
                    break;
                line = strData.Substring(ofst, end - ofst);
                if (line.Length == 0)
                {
                    ofst = end + 2;
                    if (mRequestState == 0)
                    {
                        while ((end = strData.IndexOf("\r\n", ofst)) > 0)
                        {
                            line = strData.Substring(ofst, end - ofst);
                            if (line.StartsWith("a=fmtp"))
                            {
                                sprop = getAttribute(line, "a=fmtp", "sprop-parameter-sets");
                                //TODO: set sprop
                            }
                            ofst = end + 2;
                        }
                        return ofst;
                    }
                    else
                        break;
                }
                if (line.StartsWith("Session"))
                    session = getAttribute(line, "Session");

                ofst = end + 2;
            }
            return ofst;
        }

        #region search methods
        string getAttribute(string line, string name, string sub_name = null)
        {
            if (!line.StartsWith(name + ":", true, null))
                return null;

            string[] attributes = line.Substring(name.Length + 1, line.Length - name.Length - 1).Trim().Split(';');

            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i] = attributes[i].Trim();
                if (sub_name != null)
                {
                    string sub_attribute = getSubAttribute(attributes[i], sub_name);
                    if (!string.IsNullOrEmpty(sub_attribute))
                        return sub_attribute;
                }
            }
            return attributes[0];
        }
        string getSubAttribute(string line, string name)
        {
            if (!line.StartsWith(name + "=", true, null))
                return null;
            return line.Substring(name.Length + 1, line.Length - name.Length - 1);
        }
        #endregion
    }
}
