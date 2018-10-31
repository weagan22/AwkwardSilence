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
        private int awkwardMeter_ = 20;
        private System.Drawing.Color colorDisabled_ = System.Drawing.Color.Silver;
        private System.Drawing.Color colorEnabled_ = System.Drawing.Color.White;
        private bool isRunning = false;
        private bool isOn = false;

        public MainForm()
        {
            InitializeComponent();
            delayTrackBar.Value = Properties.Settings.Default.Delay;
            toleranceTrackBar.Value = Properties.Settings.Default.Tolerance;
            muteRadio.Checked = Properties.Settings.Default.Mode;
            playRadio.Checked = !Properties.Settings.Default.Mode;
            chkMuteAds.Checked = Properties.Settings.Default.Mute;
            updateBtn_Click(null, EventArgs.Empty);
            if (sourceListBox.CheckedItems.Count==1)
            {
                startBtn_Click(null, EventArgs.Empty);
            }
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
                if (session.name != "Idle") //Skips adding system sounds as a default sounds source.
                {
                    sourceListBox.Items.Add(session.name);
                }
                
                if (session.name == "Spotify" && sourceListBox.CheckedItems.Count == 0)
                {
                    sourceListBox.SetItemChecked(sourceListBox.Items.Count-1, true);
                }
            }
        }

        // Start/stop button click to toggle running state
        private void startBtn_Click(object sender, EventArgs e)
        {
            if (isRunning == false) // Start monitoring the selected sound session
            {
                isRunning = true;
                startBtn.BackColor = System.Drawing.Color.LightCoral;
                startBtn.Text = "Stop";

                if (sourceListBox.CheckedItems.Count > 0)
                {
                    string sessionName = sourceListBox.CheckedItems[0].ToString();
                    foreach (var session in sessionList_)
                    {
                        if (session.name.Equals(sessionName))
                        {
                            defaultSession_ = session;
                            timer1.Start();
                            logTextBox.Text = "Starting...";
                            notifyIcon1.Text = "No Awkward Silence - Running";
                            this.Text = "No Awkward Silence - Running";
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
                startBtn.BackColor = SystemColors.ButtonFace;
                startBtn.Text = "Start";

                timer1.Stop();
                logTextBox.Text = "Stopped";
                notifyIcon1.Text = "No Awkward Silence - Stopped";
                this.Text = "No Awkward Silence - Stopped";
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
                startStopToolStripMenuItem.Enabled = false;
                splitContainer.Panel2.Enabled = false;
                splitContainer.Panel2.BackColor = colorDisabled_;
            }
            else
            {
                startStopToolStripMenuItem.Enabled = true;
                splitContainer.Panel2.Enabled = true;
                splitContainer.Panel2.BackColor = colorEnabled_;
            }
        }

        // Check for sound changes every second
        private void timer1_Tick(object sender, EventArgs e)
        {
            //Check if there is an ad playing and mute if there is.
            if (chkMuteAds.Checked && defaultSession_.name == "Spotify")
            {
                Process[] spotifyProcessX;
                spotifyProcessX = Process.GetProcessesByName("Spotify");

                for (int i = 0; (i < spotifyProcessX.Length); i++)
                {
                    if (spotifyProcessX[i].MainWindowTitle.Length > 0 && spotifyProcessX[i].MainWindowTitle != "Spotify")
                    {
                        if (spotifyProcessX[i].MainWindowTitle.IndexOf("-") == -1)
                        {
                            audio_.Mute(defaultSession_, true);
                            logTextBox.Text = "Spotify Muted: Advertisement";
                            return;
                        }
                        else if(isOn == true)
                        {
                            audio_.Mute(defaultSession_, false);
                        }

                    }
                }
            }

            //Check for awkward silence
            if (audio_.IsAwkward(defaultSession_, toleranceTrackBar.Value))
            {
               
                logTextBox.Text = "Audio: None " + Environment.NewLine + "Sound source: Queued\n" + Environment.NewLine + "Countdown: " + (delayTrackBar.Value - awkwardMeter_);
                if (awkwardMeter_ > (delayTrackBar.Value))
                {
                    logTextBox.Text = "Audio: None" + Environment.NewLine + "Sound source: ON\n";
                    notifyIcon1.Text = "No Awkward Silence - Running (ON)";
                    isOn = true;
                    
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
                notifyIcon1.Text = "No Awkward Silence - Running (OFF)";
                isOn = false;
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
            Properties.Settings.Default.Mode = muteRadio.Checked;
            Properties.Settings.Default.Mute = chkMuteAds.Checked;
            Properties.Settings.Default.Save();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(50);
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



        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                // WinForms app
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                // Console app
                System.Environment.Exit(1);
            }
        }

        private void startStopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startBtn_Click(null, EventArgs.Empty);
        }

    }

}
