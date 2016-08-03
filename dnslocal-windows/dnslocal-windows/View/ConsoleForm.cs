using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DNSlocal.Controller;
using DNSlocal.Util;
using System.IO;

namespace DNSlocal.View
{
    public partial class consoleForm : Form
    {
        private Controller.Controller _controller;

        public consoleForm(Controller.Controller controller)
        {
            InitializeComponent();;
            _controller = controller;
            logTextBox.WordWrap = true;
            logTextBox.AppendText(_controller.Log);
        }

        private void versionLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://coding.net/u/banben/p/dnslocal-windows/git/blob/master/INSTALL.md");
        }

        private async void updateButton_Click(object sender, EventArgs e)
        {
            updateButton.Text = "Updating";
            updateButton.Enabled = false;
            stopButton.Enabled = false;
            await _controller.Update();
            logTextBox.AppendText(_controller.Log);
            if (Controller.Controller.CompareVersion(_controller.LatestSoftVersion, Controller.Controller.SoftVersion) > 0)
            {
                versionLabel.Location = new Point(430, 10);
                versionLabel.Text = "New Version " + _controller.LatestSoftVersion;
            }
            updateButton.Text = "Update";
            startButton.Text = "Start";
            stopButton.Text = "Stop";
            updateButton.Enabled = true;
            stopButton.Enabled = true;
            startButton.Enabled = true;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Text = "Starting...";
            startButton.Enabled = false;
            stopButton.Enabled = false;
            updateButton.Enabled = false;
            string log;
            try
            {
                if (deleteHostsCheckBox.Checked)
                {
                    if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts")))
                        File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts"));
                }
                _controller.Start();
                SystemProxy.SetProxy(true);
                log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Start Success, set http proxy ok, DNS set 127.0.0.1/::1" + Environment.NewLine;
                startButton.Text = "Started";
                startButton.Enabled = false;
                stopButton.Text = "Stop";
                stopButton.Enabled = true;
                updateButton.Enabled = true;
            }
            catch (Exception ex)
            {
                log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Start Failed..." + Environment.NewLine + ex.ToString() + Environment.NewLine;
                startButton.Text = "Start";
                startButton.Enabled = true;
                stopButton.Enabled = true;
                updateButton.Enabled = true;
            }
            logTextBox.AppendText(log);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            stopButton.Text = "Stopping...";
            stopButton.Enabled = false;
            startButton.Enabled = false;
            updateButton.Enabled = false;
            string log;
            try
            {
                _controller.Stop();
                SystemProxy.SetProxy(false);
                log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Stop Success, DNSlocal stopped, unset system proxy ok, DNS automatically" + Environment.NewLine;
                stopButton.Text = "Stopped";
                startButton.Text = "Start";
                startButton.Enabled = true;
                stopButton.Enabled = false;
                updateButton.Enabled = true;
            }
            catch (Exception ex)
            {
                log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Stop Failed..." + Environment.NewLine + ex.ToString() + Environment.NewLine;
                stopButton.Text = "Stop";
                startButton.Enabled = false;
                stopButton.Enabled = true;
                updateButton.Enabled = true;
            }
            logTextBox.AppendText(log);
        }
    }
}
