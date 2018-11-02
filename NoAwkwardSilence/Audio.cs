

    //No Awkward Silence
    //Copyright (C) <2018>  <Will Eagan>

    //This program is free software: you can redistribute it and/or modify
    //it under the terms of the GNU General Public License as published by
    //the Free Software Foundation, either version 3 of the License, or
    //(at your option) any later version.

    //This program is distributed in the hope that it will be useful,
    //but WITHOUT ANY WARRANTY; without even the implied warranty of
    //MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    //GNU General Public License for more details.

    //You should have received a copy of the GNU General Public License
    //along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoAwkwardSilence
{
    // AudioSession struct, save sessions
    public struct AudioSession
    {
        public string name { get; private set; }
        public Guid groupingParam {get; private set; }

        public AudioSession(string name, Guid groupingParam) : this()
        {
            this.name = name;
            this.groupingParam = groupingParam;
        }
    }

    class Audio
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0xE0000;
        const int WM_APPCOMMAND = 0x319;


        // Get a list of all active audiosessions
        public List<AudioSession> GetAudioSessionList()
        {
            List<AudioSession> sessionList = new List<AudioSession>();
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                        using (var session2 = session.QueryInterface<AudioSessionControl2>())
                        {
                            if (session2 != null && session2.Process != null)
                            {
                                sessionList.Add(new AudioSession(session2.Process.ProcessName, session.GroupingParam));
                            }
                        }
                    }
                }
            }
            return sessionList;
        }

        // Check if there is any other sound playing except the defaulsession
        public bool IsAwkward(AudioSession defaultSession, float tolerance)
        {
            tolerance *= tolerance;
            if (tolerance < 1) tolerance = 1;
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        if(session.GroupingParam != defaultSession.groupingParam && session.DisplayName.IndexOf("SystemRoot")==-1 && session.DisplayName!= "") //Ignores system sounds and sounds from default session.
                        {
                            using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                            {
                                if (audioMeterInformation.PeakValue > tolerance / 1000)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        // Pause or Unpause audiosession
        public void TogglePause(AudioSession defaultSession)
        {
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                        using (var session2 = session.QueryInterface<AudioSessionControl2>())
                        {
                            if (session.GroupingParam == defaultSession.groupingParam && session2.Process != null)
                            {
                                SendMessageW(session2.Process.MainWindowHandle, WM_APPCOMMAND, session2.Process.MainWindowHandle, (IntPtr)APPCOMMAND_MEDIA_PLAY_PAUSE); 
                            }
                        }
                    }
                }
            }
        }

        // Mute or unmute the audioSession
        public void Mute(AudioSession defaultSession, bool mute)
        {
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        if (session.GroupingParam == defaultSession.groupingParam)
                        {
                            using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                            {
                                simpleVolume.IsMuted = mute;
                            }
                        }
                    }
                }
            }
        }

        // Check if a given session currently is play audio
        public bool SessionPlaying(AudioSession defaultSession)
        {
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        if (session.GroupingParam == defaultSession.groupingParam)
                        {
                            using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                            {
                                // Sometimes very very small but not 0 even when not playing
                                if(audioMeterInformation.PeakValue > 0.0001)
                                {
                                    return true;
                                }

                                //Wait and check to see if it is playing again in case player is between songs
                                System.Threading.Thread.Sleep(50);

                                if (audioMeterInformation.PeakValue > 0.0001)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    return AudioSessionManager2.FromMMDevice(device);
                }
            }
        }
    }
}
