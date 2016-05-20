using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Doughboy
{
    public class DoughboyBoxes:BaseRoutines
    {
        IntVector2 center; //really top-center
        public Vector2 topleft;
        public Vector2 btmright; //for use in PurchaseMenu
        public static Dictionary<string,Texture2D> textures;
        public List<Doughboy> doughboys;
        public double count;
        public double instarttime;
        double diftime;
        double indiftime; //TODO: have these be the same.



        /// <summary>
        /// Creates a new DoughboyBoxes- a series of boxes that hold Doughboys (natch)
        /// </summary>
        /// <param name="Center">The top-center of the DoughboyBoxes</param>
        public DoughboyBoxes(IntVector2 Center)
        {
            center = Center;
            Reset();
        }

        public void LoadContent(ContentManager content)
        {
            textures = new Dictionary<string, Texture2D>();
            textures.Add("begin", content.Load<Texture2D>("run-kris"));
            textures.Add("box", content.Load<Texture2D>("doughbox-kris"));
            textures.Add("active box r", content.Load<Texture2D>("doughbox tutorial"));
            textures.Add("active box l", content.Load<Texture2D>("doughbox tutorial flipped"));
        }

        public void Reset()
        {
            topleft = center.ToVector2() - 64 * Vector2.UnitX;
            //textures = new Dictionary<string, Texture2D>();

            doughboys = new List<Doughboy>();
            doughboys.Add(null); //YES THIS HAS A PURPOSE
            count = -1;
            diftime = -1;
            instarttime = -60; //some really small number
        }

        //called by Cow.cs
        public void AnimatedReset(GameTime gameTime)
        {
            instarttime = gameTime.TotalGameTime.TotalSeconds;
            Game1.pm.IsOpening = true;
        }

        public void Update(GameTime gameTime)
        {
            //Process mouse clicks
            if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released) {
                for (int i = 1; i < doughboys.Count; i++) {
                    if (IsInRegion(Game1.input.mousePosition, topleft + 64 * i * Vector2.UnitX, topleft + 64 * (new Vector2(i + 1, 1))) ) {
                        Game1.players[Game1.whoseTurn/3].money+=Game1.pm.GetPrice(128+(int)(doughboys[i].myType));
                        doughboys.RemoveAt(i);
                        break;
                    }
                }
            }

            indiftime=(gameTime.TotalGameTime.TotalSeconds-instarttime);
            if (indiftime < 2)
            {
                if (indiftime < 1)
                {
                    center.Y = (int)(704/* 64*11 */+ indiftime * 128);
                }
                else
                {
                    count = -1;
                    diftime = -1;
                    if (doughboys.Count != 1)
                    {
                        doughboys.Clear();
                        doughboys.Add(null);
                    }
                    center.Y = (int)(704/* 64*12 */+ (2 - indiftime) * 128);
                }
            }
            else if (count < 0)
            {
                center.Y = 704;
            }
            else
            {
                if (count > -0.01)
                {
                    count += gameTime.ElapsedGameTime.TotalSeconds;
                }
                if (count > doughboys.Count - 2)
                {
                    //Withdraw purchase menu
                    //Get set...
                    diftime = count - (doughboys.Count - 2);
                    if (doughboys.Count!=1 && Game1.pm.pos.Y>-65)
                        Game1.pm.pos.Y = -(65) * (float)diftime;
                }
                if (count > doughboys.Count - 1 && Game1.whoseTurn % 3 == 0)
                {
                    Game1.currentDoughboy = 0;
                    Game1.whoseTurn++; //GO!
                }
            }
            //if you don't have any doughboys, just stay there
            if(doughboys.Count==1){
                //Game1.pm.pos.Y = 0;
                center.Y = 704;
                count = -1;
                diftime = -1;
                instarttime = -60;
            }

            topleft = center.ToVector2() - 32 * (doughboys.Count + 1) * Vector2.UnitX;
            btmright = center.ToVector2() + 32 * (doughboys.Count-1) * Vector2.UnitX+64*Vector2.UnitY;
            if ((Game1.input.mouseState.LeftButton == ButtonState.Pressed) && (Game1.input.lastMouseState.LeftButton == ButtonState.Released) && count<0)
            {
                Vector2 gotl=center.ToVector2() + 32 * (doughboys.Count-1) * Vector2.UnitX;
                Vector2 gobr=center.ToVector2() + 32 * (doughboys.Count+1) * Vector2.UnitX+64*Vector2.UnitY;
                if (IsInRegion(Game1.input.mousePosition, gotl, gobr) && Game1.whoseTurn % 3 == 0)
                {
                    count = 0; //Get ready...
                }
            }
        }

        //Called by PurchaseMenu
        public void AddDoughboy()
        {
            //convert selected item to doughboy type
            int type = Game1.pm.objectHeld - 128;
            Game1.pm.objectHeld = -1;

            doughboys.Insert(1,new Doughboy(Game1.whoseTurn/3==0,(DoughboyType)type));
        }


        public void UpdateDoughboy(int n,GameTime gameTime)
        {
            doughboys[doughboys.Count - n-1].Update(gameTime);
        }

        public void DrawDoughboy(int n, GameTime gameTime)
        {
            doughboys[doughboys.Count-n-1].Draw(gameTime);
        }

        public int GetArrayIndex(int n)
        {
            return doughboys.Count - n - 1;
        }

        public void Draw()
        {
            
            for (int i = 0; i < doughboys.Count;i++ )
            {
                bool isLast = (i == 0);
                if (doughboys[i] != null)
                {

                    DrawBox(topleft + 64 * i * Vector2.UnitX, isLast);
                    if (count > (doughboys.Count-i-1))
                    {
                        Game1.spriteBatch.Draw(doughboys[i].frames[((int)(count*6) %6)+(Game1.whoseTurn/3==0?18:6)], topleft + 64 * i * Vector2.UnitX, Color.White);
                    }
                    else
                    {
                        Game1.spriteBatch.Draw(doughboys[i].frames[Game1.whoseTurn/3==0?18:6], topleft + 64 * i * Vector2.UnitX, Color.White);
                    }
                }
                else if (diftime > 0)
                {
                    DrawBox(topleft + 64 * i * Vector2.UnitX + 128 * (float)diftime * Vector2.UnitY, isLast);
                    if (doughboys.Count > 12) Game1.spriteBatch.Draw(Game1.images["noshadow"], topleft + 64 * i * Vector2.UnitX + 128 * (float)diftime * Vector2.UnitY, Color.White);
                }
                else
                {
                    DrawBox(topleft + 64 * i * Vector2.UnitX, isLast);
                    if (doughboys.Count > 12) Game1.spriteBatch.Draw(Game1.images["noshadow"], topleft + 64 * i * Vector2.UnitX, Color.White);
                }
            }
            if (diftime > 0)
            {
                Game1.spriteBatch.Draw(textures["begin"], topleft + 64 * doughboys.Count * Vector2.UnitX + 128 * (float)diftime * Vector2.UnitY, Color.White);
            }
            else
            {
                Game1.spriteBatch.Draw(textures["begin"], topleft + 64 * doughboys.Count * Vector2.UnitX, Color.White);
            }
        }

        public void DrawBox(Vector2 pos, bool isLast) {
            Game1.spriteBatch.Draw(isLast?(Game1.whoseTurn>=3?textures["active box l"]:textures["active box r"]):textures["box"],pos,Color.White);
        }
    }
}
