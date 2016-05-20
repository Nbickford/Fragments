using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Doughboy {
    public class OptionsMenu {
        Button btn;
        Button btnfull, btnwind;
        Button btnTutorial;
        Button btnMines;
        Button btnClicks;

        Texture2D fullscreen,windowed;
        Texture2D checkbox, uncheck;
        Texture2D pricer;
        bool IsPriceUpHighlighted;
        bool IsPriceDownHighlighted;
        Vector2 lastUDPos;
        public int parentScreen;

        public int[] udvals;
        public int udindex;
        int originaludindex;

        public OptionsMenu() {
            parentScreen = -1;//the title screen
            udvals = new int[] { 0, 25, 100, 150, 200, 250, 500, 1000, 10000 };
            lastUDPos = Vector2.Zero;
        }

        public int LoadUDIndex() {
            if (!int.TryParse(BaseRoutines.ReadPreferenceString("Money"), out udindex)) {
                Console.WriteLine("Hey, something's wrong with your settings file.");
                udindex = 3;
            } else if (udindex < 0 || udindex >= udvals.Length) {
                Console.WriteLine("That index is out of range.");
                udindex = 3;
            }
            originaludindex = udindex;
            return udindex;
        }
        
        public void LoadContent(ContentManager content) {
            btn=new Button(Game1.images["back button"],new Vector2((Game1.screenSize.X-Game1.images["back button"].Width)/2,Game1.screenSize.Y-Game1.images["back button"].Height-16),true);

            fullscreen = content.Load<Texture2D>("technical\\opt full size");
            windowed = content.Load<Texture2D>("technical\\opt half size");
            checkbox = content.Load<Texture2D>("technical\\checkbox checked");
            uncheck = content.Load<Texture2D>("technical\\checkbox");
            pricer = content.Load<Texture2D>("technical\\cost selector");

            //y position will be determined automatically
            btnfull = new Button(fullscreen, new Vector2(Game1.screenSize.X/2 + 8, -256), true);
            btnwind = new Button(windowed, new Vector2(Game1.screenSize.X/2 + 16 + fullscreen.Width, -256), true);

            btnTutorial = new Button(BaseRoutines.ReadPreference("Tutorial") ? checkbox : uncheck, new Vector2(Game1.screenSize.X / 2 + 8, -256), true);
            btnMines = new Button(BaseRoutines.ReadPreference("Mines") ? checkbox : uncheck, new Vector2(Game1.screenSize.X / 2 + 8, -256), true);
            btnClicks = new Button(BaseRoutines.ReadPreference("Clicks") ? checkbox : uncheck, new Vector2(Game1.screenSize.X / 2 + 8, -256), true);
            LoadUDIndex();
        }
        public void Update(GameTime gameTime) {
            btnfull.Update();
            btnwind.Update();
            if (btnfull.IsJustClicked) {
                Game1.graphics.IsFullScreen = true;
                Game1.graphics.ApplyChanges();
                Game1.FitInScreen();
                BaseRoutines.SetPreference("Fullscreen", true);
            }

            if (btnwind.IsJustClicked) {
                Game1.graphics.IsFullScreen = false;
                Game1.graphics.ApplyChanges();
                Game1.FitInScreen();
                BaseRoutines.SetPreference("Fullscreen", false);
            }

            btnTutorial.Update();
            if (btnTutorial.IsJustClicked) {
                Game1.tutorial.HasCompleted ^= true;
                btnTutorial.tex = Game1.tutorial.HasCompleted ? uncheck : checkbox;
                BaseRoutines.SetPreference("Tutorial", !Game1.tutorial.HasCompleted);
            }

            btnMines.Update();
            if(btnMines.IsJustClicked){
                Game1.showEnemyMines^=true;
                btnMines.tex = Game1.showEnemyMines ? checkbox : uncheck;
                BaseRoutines.SetPreference("Mines", Game1.showEnemyMines);
            }

            //Trickiness ensues in order to update buttons within a graphic
            //I don't know why I chose to do that- "cleanliness", perhaps?
            IsPriceDownHighlighted = false;
            IsPriceUpHighlighted = false;
            if (BaseRoutines.IsInRegion(Game1.input.mousePosition, lastUDPos + Vector2.UnitX * 58, lastUDPos + new Vector2(78, 10))) {
                IsPriceUpHighlighted = true;
                if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released
                    && udindex!=udvals.Length-1) {
                        udindex++;
                }
            } else if (BaseRoutines.IsInRegion(Game1.input.mousePosition, lastUDPos + new Vector2(58,16), lastUDPos + new Vector2(78, 26))) {
                IsPriceDownHighlighted = true;
                if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released
                    && udindex!=0) {
                        udindex--;
                }
            }

            btnClicks.Update();
            if (btnClicks.IsJustClicked) {
                Game1.playClicks ^= true;
                btnClicks.tex = Game1.playClicks ? checkbox : uncheck;
                BaseRoutines.SetPreference("Clicks", Game1.playClicks);
            }

            btn.Update();
            if (btn.IsJustClicked) {
                Game1.whoseTurn = parentScreen;
                if (udindex != originaludindex) {
                    BaseRoutines.SetPreferenceString("Money", udindex.ToString());
                    originaludindex = udindex;
                    //reinitialize players
                    for(int i=0;i<Game1.players.Length;i++){
                        Game1.players[i]=new Player(i,udvals[udindex]);
                    }
                }
            }
        }
        public void Draw(GameTime gameTime) {
            //Since this is a separate screen, we use an extra SpriteBatch pass.
            int spacing=16;

            Game1.spriteBatch.Begin();
            DrawScreenLines(gameTime,Vector2.Zero);
            string title=Game1.input.IsKeyHeldDown(Keys.F24)?"THINGS YOU CAN MODIFY!\n AWESOME KEYBOARD BTW ":"THE OPTIONS MENU!";

            Vector2 titlelen=Game1.fonts["cost"].MeasureString(title)*2;
            Game1.spriteBatch.DrawString(Game1.fonts["cost"], title, new Vector2((Game1.screenSize.X - titlelen.X) / 2, 16), Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            float ytop = 16 + (int)titlelen.Y + 3*spacing;

            Vector2 textlen = Game1.fonts["04-18"].MeasureString("Window size:");
            Game1.spriteBatch.DrawString(Game1.fonts["04-18"], "Window size:", new Vector2(Game1.screenSize.X / 2 - textlen.X - 8, trunc(ytop, 2)), Color.White);
            //Dynamically set y position of button. //For the 04-18 font, the baseline is 6 pixels above the bottom.
            btnfull.pos.Y = trunc(ytop + (textlen.Y-6 - btnfull.tex.Height) , 2);
            btnwind.pos.Y = trunc(ytop + (textlen.Y-6 - btnwind.tex.Height) , 2);
            btnfull.Draw();
            btnwind.Draw();
            ytop += textlen.Y + 2*spacing;

            textlen = Game1.fonts["04-18"].MeasureString("Show tutorial again?");
            Game1.spriteBatch.DrawString(Game1.fonts["04-18"], "Show tutorial again?", new Vector2(Game1.screenSize.X / 2 - textlen.X - 8, trunc(ytop, 2)), Color.White);
            btnTutorial.pos.Y = trunc(ytop + (textlen.Y - 6 - btnTutorial.tex.Height), 2);
            btnTutorial.Draw();
            ytop += textlen.Y + 2*spacing;

            textlen = Game1.fonts["04-18"].MeasureString("Initial funds:");
            Game1.spriteBatch.DrawString(Game1.fonts["04-18"], "Initial funds:", new Vector2(Game1.screenSize.X / 2 - textlen.X - 8, trunc(ytop, 2)), Color.White);
            int poff = (int)textlen.Y - 8 - 3*pricer.Height / 4;
            lastUDPos = new Vector2(Game1.screenSize.X / 2 + 8, ytop + poff);
            Game1.spriteBatch.Draw(pricer, lastUDPos, Color.White);
            //highlight arrows
            if (IsPriceDownHighlighted || IsPriceUpHighlighted) {
                if (IsPriceUpHighlighted) {
                    Game1.spriteBatch.Draw(Game1.pixeltex, new Rectangle(Game1.screenSize.X / 2 + 8 + 58, (int)ytop + poff + 2, 20, 10), 
                        Color.FromNonPremultiplied(0,0,0,128)); //QQQ
                } else {
                    Game1.spriteBatch.Draw(Game1.pixeltex, new Rectangle(Game1.screenSize.X / 2 + 8 + 58, (int)ytop + poff + 16, 20, 10),
    Color.FromNonPremultiplied(0,0,0,128)); //QQQ
                }
            }
            string dispcost = udvals[udindex].ToString().PadLeft(5, '0');
            Game1.spriteBatch.DrawString(Game1.fonts["cost"], dispcost, new Vector2(Game1.screenSize.X / 2 + 15, ytop+6), Color.White);
            ytop += textlen.Y + 2 * spacing;

            textlen = Game1.fonts["04-18"].MeasureString("Show enemy mines");
            Game1.spriteBatch.DrawString(Game1.fonts["04-18"], "Show enemy mines", new Vector2(Game1.screenSize.X / 2 - textlen.X - 8, trunc(ytop, 2)), Color.White);
            btnMines.pos.Y = trunc(ytop + (textlen.Y - 6 - btnTutorial.tex.Height), 2);
            btnMines.Draw();
            ytop += textlen.Y + 2 * spacing;

            textlen = Game1.fonts["04-18"].MeasureString("Play five second countdown");
            Game1.spriteBatch.DrawString(Game1.fonts["04-18"], "Play five second countdown", new Vector2(Game1.screenSize.X / 2 - textlen.X - 8, trunc(ytop, 2)), Color.White);
            btnClicks.pos.Y = trunc(ytop + (textlen.Y - 6 - btnTutorial.tex.Height), 2);
            btnClicks.Draw();
            ytop += textlen.Y + 2 * spacing;

            btn.Draw();
            Game1.spriteBatch.End();
        }

        public void DrawScreenLines(GameTime time, Vector2 offset) {
            int xp = (int)(-offset.X / 128);
            for (int x = xp; x <= (int)Game1.screenSize.X / 128 + xp; x++) {
                for (int y = -1; y <= (int)Game1.screenSize.Y / 128; y++)
                    Game1.spriteBatch.Draw(Game1.images["title-bg"], BaseRoutines.trunc(offset + new Vector2(128 * x, 128 * y + 32 * ((float)(time.TotalGameTime.TotalSeconds / 3.2) % 1)), 1), Color.White);
            }
        }

        public int trunc(float val, int mod) {
            return (int)(val - (val % mod));
        }
    }
}
