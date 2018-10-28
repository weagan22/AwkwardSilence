using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NoAwkwardSilence
{
    public partial class MainForm : Form
    {
        private Audio audio_ = new Audio();
        private List<AudioSession> sessionList_;
        private AudioSession defaultSession_;
        private int awkwardMeter_ = 0;
        private System.Drawing.Color colorDisabled_ = System.Drawing.Color.Silver;
        private System.Drawing.Color colorEnabled_ = System.Drawing.Color.White;
        public bool isRunning = false;

        public MainForm()
        {
            InitializeComponent();
            delayTrackBar.Value = Properties.Settings.Default.Delay;
            toleranceTrackBar.Value = Properties.Settings.Default.Tolerance;

        }


        // Update the sound source listbox
        private void updateBtn_Click(object sender, EventArgs e)
        {
            sourceListBox.Items.Clear();
            splitContainer.Panel2.Enabled = false;
            splitContainer.Panel2.BackColor = colorDisabled_;
            sessionList_ = audio_.GetAudioSessionList();
            foreach (var session in sessionList_)
            {
                sourceListBox.Items.Add(session.name);
            }
        }

        // Start/stop button click to toggle running state
        private void startBtn_Click(object sender, EventArgs e)
        {
            if (isRunning == false) // Start monitoring the selected sound session
            {
                isRunning = true;

                if (sourceListBox.CheckedItems.Count > 0)
                {
                    string sessionName = sourceListBox.CheckedItems[0].ToString();
                    foreach (var session in sessionList_)
                    {
                        if (session.name.Equals(sessionName))
                        {
                            defaultSession_ = session;
                            timer1.Start();
                            logTextBox.Text = "Start";
                            splitContainer.Panel1.Enabled = false;
                            splitContainer.Panel1.BackColor = colorDisabled_;
                            break;
                        }
                    }
                }
            }
            else  // Stop monintoring sound session
            {
                isRunning = false;
                timer1.Stop();
                logTextBox.Text = "Stop";
                audio_.Mute(defaultSession_, false);
                splitContainer.Panel1.Enabled = true;
                splitContainer.Panel1.BackColor = colorEnabled_;
            }
        }


        // Enable GUI elements when a source is checked
        private void sourceListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked && sourceListBox.CheckedItems.Count > 0)
            {
                sourceListBox.ItemCheck -= sourceListBox_ItemCheck;
                sourceListBox.SetItemChecked(sourceListBox.CheckedIndices[0], false);
                sourceListBox.ItemCheck += sourceListBox_ItemCheck;
            }

            if (e.NewValue == CheckState.Unchecked && sourceListBox.CheckedItems.Count <= 1)
            {
                splitContainer.Panel2.Enabled = false;
                splitContainer.Panel2.BackColor = colorDisabled_;
            }
            else
            {
                splitContainer.Panel2.Enabled = true;
                splitContainer.Panel2.BackColor = colorEnabled_;
            }
        }

        // Check for sound changes every second
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (audio_.IsAwkward(defaultSession_, toleranceTrackBar.Value))
            {
                logTextBox.Text = "Audio: None " + Environment.NewLine + "Sound source: Queued\n";
                if (awkwardMeter_ > delayTrackBar.Value*4)
                {
                    logTextBox.Text = "Audio: None" + Environment.NewLine + "Sound source: ON\n";
                    notifyIcon1.Text = "No Awkward Silence - (ON)";
                    if (muteRadio.Checked)
                    {
                        audio_.Mute(defaultSession_, false);
                        if (!audio_.SessionPlaying(defaultSession_))
                        {
                            audio_.TogglePause(defaultSession_);
                        }
                        
                    }
                    else if (!audio_.SessionPlaying(defaultSession_))
                    {
                        audio_.TogglePause(defaultSession_);
                        audio_.Mute(defaultSession_, false);
                    }
                }
                awkwardMeter_++;

            }
            else
            {
                logTextBox.Text = "Audio: Detected" + Environment.NewLine + "Sound source: OFF\n";
                notifyIcon1.Text = "No Awkward Silence - (OFF)";
                if (muteRadio.Checked)
                {
                    audio_.Mute(defaultSession_, true);
                }
                else if (audio_.SessionPlaying(defaultSession_))
                {
                    audio_.TogglePause(defaultSession_);
                }
                awkwardMeter_ = 0;
            }
        }


        // Save settings as form closes
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            audio_.Mute(defaultSession_, false);
            Properties.Settings.Default.Delay = delayTrackBar.Value;
            Properties.Settings.Default.Tolerance = toleranceTrackBar.Value;
            Properties.Settings.Default.Save();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }


    }

}
