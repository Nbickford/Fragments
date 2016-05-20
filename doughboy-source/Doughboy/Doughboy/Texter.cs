using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Doughboy {
    public class Texter : BaseRoutines {
        /// <summary>
        /// List of possible messages:
        /// PG1 : Player 1 GO!
        /// PW1 : Player 2 WIN!
        /// (int): ROUND (int)
        /// </summary>
        public List<string> transmissions;

        public Vector2 pos;
        float vel;
        int strsize;
        double starttime;
        bool startanew;
        float at; //The time it takes for messages to cross the screen
        float sp; //extra spacing, for making the transition look nice
        public Texter() {
            transmissions = new List<string>();
            pos = new Vector2(-int.MaxValue, Game1.screenSize.Y / 2);
            strsize = 0;
            starttime = -60;
            vel = -Game1.screenSize.X * 0.5f;
            at = 2.0f;
            sp = 0.5f; //don't use it- it just doesn't look nice
        }

        public void Update(GameTime gameTime) {
            double t = gameTime.TotalGameTime.TotalSeconds;
            if (startanew) {
                starttime = t;
                pos.X = Game1.screenSize.X * 1.5f;
            }
            if (t - starttime > at) {
                if (transmissions.Count > 0) {
                    transmissions.RemoveAt(0);
                    if (transmissions.Count > 0) {
                        strsize = GetMessageLength(transmissions[0]);
                    }
                    //if we don't know the correct size, just leave it as is.
                    //it's a good guess anyways!
                    starttime = t;
                    pos.X = Game1.screenSize.X * 1.5f;
                }
            } else {
                float d = strsize / 2 + 16;
                float f = (float)(t - starttime);

                //method 2
                at = 2.0f;
                bool stopQ = false;
                if(transmissions.Count>0)
                    if(transmissions[0].Length>1)
                        if(transmissions[0][1]=='W')
                            stopQ=true;
                if(stopQ){
                    if (f > at / 2) {
                        pos.X = 0;
                    } else {
                        pos.X = rlog(f - at) - rlog(f) + 2 * f - 2;
                    }
                }else{
                pos.X = rlog(f - at) - rlog(f);
                }
                pos.X *= 88;
                pos.X += Game1.screenSize.X / 2;
            }
            startanew = false;

        }

        /// <summary>
        /// Returns the real part of log(x), for all real x.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public float rlog(float x) {
            return (float)Math.Log(Math.Abs(x));
        }

        public int GetMessageLength(string str) {
            switch (str) {
                case "PG1":
                    return 258;
                case "PG2":
                    return 262;
                case "PW1":
                    return 268;
                case "PW2":
                    return 268;
                default:
                    return Math.Max(202 + 10 + GetIntWidth(int.Parse(str)), 258);
            }
        }

        public void AddMessage(string msg) {
            transmissions.Add(msg);
            //if there was nothing in the queue, start messaging
            if (transmissions.Count == 1) {
                startanew = true;
                strsize = GetMessageLength(transmissions[0]);
            }
        }

        public void Draw() {
            Vector2 npos;
            if (transmissions.Count > 0 && !startanew) //restarting can cause problems with drawing
                switch (transmissions[0]) {
                    case "PG1":
                        npos = new Vector2(Game1.screenSize.X - pos.X, pos.Y);
                        DrawPGoCentered(trunc(npos, 2), 1);
                        break;
                    case "PG2":
                        DrawPGoCentered(trunc(pos, 2), 2);
                        break;
                    case "PW1":
                        DrawPWinCentered(trunc(pos, 2), 1);
                        break;
                    case "PW2":
                        DrawPWinCentered(trunc(pos, 2), 2);
                        break;
                    default:
                        npos = new Vector2(Game1.screenSize.X - pos.X, pos.Y);
                        DrawRoundCentered(trunc(npos - 32 * Vector2.UnitY, 2), int.Parse(transmissions[0]));
                        DrawPGoCentered(trunc(npos + 32 * Vector2.UnitY, 2), 1);
                        break;
                }
        }
    }
}
