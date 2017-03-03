using System;
using System.Windows.Forms;

namespace RTSP_IPCam
{
    public partial class MainForm : Form
    {
        private RtspClient mRtspClient = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            mRtspClient = new RtspClient("10.2.0.123", 554);
            //mRtspClient.NewPacket += new PacketEventHandler(EnqueuePacket);
            mRtspClient.Start();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mRtspClient != null)
                mRtspClient.Stop();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            mRtspClient.Play();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            mRtspClient.Pause();
        }

        private void btnShutDown_Click(object sender, EventArgs e)
        {
            mRtspClient.ShutDown();
        }
    }
}
