using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace Doughboy {
    public class DJ : BaseRoutines {
        List<string> cues;
        List<Cue> playingCues;
        AudioEngine engine;
        SoundBank soundBank;
        WaveBank waveBank;
        Cue[] backgroundcues;
        public bool isPlayingBackground;
        public bool isPlayingSounds;
        float bV = 2.53f;
        float dV = 2.0f;
        float mV = 0.8f;

        public DJ() {
            //check it, one two
            if (engine != null) engine.Dispose();
            if (waveBank != null) waveBank.Dispose();
            engine = new AudioEngine("Music\\doughboy.xgs");
            soundBank = new SoundBank(engine, "Music\\Sound Bank.xsb");
            waveBank = new WaveBank(engine, "Music\\Wave Bank.xwb");
            cues = new List<string>();
            playingCues = new List<Cue>();

            isPlayingBackground = ReadPreference("Music");
            isPlayingSounds = ReadPreference("Sound Effects");

            engine.GetCategory("Background").SetVolume(isPlayingBackground ? bV : 0);
            engine.GetCategory("Default").SetVolume(isPlayingBackground ? dV : 0);
            engine.GetCategory("Music").SetVolume(isPlayingSounds ? mV : 0);
            backgroundcues = new Cue[] { soundBank.GetCue("battle field intro 2"), soundBank.GetCue("battle field loop 2"), soundBank.GetCue("battle theme"),
            soundBank.GetCue("main menu theme")};

        }

        public void Update() {

            engine.Update();

            while (cues.Count != 0) {
                //we gotta remove from the back, yo
                //otherwise it be slowin' it down
                //and it be makin' the town
                //un-like-a-ble
                //un-accept-a-ble
                //in-corr-ig-ible
                //irr-rasc-ible
                //...that rhyme doesn't work.

                Cue cue = soundBank.GetCue(cues[cues.Count - 1]);
                
                playingCues.Add(cue);
                playingCues.Last().Play();
                cues.RemoveAt(cues.Count - 1);
            }
            for (int i = playingCues.Count-1; i >=0; i--) {
                if (!playingCues[i].IsPlaying) {
                    playingCues.RemoveAt(i);
                }
            }
            if (isPlayingBackground) {
                if (Game1.whoseTurn < 0) {
                    StopAllBackgroundCues(3);
                    if (!backgroundcues[3].IsPlaying) {
                        backgroundcues[3] = soundBank.GetCue("main menu theme");
                        backgroundcues[3].Play();
                    }
                } else {
                    //Stop all background cues immediately
                    if (backgroundcues[3].IsPlaying) backgroundcues[3].Stop(AudioStopOptions.AsAuthored);
                    if (Game1.whoseTurn % 3 == 1 && Game1.breadbox.doughboys.Count > 1) {
                        if (backgroundcues[1].IsPlaying) {
                            backgroundcues[1].Stop(AudioStopOptions.AsAuthored);
                        }
                        if (!backgroundcues[2].IsPlaying) {
                            backgroundcues[2] = soundBank.GetCue("battle theme");
                            backgroundcues[2].Play();
                        }
                    } else {
                        if (backgroundcues[2].IsPlaying) {
                            backgroundcues[2].Stop(AudioStopOptions.AsAuthored);
                        }
                        if (!backgroundcues[1].IsPlaying) {
                            backgroundcues[1] = soundBank.GetCue("battle field loop 2");
                            backgroundcues[1].Play();
                        }
                    }
                }
            }

        }

        private void StopAllBackgroundCues(int exception) {
            for (int i = 0; i < backgroundcues.Length; i++) {
                if (i!=exception && backgroundcues[i].IsPlaying) {
                    backgroundcues[i].Stop(AudioStopOptions.AsAuthored);
                }
            }
        }

        public void addCue(string cue) {
            cues.Add(cue);
        }

        public void StopAllCues(string cue) {
            for (int i = playingCues.Count - 1; i >= 0; i--) {
                if (playingCues[i].Name == cue) {
                    playingCues[i].Stop(AudioStopOptions.AsAuthored);
                    playingCues.RemoveAt(i);
                }
            }
        }

        public void PlayBackgroundMusic() {
            isPlayingBackground = true;
            engine.GetCategory("Background").SetVolume(bV);
            engine.GetCategory("Default").SetVolume(dV);
        }

        public void PlaySoundFX() {
            isPlayingSounds = true;
            engine.GetCategory("Music").SetVolume(mV);
        }

        public void StopBackgroundMusic() {
            isPlayingBackground = false;
            engine.GetCategory("Background").SetVolume(0.0f);
            engine.GetCategory("Default").SetVolume(0.0f);
        }

        public void StopSoundFX() {
            isPlayingSounds = false;
            engine.GetCategory("Music").SetVolume(0.0f);
        }

        public void ToggleBackgroundMusic(bool changePreference) {
            isPlayingBackground ^= true;
            engine.GetCategory("Background").SetVolume(isPlayingBackground ? bV : 0);
            engine.GetCategory("Default").SetVolume(isPlayingBackground ? dV : 0);
            if (changePreference) {
                SetPreference("Music", !ReadPreference("Music"));
            }
        }

        public void ToggleSoundFX(bool changePreference) {
            isPlayingSounds ^= true;
            engine.GetCategory("Music").SetVolume(isPlayingBackground ? mV : 0);
            if (changePreference) {
                SetPreference("Sound Effects", !ReadPreference("Sound Effects"));
            }
        }
    }
}
