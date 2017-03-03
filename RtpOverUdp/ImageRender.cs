namespace RtpOverUdp
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.IO;

    public class ImageRender 
    {
        Graphics graphics = null;
        
        public ImageRender(Graphics g)
        {
            this.graphics = g;
        }
        ~ImageRender()
        {
            if (graphics != null)
                graphics.Dispose();
        }

        public Graphics Graphics
        {
            get { return graphics; }
        }
    }
}
