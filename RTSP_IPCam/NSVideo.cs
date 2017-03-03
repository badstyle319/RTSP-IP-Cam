namespace RTSP_IPCam
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Collections;
    using MyExtensions;
    using System.Text;
    using System.Runtime.InteropServices;
    using DCDWrapper;

    class NSVideo : IWorkingThread
    {
        public static Queue m_pQueue = null;

        uint m_pDecoder;
        ImageRender m_pRender = null;

        byte[] m_pxBuf, m_pszParm;
        int m_szData, m_nszParm;
        uint t_stmp, clock_ms;
        int counter;
        Thread thread = null;
        ManualResetEvent stopEvent = null;

        public NSVideo()
        {
            t_stmp = 0;
            clock_ms = 90;
            m_pxBuf = null;
            m_szData = m_nszParm = 0;
            m_pQueue = new Queue();
            m_pDecoder = MyDecoder.DCD_Create("h264");
            //m_pRender = new ImageRender();
            counter = 0;
        }
        ~NSVideo()
        {
            Free();
        }
        void Free()
        {
            m_pQueue.Clear();   
            Stop();
        }
        /*public bool SetDecoder()
        {
            if (m_pDecoder == null)
                m_pDecoder = new AVDecoder();
            int clock = 90000;
            clock_ms = (uint)clock / 1000;
            if (clock_ms == 0) clock_ms = 90;
            return true;
        }*/

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
        unsafe public void Start()
        {
            if (thread == null)
            {
                if (m_pDecoder == 0) return;
                if (m_pxBuf == null) m_pxBuf = new byte[1024 * 1000];
                m_szData = 0;
                //TODO: start render
                //if (m_pRender != null) m_pRender.Start();
                fixed (Byte* ptr = m_pszParm)
                    MyDecoder.DCD_Open(m_pDecoder, m_nszParm, ptr);

                stopEvent = new ManualResetEvent(false);
                thread = new Thread(new ThreadStart(DecodeData));
                thread.Name = "DecodeData";
                thread.Start();
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
        public void SignalToStop()
        {
            if (thread != null)
                stopEvent.Set();
        }
        public void Stop()
        {
            if (this.Running)
            {
                SignalToStop();
                WaitForStop();
                if (m_pDecoder == 0) return;
                MyDecoder.DCD_Close(m_pDecoder);
                if (m_pRender != null) m_pRender.Stop();
            }
        }
        #endregion

        void DecodeData()
        {
            RTPPacket e;
            while (!stopEvent.WaitOne(0, true))
            {
                if (m_pQueue.Count > 0)
                {
                    lock (this)
                    {
                        e = (RTPPacket)m_pQueue.Dequeue();
                    }
                    double dt = e.TimeMark - Convert.ToDouble(DateTime.Now.Ticks);
                    if (dt < 10)
                    {
                        po_Draw(e.Data);
                        counter++;
                    }
                    e = null;
                }
            }
        }

        unsafe void po_Draw(byte[] pData)
        {
            bool done = false;

            if (pData[0] == 0)
            {
                fixed (Byte* ptr = pData)
                    done = MyDecoder.DCD_Decode(m_pDecoder, ptr, pData.Length);
            }
            else
            {
                byte cc = pData[0];
                byte type = (byte)(cc & 0x1f);
                bool doit = false;
                int size ;
                switch (type)
                {
                    case 0:
                        size = pData.Length - 1;
                        if ((cc & 0x40) != 0) m_szData = 0;
                        Buffer.BlockCopy(pData, 1, m_pxBuf, m_szData, size);
                        m_szData += size;
                        if ((cc & 0x80) != 0) doit = true;
                        break;
                    case 28:
                        size = pData.Length - 2;
                        if ((pData[1] & 0x80) > 0)
                        {
                            m_pxBuf[0] = 0;
                            m_pxBuf[1] = 0;
                            m_pxBuf[2] = 1;
                            m_pxBuf[3] = (byte)((pData[0] & 0xe0) | (pData[1] & 0x1f));
                            m_szData = 4;
                        }
                        if ((pData[1] & 0x40) > 0) doit = true;
                        Buffer.BlockCopy(pData, 2, m_pxBuf, m_szData, size);
                        m_szData += size;
                        break;
                    case 1:
                    case 5:
                        size = pData.Length;
                        m_pxBuf[0] = 0;
                        m_pxBuf[1] = 0;
                        m_pxBuf[2] = 1;
                        m_szData = 3;
                        Buffer.BlockCopy(pData, 0, m_pxBuf, m_szData, size);
                        m_szData += size;
                        doit = true;
                        break;
                    default:
                        break;
                }
                if (doit)
                {
                    fixed (Byte* ptr = m_pxBuf)
                        done = MyDecoder.DCD_Decode(m_pDecoder, ptr, m_szData);
                    m_szData = 0;
                }
            }
            if (done)
            {
                //TODO: show decoded image
                int sz, w, h;
                Byte* data = MyDecoder.DCD_GetPicture(m_pDecoder, out w, out h);
                sz = MyDecoder.DCD_GetPictureSize(m_pDecoder);
                Byte[] temp = new Byte[100];
                Byte* t = data;
                for (int i = 0; i < 100; i++)
                {
                    temp[i] = *t;
                    t++;
                }

            }
        }

        /*void SaveFrame(FFmpeg.AVPicture pFrame, int width, int height)
        {
            StreamWriter sw = new StreamWriter("123.bmp");
            sw.Write("P6\n"+width.ToString()+" "+height.ToString()+"\n255\n");
            
            
            // Write pixel data
            for (int y = 0; y < height; y++)
                sw.Write(pFrame.data[0] + y * pFrame.linesize[0]);

            // Close file
            sw.Close();
        }*/

        public void SetDecodeParameter(string sprop)
        {
            string[] parms = sprop.Split(new char[] { ',', ' ' });
            byte[] startCode = new byte[3] { 0, 0, 1 };
            byte[] sps = Convert.FromBase64String(parms[0]);
            byte[] pps = Convert.FromBase64String(parms[1]);
            m_nszParm = startCode.Length * 2 + sps.Length + pps.Length;
            m_pszParm = new byte[m_nszParm];
            Buffer.BlockCopy(startCode, 0, m_pszParm, 0, startCode.Length);
            Buffer.BlockCopy(sps, 0, m_pszParm, startCode.Length, sps.Length);
            Buffer.BlockCopy(startCode, 0, m_pszParm, startCode.Length + sps.Length, startCode.Length);
            Buffer.BlockCopy(pps, 0, m_pszParm, startCode.Length * 2 + sps.Length, pps.Length);
        }

        public void PushMediaPacket(byte[] bytes)
        {
            if (bytes.Length < 12) return;
            RTPHeader header = new RTPHeader(bytes.SubArray(0, 12));
            if (t_stmp == 0) t_stmp = header.Timestamp;
            uint dt = (header.Timestamp - t_stmp) / clock_ms;
            if (dt < 0) return;
            RTPPacket packet = new RTPPacket();
            packet.TimeMark = Convert.ToDouble(DateTime.Now.Ticks) + dt;
            packet.Data = bytes.SubArray(12, bytes.Length - 12);
            lock (this)
            {
                m_pQueue.Enqueue(packet);
            }
        }
    }
}
