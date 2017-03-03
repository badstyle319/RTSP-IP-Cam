using System;
using System.Runtime.InteropServices;
using MyExtensions;
using FFmpegSharp.Interop;
using FFmpegSharp.Interop.Codec;
using FFmpegSharp.Interop.Util;

namespace RTSP_IPCam
{
    unsafe class AVDecoder
    {
        AVCodec* codec;
        AVCodecContext* c;
        AVFrame* decode_frame, rgb_frame;
   
        /*extra data*/
        byte[] p_exdata;
        int nexsz;

        /*result image*/
        short m_width, m_height;
        int m_szPicture;
        byte[] rgb_buffer;
        int oflag;

        public AVDecoder()
        {
            FFmpeg.avcodec_init();
            FFmpeg.avcodec_register_all();
            codec = FFmpeg.avcodec_find_decoder(AVCodecID.CODEC_ID_H264);

            c = FFmpeg.avcodec_alloc_context();
            decode_frame = FFmpeg.avcodec_alloc_frame();

            /*extra data*/
            p_exdata = null;
            nexsz = 0;

            /*result image*/
            m_width = 0;
            m_height = 0;
            m_szPicture = 0;
            rgb_buffer = null;
            oflag = 0;
        }
        ~AVDecoder()
        {
            Close();
        }
        public void Close()
        {
            Free(c);
            Free(decode_frame);
            Free(rgb_frame);
            m_szPicture = 0;
            nexsz = 0;
        }
        private void Free(void* ptr)
        {
            if (ptr != null)
            {
                FFmpeg.av_free(ptr);
                ptr = null;
            }
        }

        public bool Open(int nbytes, byte[] extb)
        {
            if (codec == null) return false;
            if (c == null) return false;

            if (oflag != 0) { FFmpeg.avcodec_close(ref *c); oflag = 0; }
            FFmpeg.avcodec_get_context_defaults(ref *c);

            if (FFmpeg.avcodec_open(ref *c, codec) < 0) goto p_clean;
            oflag = 1;

            p_exdata = new byte[64];
            Buffer.BlockCopy(extb, 0, p_exdata, 0, nbytes);
            c->extradata_size = nexsz = nbytes;
            fixed (byte* ptr = p_exdata)
                c->extradata = ptr;

            return true;

        p_clean:
            Close();
            return false;
        }

        public bool Decode(byte[] data, int size)
        {
            bool got_picture = false;
           
            while (size > 0)
            {
                int len = -1;
                fixed (byte* p = data)
                {
                    len = FFmpeg.avcodec_decode_video(ref *c, decode_frame, out got_picture, p, size);
                }
                if (len < 0) return false;

                if (got_picture)
                {
                    AVFrame* rgb24 = get_avframe((int)PixelFormat.PIX_FMT_RGB24, c->width, c->height);
                    if (rgb24 != null)
                        ;
                }
            }
            return (got_picture) ? true : false;
        }

        private AVFrame* get_avframe(int pix_fmt, int width, int height)
        {
            if (m_width != width || m_height != height)
            {
                rgb_buffer = null;
                
                int size = FFmpeg.avpicture_get_size((PixelFormat)pix_fmt, width, height);
                if (size > 0) rgb_buffer = new byte[size];
                if (rgb_buffer == null) return null;

                if (rgb_frame == null) rgb_frame = FFmpeg.avcodec_alloc_frame();
                AVPicture* temp = (AVPicture*)rgb_frame;
                FFmpeg.avpicture_fill(out *temp, rgb_buffer, (PixelFormat)pix_fmt, width, height);
                m_width = (short)width;
                m_height = (short)height;
            }
            return rgb_frame;
        }

        public byte[] GetPicture(out int w, out int h)
        {
            w = this.m_width;
            h = this.m_height;
            return rgb_buffer;
        }
        public int GetPictureSize()
        {
            return m_szPicture;
        }
        public bool IsOpen()
        {
            return (oflag != 0) ? true : false;
        }

    }
}
