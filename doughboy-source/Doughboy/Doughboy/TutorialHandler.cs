using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Doughboy {
    public class TutorialHandler {
        Texture2D arrow;
        Vector2 center;

        double startTime;
        Vector2 tpos;
        Vector2 pos;
        float rotation;
        public bool HasCompleted;
        bool IsVisible;
        int tutorialStage;

        public TutorialHandler() {
            HasCompleted = false;
            startTime = 0;
            tutorialStage = 0;
            IsVisible = true;

        }
        public void LoadContent(ContentManager content) {
            arrow = content.Load<Texture2D>("a down arrow");
            center = new Vector2(arrow.Bounds.Center.X, arrow.Bounds.Center.Y);
            HasCompleted = !BaseRoutines.ReadPreference("Tutorial");
        }

        public void Update(GameTime gameTime) {
            double s = gameTime.TotalGameTime.TotalSeconds;
            double ds = s - startTime;
            int h = 64 + 16;
            if (!HasCompleted) {
                switch (tutorialStage) {
                    case 0:
                        rotation = -MathHelper.PiOver2;
                        pos = new Vector2(Game1.screenSize.X - 64 - 17 + 4 * (float)Math.Sin(2 * Math.PI * s), 32);
                        if (Game1.pm.pos.X < pos.X - center.X && Game1.numRounds>1) {
                            tutorialStage = 2;
                            startTime = s;
                        }
                        break;
                    case 2:
                        IsVisible = false;
                        if (Game1.pm.objectHeld >= 128) {
                            startTime = s;
                            tutorialStage = 3;
                            Game1.hammieham.addCue("jump");
                        }
                        break;
                    case 3:
                        IsVisible = true;
                        pos.X = Game1.screenSize.X / 2 - 32;
                        rotation = 0;
                        if (ds < 0.5f) {
                            //The arrow "springs" up above the doughboy box
                            pos.Y = Game1.screenSize.Y - (float)(4 * h * ds * (1 - ds)) - 5;
                        } else {
                            pos.Y = Game1.screenSize.Y - (h + 1 + 4 * (float)Math.Cos(2 * Math.PI * (ds - 0.5)));
                        }
                        if (Game1.whoseTurn % 3 != 0 || Game1.breadbox.doughboys.Count > 1 || Game1.pm.objectHeld < 128) {
                            startTime = s;
                            tutorialStage = 4;
                            tpos = new Vector2(4 * (float)Math.Sin(2 * Math.PI * (ds - 0.5)), pos.Y); // x is the velocity, y is the position.
                        }
                        break;
                    case 4:
                        pos.X = Game1.screenSize.X / 2 - 32;
                        if (pos.Y < Game1.screenSize.Y) {
                            pos.Y = tpos.Y + (float)(ds * (tpos.X + 4 * h * ds));
                        } else {
                            if (Game1.breadbox.doughboys.Count > 1) {
                                tutorialStage = 5;
                            } else if (Game1.pm.objectHeld >= 128) {
                                startTime = s;
                                tutorialStage = 3;
                                Game1.hammieham.addCue("jump");
                            }
                        }

                        break;
                    case 5:
                        if (Game1.whoseTurn % 3 == 2) {
                            HasCompleted = true;
                            BaseRoutines.SetPreference("Tutorial", false);
                        }
                        break;
                }
            }
        }

        public void Draw(GameTime gameTime) {
            if (IsVisible && !HasCompleted) {
                if (tutorialStage == 4 || Game1.breadbox.doughboys.Count > 1) {
                    string text = "USE " + "     " + " TO CONTROL THE DOUGHBOY!";
                    Vector2 strlen = 2 * Game1.fonts["cost"].MeasureString(text);
                    Vector2 strpos = new Vector2((Game1.screenSize.X - strlen.X) / 2, 10 * 64 + (64 - strlen.Y) / 2);
                    Vector2 ksize = getwh(Game1.animationframes[27][0].Bounds);
                    Vector2 kpos = strpos + 2 * Game1.fonts["cost"].MeasureString("USE ") + (2 * Game1.fonts["cost"].MeasureString("     ") - ksize) / 2;
                    kpos.Y = strpos.Y + Game1.fonts["cost"].MeasureString("USE ").Y - ksize.Y / 2;
                    Game1.spriteBatch.DrawString(Game1.fonts["cost"], text, strpos, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
                    Game1.spriteBatch.Draw(Game1.animationframes[27][(int)(gameTime.TotalGameTime.TotalSeconds * 10) % 16],
                        kpos, Color.White);
                } else {
                    Game1.spriteBatch.Draw(arrow, BaseRoutines.trunc(pos, 2), null, Color.White, rotation, center, 1, SpriteEffects.None, 0);
                }
            }
        }

        public Vector2 getwh(Rectangle rect) {
            return new Vector2(rect.Width, rect.Height);
        }
    }
}
