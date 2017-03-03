using System;
using System.Collections.Generic;
using RTP;
using DCDWrapper;
using Extensions;

namespace RtpOverUdp
{
    internal class RTPQueue:Queue<RTPPacket>
    {
        new public void Enqueue(RTPPacket packet)
        {
            lock (this)
            {
                base.Enqueue(packet);
            }
        }

        new public RTPPacket Dequeue()
        {
            lock (this)
            {
                RTPPacket packet = base.Dequeue();
                return packet;
            }
        }
    }

    class NSVideo : BaseThread
    {
        uint t_stmp, clock_ms;
        uint m_pDecoder;
        //TODO:drawer

        byte[] m_pxBuf, m_pszParm;
        int m_szData, m_nszParm;

        volatile bool mRun;
        int counter = 0;

        Queue<RTPPacket> mQueue = null;

        public NSVideo()
        {
            m_pDecoder = 0;
            t_stmp = 0;
            clock_ms = 0;

            mRun = false;

            mQueue = new Queue<RTPPacket>();
        }
        ~NSVideo()
        {
            if (m_pDecoder != 0)
                MyDecoder.DCD_Close(m_pDecoder);

            mQueue.Clear();
        }

        public override void RunThread()
        {
            while (mRun)
            {
                if (mQueue.Count > 0)
                {
                    RTPPacket packet = mQueue.Dequeue();
                    po_Draw(packet.Data);
                }
            }
        }

        public override void StopThread()
        {
            mRun = false;
            mQueue.Clear();
        }

        public void AttachWindow(System.Drawing.Graphics g)
        { 
        
        }

        new unsafe public bool Start()
        {
            if (m_pDecoder == 0) return false;
            m_pxBuf = new byte[1024 * 64];

            m_szData = 0;

            if (m_nszParm == 0)
                return false;
            fixed (byte* pszParm = m_pszParm)
            {
                bool temp =MyDecoder.DCD_Open(m_pDecoder, m_nszParm, pszParm);
            }
            mRun = true;
            base.Start();

            return true;
        }

        public void Stop()
        {
            StopThread();

            if (m_pDecoder == 0) return;
            MyDecoder.DCD_Close(m_pDecoder);
            //TODO:stop drawer
        }

        public bool SetDecoder(string name)
        {
            if (m_pDecoder != 0)
            {
                MyDecoder.DCD_Close(m_pDecoder);
            }
            m_pDecoder = MyDecoder.DCD_Create(name);

            int clock = 90000;
            clock_ms = (uint)clock / 1000;
            if (clock_ms == 0) clock_ms = 90;
            return true;
        }

        public void SetDecodeParameter(string sprop)
        {
            string[] parms = sprop.Split(new char[] { ',', ' ' });
            byte[] startCode = new byte[3] { 0, 0, 1 };
            byte[] sps = Convert.FromBase64String(parms[0]);
            byte[] pps = Convert.FromBase64String(parms[1]);

            byte[] sps2 = ArrayExtensions.Combine(startCode, sps);
            byte[] pps2 = ArrayExtensions.Combine(startCode, pps);
            m_nszParm = sps2.Length + pps2.Length;
            m_pszParm = ArrayExtensions.Combine(sps2, pps2);
        }

        public void PushMediaPacket(byte[] pData)
        {
            mQueue.Enqueue(new RTPPacket(pData));
        }

        unsafe void po_Draw(byte[] data)
        {
            bool done = false;

            int ppt = data[1] & 0x7f;

            if (data[0] == 0)
            {
                fixed (byte* pData = data)
                {
                    done = MyDecoder.DCD_Decode(m_pDecoder, pData, data.Length);
                }
            }
            else
            {
                int cc = data[0];
                int type = cc & 0x1f;
                bool doit = false;
                int size;
                fixed (byte* pb = m_pxBuf)
                {
                    switch (type)
                    {
                        case 0:
                            size = data.Length - 1;
                            if ((cc & 0x40) != 0) m_szData = 0;
                            break;
                        case 28:
                            size = data.Length - 2;
                            if ((data[1] & 0x80) != 0)
                            {
                                pb[0] = 0;
                                pb[1] = 0;
                                pb[2] = 1;
                                pb[3] = (byte)((data[0] & 0xe0) | (data[1] & 0x1f));

                                m_szData = 4;
                            }
                            if ((data[1] & 0x40) != 0) doit = true;

                            Buffer.BlockCopy(data, 2, m_pxBuf, m_szData, size);
                            m_szData += size;
                            break;
                        case 1:
                        case 5:
                            size = data.Length;
                            pb[0] = 0;
                            pb[1] = 0;
                            pb[2] = 1;
                            m_szData = 3;
                            Buffer.BlockCopy(data, 0, m_pxBuf, m_szData, size);
                            m_szData += size;
                            doit = true;
                            break;
                        default:
                            break;
                    }
                    if (doit)
                    {
                        done = MyDecoder.DCD_Decode(m_pDecoder, pb, m_szData);
                        m_szData = 0;
                    }
                }
            }
            if (done)
            {
                int sz, w, h;
                byte* pData = MyDecoder.DCD_GetPicture(m_pDecoder, out w, out h);
                sz = MyDecoder.DCD_GetPictureSize(m_pDecoder);
                //TODO:draw frame
            }
        }
    }
}
