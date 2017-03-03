namespace RTSP_IPCam
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Collections.Generic;
    using MyExtensions;

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

    enum ParsingMode
    {
        HEADER = 0,
        DATA
    }

    enum TransportType
    {
        RTP_AVP_UNICAST = 0,
        RTP_AVP_MULTICAST,
        RTP_AVP_TCP_UNICAST
    }

    class RtspClient : TcpClient, IWorkingThread
    {
        NetworkStream mServerStream = null;
        NSVideo nsvideo;
        int cseq;
        string ipAddress;
        int port;
        string codec;
        string rso;
        string session = null;
        string sprop = null;
        string user = null;
        string password = null;
        string port1, port2;

        TransportType transport_type;
        RtspCommand current_command;
        ParsingMode parsing_mode;
        List<byte> data;

        byte doneCommand;
 
        Thread thread = null;
        ManualResetEvent stopEvent = null;

        //public event DataEventHandler ReceiveData;

        #region constructor and destructor
        public RtspClient(string ipAddress)
            : this(ipAddress,554) { }
        public RtspClient(string ipAddress, int port)
        {
            Init();

            this.ipAddress = ipAddress;
            this.port = port;
            codec = "h264";
            rso = "352x240";
            user = "admin";
            password = "admin";
            transport_type = TransportType.RTP_AVP_UNICAST;

            port1 = "50000";
            port2 = "50001";

            data = new List<byte>();
            nsvideo = new NSVideo();

            Connect(this.ipAddress, this.port);
            mServerStream = GetStream();
        }
        ~RtspClient()
        {
            mServerStream.Close();
            Close();
            data.Clear();
        }
        #endregion

        void Init()
        {
            doneCommand = 0;
            current_command = RtspCommand.NONE;
            parsing_mode = ParsingMode.HEADER;

            session = null;
            sprop = null;
            cseq = 0;
        }
        #region interface method
        public bool Running
        {
            get
            {
                if (thread != null)
                {
                    if (thread.Join(0) == false)
                        return true;

                    Free();
                }
                return false;
            }
        }

        public void Start()
        {
            if (thread == null)
            {
                stopEvent = new ManualResetEvent(false);

                thread = new Thread(new ThreadStart(ReceivingData));
                thread.Name = "ReceivingData";
                thread.Start();
            }
        }

        public void SignalToStop()
        {
            if (thread != null)
            {
                stopEvent.Set();
            }
        }

        public void WaitForStop()
        {
            if (thread != null)
            {
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
        #endregion

        #region gui method
        public void Play()
        {
            if (string.IsNullOrEmpty(sprop))
                SendCommand(RtspCommand.DESCRIBE);
            while (!CommandIsDone(RtspCommand.DESCRIBE))
                Thread.Sleep(100);

            if (string.IsNullOrEmpty(session))
                SendCommand(RtspCommand.SETUP);
            while (!CommandIsDone(RtspCommand.SETUP))
                Thread.Sleep(100);
      
            SendCommand(RtspCommand.PLAY);
        }

        public void Pause()
        {
            parsing_mode = ParsingMode.HEADER;
            SendCommand(RtspCommand.PAUSE);
        }

        public void ShutDown()
        {
            SendCommand(RtspCommand.TEARDOWN);
            Init();
        }
        #endregion

        private void Free()
        {
            thread = null;
            stopEvent.Close();
            stopEvent = null;
        }

        void SendCommand(RtspCommand cmd)
        {
            current_command = cmd;
            string request = PrepareRequest(current_command);
            byte[] requestBytes = Encoding.ASCII.GetBytes(request);
            mServerStream.Write(requestBytes, 0, requestBytes.Length);
            mServerStream.Flush();
        }

        private void SetCommand(RtspCommand cmd)
        {
            doneCommand |= Convert.ToByte(cmd);
        }
        private bool CommandIsDone(RtspCommand cmd)
        {
            if (((int)doneCommand & (int)cmd) > 0)
                return true;
            else
                return false;
        }

        public void ReceivingData()
        {
            while (!stopEvent.WaitOne(0, true))
            {
                if (mServerStream.DataAvailable)
                {
                    byte[] readBuffer = new byte[ReceiveBufferSize];
                    int numberOfBytesRead = 0, pos = 0;

                    if (parsing_mode == ParsingMode.HEADER)
                    {
                        StringBuilder complegeMessage = new StringBuilder();
                        while (mServerStream.DataAvailable)
                        {
                            numberOfBytesRead = mServerStream.Read(readBuffer, 0, readBuffer.Length);
                            complegeMessage.Append(Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));
                        }
                        pos = parseData(complegeMessage.ToString());

                        DebugFunctions.PrintChars(complegeMessage.ToString());

                        if (pos == complegeMessage.Length)
                            SetCommand(current_command);

                        if (current_command == RtspCommand.PLAY)
                            if (CommandIsDone(current_command))
                            {
                                nsvideo.SetDecodeParameter(sprop);
                                nsvideo.Start();
                                parsing_mode = ParsingMode.DATA;
                            }
                    }

                    if (parsing_mode == ParsingMode.DATA)
                    {
                        while (mServerStream.DataAvailable)
                        {
                            numberOfBytesRead = mServerStream.Read(readBuffer, 0, readBuffer.Length);
                            DebugFunctions.PrintChars("receieve "+numberOfBytesRead.ToString()+"\n");
                            /*using (var fileStream = new FileStream("data.bin", FileMode.Append, FileAccess.Write, FileShare.None))
                            {
                                using (var bw = new BinaryWriter(fileStream))
                                {
                                    bw.Write(readBuffer, 0, numberOfBytesRead);
                                }
                            }*/
                            data.AddRange(readBuffer.SubArray(0, numberOfBytesRead));
                            if (data.Count > 4)
                            {
                                RTSPInterleavedFrame iFrame = new RTSPInterleavedFrame(data.GetRange(0, 4).ToArray());

                                if (data.Count >= (iFrame.Length + 4))
                                {
                                    nsvideo.PushMediaPacket(data.GetRange(4, iFrame.Length).ToArray());
                                    data.RemoveRange(0, iFrame.Length + 4);
                                }
                            }
                        }
                    }
                }//end of if(mServerStream.DataAvailable) statement
            }//end of while(!stopEvent.WaitOne(0, true)) statement
        }

        private int parseData(string strData)
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
                    if (current_command == RtspCommand.DESCRIBE)
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
        #region search attribute method
        private string getAttribute(string line, string name, string sub_name = null)
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
        private string getSubAttribute(string line, string name)
        {
            if (!line.StartsWith(name + "=", true, null))
                return null;
            return line.Substring(name.Length + 1, line.Length - name.Length - 1);
        }
        #endregion
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
                request += (port != 554) ?
                    "rtsp://" + ipAddress + ":" + port.ToString() + "/axis-media/media.amp?" :
                    "rtsp://" + ipAddress + "/axis-media/media.amp?";

                if (!string.IsNullOrEmpty(codec))
                {
                    request += "videocodec=" + codec;
                    if (!string.IsNullOrEmpty(rso))
                        request += "&resolution=" + rso;
                }
                
                if (cmd == RtspCommand.DESCRIBE)
                { 
                    
                }
            }

            request += " RTSP/1.0\r\n";

            request += "CSeq: " + cseq.ToString() + "\r\n";
            cseq++;

            //request += "User-Agent: Axis AMC\r\n";

            switch (cmd)
            { 
                case RtspCommand.DESCRIBE:
                    request += "Accept: application/sdp\r\n";
                    break;
                case RtspCommand.SETUP:
                    request += "Transport: RTP/AVP";
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
                    }
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
            if (!string.IsNullOrEmpty(user))
            {
                string encode = user;
                if (!string.IsNullOrEmpty(password))
                    encode += ":" + password;
                request += "Authorization: Basic "
                    + Convert.ToBase64String(Encoding.Default.GetBytes(encode)) + "\r\n";
            }
            return request += "\r\n";
        }
    }
}