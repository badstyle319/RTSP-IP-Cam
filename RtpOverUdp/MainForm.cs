using System;
using System.Windows.Forms;

namespace RtpOverUdp
{
    public partial class MainForm : Form
    {
        RtpClient mRtpClient = null;

        public MainForm()
        {
            InitializeComponent();

            mRtpClient = new RtpClient("10.2.0.123");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mRtpClient.StopThread();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            mRtpClient.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            mRtpClient.Stop();
        }
    }
}
