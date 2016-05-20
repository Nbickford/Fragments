//Almost exactly a copy of the Doughboy class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Doughboy {
    public class Cow : BaseRoutines {
        Vector2 screenPos;
        public Vector2[] waypoints;
        double startTime;
        double bootBeginTime;
        IVEqualityComparer ivec;
        float speed = 1.0f;


        public bool IsMoving;
        public bool HasMoved;
        public bool IsBeingBooted;

        Direction currentDirection; //computed automatically
        int currentAnim;
        int currentFrame;
        int frameRate = 10;

        public IntVector2 arrayPos;

        IntVector2 boardlo = new IntVector2(1, 1);
        IntVector2 boardhi = new IntVector2(13, 11);

        public Cow(int x, int y) {
            screenPos = new Vector2(64 * x, 64 * y);
            waypoints = new Vector2[] { screenPos };
            IsMoving = false;
            HasMoved = false;
            IsBeingBooted = false;
        }

        public Cow(Vector2 ipos, Vector2 epos) {
            screenPos = ipos;
            waypoints = new Vector2[] { ipos, epos };
            IsMoving = true;
            HasMoved = false;
            IsBeingBooted = false;
        }

        public void BeginMoving(GameTime gameTime) {
            startTime = gameTime.TotalGameTime.TotalSeconds;
        }

        public void ChartPath() {
            //TODO: adapt for cows

            IsMoving = true;
            int arX = (int)Math.Round(screenPos.X / 64);
            int arY = (int)Math.Round(screenPos.Y / 64);
            IntVector2 boardPos = new IntVector2(arX, arY);

            IntVector2[] directions = new IntVector2[]{
                new IntVector2(1,0),new IntVector2(-1,0),
                new IntVector2(0,1),new IntVector2(0,-1)};



            bool FoodExists = false;
            for (int y = 0; y < Game1.board.GetLength(0); y++)
                for (int x = 0; x < Game1.board.GetLength(1); x++)
                    if (Game1.board[y, x] == 32 && ((Game1.vars[y, x] >> 8) != 0))
                        FoodExists = true;
            int[] unblocked = new int[] { 0, 4, 8, 10, 11, 12, 32 };

            Random r = new Random();
            if (FoodExists) {
                speed = 2.0f;
                IntVector2 tvec;
                IVEqualityComparer ivec = new IVEqualityComparer();
                Dictionary<IntVector2, IntVector2[]> newpaths = new Dictionary<IntVector2, IntVector2[]>();
                Dictionary<IntVector2, IntVector2[]> newerpaths = new Dictionary<IntVector2, IntVector2[]>(ivec);
                HashSet<IntVector2> allposes = new HashSet<IntVector2>(ivec);//TODO: replace with 2D array
                newerpaths.Add(arrayPos, new IntVector2[] { arrayPos });
                while (newerpaths.Count != 0) {
                    newpaths = new Dictionary<IntVector2, IntVector2[]>(newerpaths, ivec);
                    foreach (KeyValuePair<IntVector2, IntVector2[]> kvp in newpaths) {
                        allposes.Add(kvp.Key);
                        if (Game1.board[kvp.Key.Y, kvp.Key.X] == 32 && ((Game1.vars[kvp.Key.Y, kvp.Key.X] >> 8) != 0)) {
                            waypoints = new Vector2[kvp.Value.Length];
                            for (int i = 0; i < kvp.Value.Length; i++)
                                waypoints[i] = new Vector2(kvp.Value[i].X * 64, kvp.Value[i].Y * 64);

                            return;
                        }
                    }
                    newerpaths.Clear();

                    //generate new positions
                    foreach (KeyValuePair<IntVector2, IntVector2[]> kvp in newpaths) {
                        for (int i = 0; i < 4; i++) {
                            tvec = kvp.Key + directions[i];
                            if (IsInRegion(tvec, boardlo, boardhi))
                                if (Contains(Game1.board[tvec.Y, tvec.X], unblocked) || ((Game1.vars[tvec.Y, tvec.X] & 1) == 1))
                                    if (Game1.board[tvec.Y + 1, tvec.X] != 7 && Game1.board[tvec.Y + 1, tvec.X] != 8)//Cows don't like being behind guillotines. It makes them nervous.
                                        if (!allposes.Contains(tvec, ivec))
                                            if (!newerpaths.ContainsKey(tvec))
                                                newerpaths.Add(tvec, Append(kvp.Value, tvec));
                        }
                    }
                }
                //no solution was found, so try randomized approach
            }
            speed = 1.0f;
            IntVector2 newplace;
            List<IntVector2> directionstogo = directions.ToList<IntVector2>();
            do {
                int n = r.Next(directionstogo.Count);
                newplace = boardPos + directionstogo[n];
                if (IsInRegion(newplace, boardlo, boardhi)) {
                    if (Contains(Game1.board[newplace.Y, newplace.X], unblocked))
                        if (Game1.board[newplace.Y + 1, newplace.X] != 7 && Game1.board[newplace.Y + 1, newplace.X] != 8)//See above.
                            break;
                }

                directionstogo.RemoveAt(n);
            } while (directionstogo.Count != 0);
            if (directionstogo.Count == 0)
                waypoints = new Vector2[] { new Vector2(arX * 64, arY * 64) };//you blocked in the cow!
            else
                waypoints = new Vector2[] { new Vector2(arX * 64, arY * 64), new Vector2(newplace.X * 64, newplace.Y * 64) };
        }

        public IntVector2[] Append(IntVector2[] lis, IntVector2 val) {
            IntVector2[] result = new IntVector2[lis.Length + 1];
            for (int i = 0; i < lis.Length; i++)
                result[i] = lis[i];
            result[lis.Length] = val;
            return result;
        }

        public void PlanMove(GameTime gameTime) {

            if (!IsInRegion(arrayPos, boardlo, boardhi)) {
                if (arrayPos.Y == 0)
                    waypoints = new Vector2[] { arrayPos.ToVector2() * 64, (arrayPos.ToVector2() + Vector2.UnitY) * 64 };
                if (arrayPos.Y == 11)
                    waypoints = new Vector2[] { arrayPos.ToVector2() * 64, (arrayPos.ToVector2() - Vector2.UnitY) * 64 };
                if (arrayPos.X == 0)
                    waypoints = new Vector2[] { arrayPos.ToVector2() * 64, (arrayPos.ToVector2() + Vector2.UnitX) * 64 };
                if (arrayPos.X == 13)
                    waypoints = new Vector2[] { arrayPos.ToVector2() * 64, (arrayPos.ToVector2() - Vector2.UnitX) * 64 };
            } else {
                ChartPath();
            }

            BeginMoving(gameTime);
            IsMoving = true;
            HasMoved = false;
        }


        public bool IsInRegion(IntVector2 pos, IntVector2 lo, IntVector2 hi) {
            return (lo.X <= pos.X) && (pos.X < hi.X) && (lo.Y <= pos.Y) && (pos.Y < hi.Y);
        }

        void NextTurn(GameTime gameTime) {
            IsMoving = false;
            HasMoved = true;
            //set things up for the next player

            //Fun fact: Cows give out money!
            Game1.players[Game1.whoseTurn / 3].money += 100;

            Game1.whoseTurn++;
            if (Game1.whoseTurn >= 6) {
                Game1.numRounds++;
                Game1.whoseTurn = 0;
                Game1.texter.AddMessage(Game1.numRounds.ToString());
            }
            if (Game1.whoseTurn == 3)
                Game1.texter.AddMessage("PG2");
            Game1.breadbox.AnimatedReset(gameTime);

            if (Game1.board[arrayPos.Y, arrayPos.X] == 32)
                Game1.board[arrayPos.Y, arrayPos.X] = 0; //om nom

            //Reset pits
            for (int y = 0; y < Game1.board.GetLength(0); y++)
                for (int x = 0; x < Game1.board.GetLength(1); x++)
                    if (10 <= Game1.board[y, x] && Game1.board[y, x] <= 12)
                        Game1.board[y, x] = 9;

            //reset variables
            for (int y = 0; y < Game1.board.GetLength(0); y++) {
                for (int x = 0; x < Game1.board.GetLength(1); x++) {
                    bool wasDisarmed = false;
                    if ((Game1.vars[y, x] & 1) == 1) {
                        Game1.vars[y, x] &= ~1;
                        wasDisarmed = true;
                    }
                    //item-specific actions
                    switch (Game1.board[y, x]) {
                        case 5:
                            if (wasDisarmed) {
                                Game1.animations.Add(new Animation(18, 64 * new IntVector2(x, y), false, -1, true));
                            }
                            break;
                        case 7:
                            if (wasDisarmed) {
                                Game1.animations.Add(new Animation(13, 64 * new IntVector2(x, y - 1), false, 2, true));
                            }
                            break;
                        case 8:
                            if (wasDisarmed) {
                                Game1.animations.Add(new Animation(14, 64 * new IntVector2(x, y - 1), false, 2, true));
                            }
                            break;
                        case 32:
                            if (Game1.vars[y, x] >> 8 != 0) { //increase age
                                Game1.vars[y, x] -= 0x0100;
                            } else if(Game1.board[y,x]==32){
                                Game1.board[y, x] = 0;
                                Game1.vars[y, x] = 0;//this is to handle fading out if the drawing code didn't work.
                            }
                            break;
                    }
                }
            }

            Game1.turnStart = gameTime.TotalGameTime.TotalSeconds;
        }


        public void Update(GameTime gameTime) {
            if ((!HasMoved) && (!IsMoving)) {
                PlanMove(gameTime);
            }

            double t = gameTime.TotalGameTime.TotalSeconds - startTime;
            int v = (int)Math.Floor(speed * t);
            double m = (speed * t) % 1;

            if ((v >= waypoints.Length - 1) && (!HasMoved)) {

                if (IsInRegion(arrayPos, new IntVector2(1, 5), new IntVector2(2, 7)) || IsInRegion(arrayPos, new IntVector2(12, 5), new IntVector2(13, 7)) || IsBeingBooted) {
                    //cow boot animation
                    if (!IsBeingBooted) {
                        bootBeginTime = gameTime.TotalGameTime.TotalSeconds;
                        IsBeingBooted = true;
                        Game1.hammieham.addCue("cow moo");
                    }


                } else {
                    NextTurn(gameTime);
                }
            }
            if (IsBeingBooted) {
                Vector2 tvec = waypoints[waypoints.Length - 1];
                double tt = gameTime.TotalGameTime.TotalSeconds - bootBeginTime;

                if (tt > 0.75 && (tt < 1 || false)) { //change false to true for Gangnam Style Cow!
                    screenPos = Vector2.Lerp(tvec, 64 * (arrayPos.X <= 2 ? 1 : -1) * Vector2.UnitX + tvec, 4 * (float)(tt - 0.75));
                    if (arrayPos.X <= 2) {
                        currentDirection = Direction.Right;
                    } else {
                        currentDirection = Direction.Left;
                    }
                } else if (tt > 1) {
                    IsBeingBooted = false;
                    if (arrayPos.X <= 2) {
                        screenPos = 64 * Vector2.UnitX + tvec;
                        currentDirection = Direction.Right;
                    } else {
                        screenPos = -64 * Vector2.UnitX + tvec;
                        currentDirection = Direction.Left;
                    }
                    NextTurn(gameTime);
                }
            } else if (IsMoving) {
                screenPos = Vector2.Lerp(waypoints[v], waypoints[v + 1], (float)m);
                Vector2 dir = waypoints[v + 1] - waypoints[v];
                if (dir.X > 0.9)
                    currentDirection = Direction.Right;
                if (dir.X < -0.9)
                    currentDirection = Direction.Left;
                if (dir.Y < -0.9)
                    currentDirection = Direction.Up;
                if (dir.Y > 0.9)
                    currentDirection = Direction.Down;

            } else {
                screenPos = waypoints[waypoints.Length - 1];
                currentDirection = Direction.None;
            }

            arrayPos = new IntVector2((int)Math.Round(screenPos.X / 64), (int)Math.Round(screenPos.Y / 64));


            //Manage animations
            if (currentDirection == Direction.None) {
                currentFrame = 0;
            } else {
                currentAnim = (int)currentDirection;
                currentFrame = (int)((t * frameRate) % 8);
            }

            //manage mooing
            if (Game1.input.mouseState.LeftButton == ButtonState.Pressed && Game1.input.lastMouseState.LeftButton == ButtonState.Released) {
                if (IsInRegion(Game1.input.mousePosition, screenPos, screenPos + 64 * Vector2.One)) {
                    Color[] pxls = new Color[1];
                    Vector2 ipos = Game1.input.mousePosition - screenPos;
                    Game1.cowframes[currentAnim][currentFrame].GetData<Color>(0, new Rectangle((int)ipos.X, (int)ipos.Y, 1, 1), pxls, 0, 1);
                    if (pxls[0].A > 1)
                        Game1.hammieham.addCue("cow moo");
                }
            }

        }




        public void Draw() {

            //EASTER EGG!
            if (Game1.input.IsKeyPressed(Keys.N) && Game1.input.IsKeyPressed(Keys.B) && waypoints.Length == 1)
                Game1.spriteBatch.Draw(Game1.cowframes[currentAnim][currentFrame], screenPos, null, Color.White, Game1.rand.Next(800), Vector2.Zero, 1, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            else
                Game1.spriteBatch.Draw(Game1.cowframes[currentAnim][currentFrame], screenPos, Color.White);
            if (IsBeingBooted) {

                if (arrayPos.X == 1) {
                    Game1.spriteBatch.Draw(Game1.images["zzz 1"], new Vector2(128, screenPos.Y - 32), Color.White);
                } else if (arrayPos.X == 12) {
                    Game1.spriteBatch.Draw(Game1.images["zzz 2"], new Vector2(11 * 64, screenPos.Y - 32), Color.White);
                }
            }
        }
    }
}
