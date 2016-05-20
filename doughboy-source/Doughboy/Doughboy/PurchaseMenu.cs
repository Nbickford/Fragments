using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Doughboy {
    public class PurchaseMenu : BaseRoutines {
        public Vector2 pos;
        float openpos;
        float closepos;
        Direction openDirection;
        double totalTime;
        Dictionary<String, Texture2D> textures;
        int[] prices;
        int[] objects;
        public int objectHeld;

        public bool IsOpening;

        public PurchaseMenu() {

            textures = new Dictionary<String, Texture2D>();
            objects = new int[] { 1, 5, 6, 7, 9,  5, 32, 128, 129, 130, 131 }; //Wall, mine, spring, guillotine, pit, smartmine, food, doughboy, doughbomb, driller, disarmer
            prices = new int[] { 20, 30, 50, 50, 75, 75, 25, 30, 100, 50, 100 };

            pos = new Vector2((Game1.board.GetLength(1) - 1) * 64, 0);
            closepos = pos.X;
            openpos = (Game1.board.GetLength(1) - 1 - objects.Length) * 64;
            openDirection = Direction.Right;
            totalTime = 1;//already closed


            objectHeld = -1;
            IsOpening = false;
        }

        public int GetPrice(int obj) {
            //could use binary search, but not enough values in the array to matter
            for (int i = 0; i < objects.Length; i++) {
                if (objects[i] == obj) {
                    return prices[i];
                }
            }
            return -1;
        }

        public void LoadContent(ContentManager content) {
            textures = new Dictionary<String, Texture2D>();
            textures.Add("open", content.Load<Texture2D>("open-kris"));
            textures.Add("opencont", content.Load<Texture2D>("opencont-kris"));
            textures.Add("moneybox", content.Load<Texture2D>("kris money 2"));
            //QQQ
            string folder = "shop icons\\shop icons-";
            textures.Add("0", content.Load<Texture2D>(folder + "wall"));
            textures.Add("1", content.Load<Texture2D>(folder + "mine"));
            textures.Add("2", content.Load<Texture2D>(folder + "spring board"));
            textures.Add("3", content.Load<Texture2D>(folder + "guillotine"));
            textures.Add("4", content.Load<Texture2D>(folder + "pit trap"));
            textures.Add("5", content.Load<Texture2D>(folder + "mine"));
            textures.Add("6", content.Load<Texture2D>(folder + "bait"));
            textures.Add("7", content.Load<Texture2D>(folder + "doughboy"));
            textures.Add("8", content.Load<Texture2D>(folder + "doughbomb"));
            textures.Add("9", content.Load<Texture2D>(folder + "drillboy"));
            textures.Add("10", content.Load<Texture2D>(folder + "disarmboy"));
        }

        public void Update(GameTime gameTime) {
            totalTime += gameTime.ElapsedGameTime.TotalSeconds;
            //if clicked, change direction and open/close
            if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released) {
                if (IsInRegion(Game1.input.mousePosition, (int)pos.X, (int)pos.X + 64, (int)pos.Y, (int)pos.Y + 64)) {
                    if (openDirection == Direction.Right) openDirection = Direction.Left;
                    else openDirection = Direction.Right;
                    totalTime = 0;
                }
            }

            //position
            switch (openDirection) {
                case Direction.Right:
                    if (totalTime < 1) {
                        pos.X = Vector2.Hermite(openpos * Vector2.UnitX, Vector2.Zero, closepos * Vector2.UnitX, 64 * Vector2.UnitX, (float)totalTime).X;
                    } else {
                        pos.X = closepos;
                    }
                    break;

                case Direction.Left:
                    if (totalTime < 1) {
                        pos.X = Vector2.Hermite(closepos * Vector2.UnitX, -64 * Vector2.UnitX, openpos * Vector2.UnitX, Vector2.Zero, (float)totalTime).X;
                    } else {
                        pos.X = openpos;
                    }
                    break;

            }

            if (IsOpening) {
                if (pos.Y < 0) {
                    pos.Y += (float)gameTime.ElapsedGameTime.TotalSeconds * 64;
                } else {
                    IsOpening = false;
                    //There is a small but nonzero chance that we have overshot.
                    pos.Y = 0;
                }
            }
            //Pixelize position
            pos = trunc(pos, 2);

            //Actual functional stuff
            if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released) {
                if (IsInRegion(Game1.input.mousePosition, (int)pos.X + 64, (int)pos.X + 64 * (objects.Length + 1), (int)pos.Y, (int)pos.Y + 64)) {
                    //get index
                    int itemIndex = (int)(Game1.input.mousePosition.X - (pos.X + 64)) / 64;
                    
                    if (prices[itemIndex] <= Game1.players[Game1.whoseTurn / 3].money && (!(objects[itemIndex] >= 128 && Game1.numRounds == 1))) {
                        objectHeld = objects[itemIndex];
                        Game1.players[Game1.whoseTurn / 3].money -= prices[itemIndex];
                    }
                    //if not, beep?
                }
            }

            if (objectHeld != -1 && Game1.input.mouseState.LeftButton == ButtonState.Released && Game1.input.lastMouseState.LeftButton == ButtonState.Pressed) {
                if (objectHeld >= 128) {


                    if (!IsInRegion(Game1.input.mousePosition, Game1.breadbox.topleft, Game1.breadbox.btmright) && Game1.breadbox.doughboys.Count <= (12 + 1)) {
                        RejectItem();
                    } else {
                        Game1.breadbox.AddDoughboy();
                    }
                } else {
                    if (IsInOkayRegion()) {
                        IntVector2 arrayPos = new IntVector2((int)Game1.input.mousePosition.X / 64, (int)Game1.input.mousePosition.Y / 64);

                        if (!IsSpaceOkay(arrayPos)) {
                            RejectItem();
                        } else {
                            if (Game1.board[arrayPos.Y, arrayPos.X] == 5) {
                                //You placed something on a mine!
                                Game1.board[arrayPos.Y, arrayPos.X] = 4;//rubble, to signify a mine
                                Game1.animations.Add(new Animation(7, arrayPos * 64, false, 1));
                                Game1.hammieham.addCue("splosion2");//BOOM!
                            } else {
                                Game1.board[arrayPos.Y, arrayPos.X] = objectHeld;
                                int t = 0;
                                t = (Game1.whoseTurn / 3) << 4; //set owner of piece. By default, it's disarmed.
                                switch (Game1.board[arrayPos.Y, arrayPos.X]) {
                                    case 32: Game1.vars[arrayPos.Y, arrayPos.X] = t + (3 << 8);//set number of rounds to go
                                        break;
                                    default:
                                        Game1.vars[arrayPos.Y, arrayPos.X] = t; //get lower byte, since this will have no variables.
                                        break;
                                }
                            }
                            objectHeld = -1;
                        }
                    } else {
                        RejectItem();
                    }
                }
            }
        }

        private void RejectItem() {
            //refund, drop item
            for (int i = 0; i < objects.Length; i++)
                if (objects[i] == objectHeld)
                    Game1.players[Game1.whoseTurn / 3].money += prices[i];
            objectHeld = -1;
        }

        public void Draw() {
            Game1.spriteBatch.Draw(textures["open"], pos, Color.White);
            //draw prices
            Vector2 spacing, place, strsize;
            for (int i = 0; i < objects.Length; i++) {
                strsize = Game1.fonts["cost"].MeasureString("$" + prices[i].ToString());
                spacing = new Vector2((64 - strsize.X) / 2, (64 - strsize.Y - 6));
                place = pos + 64 * (i + 1) * Vector2.UnitX;
                Game1.spriteBatch.Draw(textures["opencont"], place, Color.White);
                int playermoney = Game1.players[Game1.whoseTurn / 3].money;
                if (objects[i] >= 128) {
                    DrawFlippedImage(textures[i.ToString()], place + new Vector2(16, 8), (Game1.numRounds == 1 || playermoney < prices[i]) ? Color.DarkGray : Color.White);
                } else {
                    Game1.spriteBatch.Draw(textures[i.ToString()], place + new Vector2(16, 8), (playermoney < prices[i]) ? Color.DarkGray : Color.White);
                }
                Game1.spriteBatch.DrawString(Game1.fonts["cost"], "$" + prices[i].ToString(), trunc(place + spacing, 2) + Vector2.UnitX, Color.Black);


            }

            if (IsInRegion(Game1.input.mousePosition, (int)pos.X + 64, (int)pos.X + 64 * (objects.Length + 1), (int)pos.Y, (int)pos.Y + 64)) {
                 int itemIndex = (int)(Game1.input.mousePosition.X - (pos.X + 64)) / 64;
                if(itemIndex==5){
                    Game1.spriteBatch.DrawString(Game1.fonts["cost"], "Not implemented yet!",
                        Game1.input.mousePosition+new Vector2(24,0),Color.White);
                 }
            }


            //draw money box

            string dispnum = Game1.players[Game1.whoseTurn / 3].money.ToString();
            dispnum = dispnum.PadLeft(5, '0');
            strsize = Game1.fonts["cost"].MeasureString(dispnum);
            Game1.spriteBatch.DrawString(Game1.fonts["cost"], dispnum, new Vector2((int)Game1.screenSize.X - 27 - strsize.X / 2, 64 + 5 + pos.Y), Color.FromNonPremultiplied(255,224,6,255));
            Game1.spriteBatch.Draw(textures["moneybox"], new Vector2((int)Game1.screenSize.X - 27 * 2, 64 + 14 + pos.Y), Color.White);

            //draw object held
            //TODO: replace this with a lookup in an array
            switch (objectHeld) {
                case 1:
                    DrawShadow();
                    Game1.spriteBatch.Draw(Game1.images["wall"], Game1.input.mousePosition - 32 * Vector2.One, Color.White);

                    break;
                case 5:
                    DrawShadow();
                    Game1.spriteBatch.Draw(Game1.images["mine"], Game1.input.mousePosition - 32 * Vector2.One, Color.White);
                    break;
                case 6:
                    DrawShadow();
                    Game1.spriteBatch.Draw(Game1.images["spring"], Game1.input.mousePosition - 32 * Vector2.One, Color.White);
                    break;
                case 7:
                    DrawShadow();
                    Game1.spriteBatch.Draw(Game1.animationframes[10][0], Game1.input.mousePosition - 32 * Vector2.One - 64 * Vector2.UnitY, Color.White);
                    Game1.spriteBatch.Draw(Game1.animationframes[11][0], Game1.input.mousePosition - 32 * Vector2.One - 64 * Vector2.UnitY, Color.White);
                    break;
                case 9:

                    Game1.spriteBatch.Draw(Game1.images["pit"], trunc(Game1.input.mousePosition, 64), Color.White);
                    break;
                case 32:
                    DrawShadow(); //need to fix shadow here
                    Game1.spriteBatch.Draw(Game1.images["cow bait 0"], Game1.input.mousePosition - 32 * Vector2.One, Color.White);
                    break;
                case 128:
                    Game1.spriteBatch.Draw(Game1.doughboyframes[0][Game1.whoseTurn / 3 == 0 ? 18 : 6], Game1.input.mousePosition - 24 * Vector2.One, Color.White);
                    break;
                case 129:
                    Game1.spriteBatch.Draw(Game1.doughboyframes[1][Game1.whoseTurn / 3 == 0 ? 18 : 6], Game1.input.mousePosition - 24 * Vector2.One, Color.White);
                    break;
                case 130:
                    Game1.spriteBatch.Draw(Game1.doughboyframes[2][Game1.whoseTurn / 3 == 0 ? 18 : 6], Game1.input.mousePosition - 24 * Vector2.One, Color.White);
                    break;
                case 131:
                    Game1.spriteBatch.Draw(Game1.doughboyframes[3][Game1.whoseTurn / 3 == 0 ? 18 : 6], Game1.input.mousePosition - 24 * Vector2.One, Color.White);
                    break;
            }
        }

        private bool IsInOkayRegion() {
            if (IsInRegion(Game1.input.mousePosition, 2 * 64, 12 * 64, 2 * 64, 10 * 64) &&
                            !IsInRegion(Game1.input.mousePosition, (int)pos.X + 64, (int)pos.X + 64 * (objects.Length + 1), (int)pos.Y, (int)pos.Y + 64) &&
                            !IsInRegion(Game1.input.mousePosition, 1 * 64, 3 * 64, 5 * 64, 7 * 64) &&
                            !IsInRegion(Game1.input.mousePosition, 11 * 64, 13 * 64, 5 * 64, 7 * 64)) {
                if (Game1.currentCow != null) {
                    if (Game1.currentCow.arrayPos * 64 != new IntVector2(trunc(Game1.input.mousePosition, 64))) {
                        return true;
                    }
                } else {
                    return true;
                }
            }
            return false;
        }

        private void DrawFlippedImage(Texture2D img, Vector2 pos, Color c) {
            Game1.spriteBatch.Draw(img, pos, null, c, 0, Vector2.Zero, 1, Game1.whoseTurn < 3 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        private bool IsSpaceOkay(IntVector2 p) {
            //empty, rubble, mine (maniacal laugh) or nonexistent cow bait
            return (
                Game1.board[p.Y, p.X] == 0 ||
                Game1.board[p.Y, p.X] == 4 ||
                Game1.board[p.Y,p.X]==5 ||
                (Game1.board[p.Y, p.X] == 32 && (Game1.vars[p.Y, p.X] >> 8 == 0)));
        }

        private void DrawShadow() {
            if (IsInOkayRegion()) {
                IntVector2 arrayPos = new IntVector2((int)Game1.input.mousePosition.X / 64, (int)Game1.input.mousePosition.Y / 64);
                if (IsSpaceOkay(arrayPos)) {
                    Game1.spriteBatch.Draw(Game1.images["shadow"], trunc(Game1.input.mousePosition, 64), Color.White);
                } else {
                    Game1.spriteBatch.Draw(Game1.images["noshadow"], trunc(Game1.input.mousePosition, 64), Color.White);
                }
            } else
                Game1.spriteBatch.Draw(Game1.images["noshadow"], trunc(Game1.input.mousePosition, 64), Color.White);
        }
    }
}
