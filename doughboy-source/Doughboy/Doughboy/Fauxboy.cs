using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Doughboy {

    public class Fauxboy : BaseRoutines {
        DoughboyType myType;

        public Vector2 screenPos;
        Vector2 sOffset;

        Vector2[] waypoints;
        double startTime;
        double jumpTime;
        double wallTime;

        bool IsOnWall;
        bool IsInAir;

        int numToDrill;


        int springJumpPhase;

        IVEqualityComparer ivec;
        float speed = 5.0f;

        Direction movingDirection;

        public Texture2D[] frames;
        public int currentFrame;
        public int currentAnim;

        bool leftSideQ;

        //Parameters specific to the Fauxboy class
        public string[] instructions;
        public char keyPressed;
        public char lastKeyPressed;
        public int currentInstruction;
        //chars: 'j':jump   'n':north   'w':west   's':south    'e':east

        public Fauxboy(string Instructions) {
            instructions = Instructions.Split('\n');
            //parse first line of instructions
            int tside, ttype;
            if (!(int.TryParse(instructions[0].Substring(5, 1), out ttype) && int.TryParse(instructions[0].Substring(7, 1), out tside))) {
                throw new Exception("Someone's been messing with your intro.dboy file! #1");
            }
            if (tside > 1 || ttype > 3) {
                throw new Exception("Someone's been messing with your intro.dboy file! #2");
            }

            leftSideQ = (tside == 1);
            myType = (DoughboyType)ttype;

            if (leftSideQ) {
                screenPos = new Vector2(64 * 1, 64 * 5);
            } else {
                screenPos = new Vector2(64 * 12, 64 * 5);
            }

            frames = new Texture2D[6];
            ivec = new IVEqualityComparer();
            jumpTime = 60;//<some large value>
            startTime = -1;

            frames = Game1.doughboyframes[(int)myType];

            currentFrame = 0;
            currentAnim = leftSideQ ? 18 : 6;
            sOffset = Vector2.Zero;

            springJumpPhase = -1;
            IsOnWall = false;
            IsInAir = false;

            numToDrill = 3;
            currentInstruction = 1;

            movingDirection = leftSideQ ? Direction.Right : Direction.Left;
        }



        public void ChartPath(GameTime gameTime) {
            startTime = gameTime.TotalGameTime.TotalSeconds;
            int arX = (int)Math.Round(screenPos.X / 64);
            int arY = (int)Math.Round(screenPos.Y / 64);
            Dictionary<IntVector2, IntVector2[]> nodes = new Dictionary<IntVector2, IntVector2[]>(ivec);
            Dictionary<IntVector2, IntVector2[]> lastnodes = new Dictionary<IntVector2, IntVector2[]>(ivec);
            Dictionary<IntVector2, IntVector2[]> beforethat = new Dictionary<IntVector2, IntVector2[]>(ivec);

            IntVector2[] directions = new IntVector2[]{
                new IntVector2(1,0),new IntVector2(-1,0),
                new IntVector2(0,1),new IntVector2(0,-1)};

            IntVector2 lo = new IntVector2(0, 0);
            IntVector2 hi = new IntVector2(14, 12);
            IntVector2 goal = leftSideQ ? new IntVector2(12, 5) : new IntVector2(1, 5);

            #region search1
            nodes.Add(new IntVector2(arX, arY), new IntVector2[] { new IntVector2(arX, arY) });
            while (nodes.Count != 0) {
                //Check for positions
                foreach (KeyValuePair<IntVector2, IntVector2[]> pair in nodes) {
                    if (pair.Key.X == goal.X) {
                        waypoints = new Vector2[pair.Value.Length];
                        for (int i = 0; i < pair.Value.Length; i++) {
                            waypoints[i] = new Vector2(64 * pair.Value[i].X, 64 * pair.Value[i].Y);
                        }
                        return;
                    }
                }

                beforethat = new Dictionary<IntVector2, IntVector2[]>(lastnodes, ivec);
                lastnodes = new Dictionary<IntVector2, IntVector2[]>(nodes, ivec);
                nodes.Clear();
                foreach (KeyValuePair<IntVector2, IntVector2[]> pair in lastnodes) {
                    for (int i = 0; i < 4; i++) {
                        IntVector2 tvector = pair.Key + directions[i];
                        if (IsInRegion(tvector, lo, hi)) {
                            if (Game1.board[tvector.Y, tvector.X] == 0)
                                if (!(beforethat.ContainsKey(tvector) || nodes.ContainsKey(tvector))) {
                                    IntVector2[] tlist = new IntVector2[pair.Value.Length + 1];
                                    pair.Value.CopyTo(tlist, 0);
                                    tlist[pair.Value.Length] = tvector;
                                    nodes.Add(tvector, tlist);
                                }
                        }
                    }
                }

            }
            #endregion

            #region search2
            float bestDistance = float.MaxValue;
            IntVector2[] bestPath = new IntVector2[0];
            nodes.Add(new IntVector2(arX, arY), new IntVector2[] { new IntVector2(arX, arY) });
            while (nodes.Count != 0) {
                //Check for positions
                foreach (KeyValuePair<IntVector2, IntVector2[]> pair in nodes) {
                    if (goal.X - pair.Key.X < bestDistance) {
                        bestPath = new IntVector2[pair.Value.Length];
                        bestDistance = Math.Abs(goal.X - pair.Key.X);
                        pair.Value.CopyTo(bestPath, 0);
                    }
                }

                beforethat = new Dictionary<IntVector2, IntVector2[]>(lastnodes, ivec);
                lastnodes = new Dictionary<IntVector2, IntVector2[]>(nodes, ivec);
                nodes.Clear();
                foreach (KeyValuePair<IntVector2, IntVector2[]> pair in lastnodes) {
                    for (int i = 0; i < 4; i++) {
                        IntVector2 tvector = pair.Key + directions[i];
                        if (IsInRegion(tvector, lo, hi)) {
                            if (Game1.board[tvector.Y, tvector.X] == 0)
                                if (!(beforethat.ContainsKey(tvector) || nodes.ContainsKey(tvector))) {
                                    IntVector2[] tlist = new IntVector2[pair.Value.Length + 1];
                                    pair.Value.CopyTo(tlist, 0);
                                    tlist[pair.Value.Length] = tvector;
                                    nodes.Add(tvector, tlist);
                                }
                        }
                    }
                }

            }

            waypoints = new Vector2[bestPath.Length];
            for (int i = 0; i < bestPath.Length; i++)
                waypoints[i] = new Vector2(64 * bestPath[i].X, 64 * bestPath[i].Y);
            #endregion
        }

        public bool IsInRegion(IntVector2 pos, IntVector2 lo, IntVector2 hi) {
            return (lo.X <= pos.X) && (pos.X < hi.X) && (lo.Y <= pos.Y) && (pos.Y < hi.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>0 if in no bunker, 1 if in left, 2 if in right</returns>
        public int IsInBunker(int eps) {
            if (IsInRegion(screenPos, new Vector2(12 * 64 - eps, 5 * 64 - eps), new Vector2(12 * 64 + eps, 7 * 64))) return 2;
            if (IsInRegion(screenPos, new Vector2(64 - eps, 5 * 64 - eps), new Vector2(64 + eps, 7 * 64))) return 1;
            return 0;

        }

        public void Update(GameTime gameTime) {
            sOffset = Vector2.Zero;
            if (startTime < 0)
                startTime = gameTime.TotalGameTime.TotalSeconds;
            double s = (gameTime.TotalGameTime.TotalSeconds - startTime);
            double e = gameTime.ElapsedGameTime.TotalSeconds;

            //Virtual key presses
            lastKeyPressed = keyPressed;
            float ttime;
            int tposx, tposy;

            if (currentInstruction < instructions.Length) {
                string ci = instructions[currentInstruction];
                if (float.TryParse(ci.Substring(0, ci.IndexOf(' ')), out ttime)) {
                    if (s > ttime) {
                        if (ci.Length <= ci.IndexOf(' ') + 1) {
                            throw new Exception("Someone's been messing with your intro.dboy file! #4");
                        }
                        string[] data = ci.Substring(ci.IndexOf(' ') + 1).Split(',');
                        if (data.Length != 3) {
                            throw new Exception("Someone's been messing with your intro.dboy file! #5");
                        }
                        if (data[0].Length != 1) {
                            throw new Exception("Someone's been messing with your intro.dboy file! #6");
                        }
                        keyPressed = data[0][0];
                        if (!(int.TryParse(data[1], out tposx) && int.TryParse(data[2], out tposy))) {
                            throw new Exception("Someone's been messing with your intro.dboy file! #7");
                        }
                        screenPos = new Vector2(64 * (tposx + 2), 64 * (tposy + 2));
                        currentInstruction++;

                    }
                } else {
                    throw new Exception("Someone's been messing with your intro.dboy file! #3");
                }
            }

            if (keyPressed == 'x') {
                //die instantly (doughboys that do this are often for timing)
                Game1.currentDoughboy++;
                return;
            }

            bool jjp = (keyPressed == 'j' && lastKeyPressed != 'j');
            if (jjp && !IsInAir && springJumpPhase == -1) {
                Game1.hammieham.addCue("jump");
            }
            float jumpSpace = 2.0f;
            //Animation
            currentFrame = currentAnim + (int)(s * speed * 2 % 6);

            //Which direction should I travel in?

            if (keyPressed == 'n') {
                currentAnim = 0;
                movingDirection = Direction.Up;
            }

            if (keyPressed == 'w') {
                currentAnim = 6;
                movingDirection = Direction.Left;
            }

            if (keyPressed == 's') {
                currentAnim = 12;
                movingDirection = Direction.Down;
            }

            if (keyPressed == 'e') {
                currentAnim = 18;
                movingDirection = Direction.Right;
            }

            Vector2 feetOffset = new Vector2(32, 32);


            //obstacle checking
            //Look before you move!
            IntVector2 arrayPos = new IntVector2((int)(screenPos.X + feetOffset.X) / 64, (int)(screenPos.Y + feetOffset.Y) / 64);
            IntVector2 topLeft = new IntVector2((int)(round(screenPos.X, 64)) / 64, (int)(round(screenPos.Y, 64)) / 64);

            //in general, is blocked if facing non-1 wall || facing cow || facing disarmanim ||(facing #1 xor on wall) & is not propelled by spring
            int nextCell = 1; //default value
            int[] non1walls = new int[] { 2, 3 };
            switch (movingDirection) {
                case Direction.Up:
                    nextCell = Game1.board[topLeft.Y, arrayPos.X];
                    if (Contains(non1walls, nextCell) ||
                        ArraySquareContainsCow(topLeft.Y, arrayPos.X) ||
                        //(12<=Game1.GetAnimationType(arrayPos.X,topLeft.Y-1) && Game1.GetAnimationType(arrayPos.X,topLeft.Y-1)<=14)||
                        ((nextCell == 1 ^ IsOnWall) && (springJumpPhase == -1)))
                        movingDirection = Direction.None;
                    if (Game1.board[topLeft.Y, arrayPos.X] == 6 && springJumpPhase == -1 && !IsOnWall && !IsInAir) {
                        PrepareForSpring();
                    }
                    break;
                case Direction.Left:
                    nextCell = Game1.board[arrayPos.Y, topLeft.X];
                    if (Contains(non1walls, nextCell) ||
                        ArraySquareContainsCow(arrayPos.Y, topLeft.X) ||
                        //(12 <= Game1.GetAnimationType(topLeft.X, arrayPos.Y-1) && Game1.GetAnimationType(topLeft.X, arrayPos.Y-1) <= 14) ||
                        ((nextCell == 1 ^ IsOnWall) && (springJumpPhase == -1)))
                        movingDirection = Direction.None;
                    if (Game1.board[arrayPos.Y, topLeft.X] == 6 && springJumpPhase == -1 && !IsOnWall && !IsInAir) {
                        PrepareForSpring();
                    }
                    break;
                case Direction.Down:
                    nextCell = Game1.board[topLeft.Y + 1, arrayPos.X];
                    if (Contains(non1walls, nextCell) ||
                        ArraySquareContainsCow(topLeft.Y + 1, arrayPos.X) ||
                        //(12 <= Game1.GetAnimationType(arrayPos.X, topLeft.Y) && Game1.GetAnimationType(arrayPos.X, topLeft.Y) <= 14) ||
                        ((nextCell == 1 ^ IsOnWall) && (springJumpPhase == -1)))
                        movingDirection = Direction.None;
                    if (Game1.board[topLeft.Y + 1, arrayPos.X] == 6 && springJumpPhase == -1 && !IsOnWall && !IsInAir) {
                        PrepareForSpring();
                    }
                    break;
                case Direction.Right:
                    nextCell = Game1.board[arrayPos.Y, topLeft.X + 1];
                    if (Contains(non1walls, nextCell) ||
                        ArraySquareContainsCow(arrayPos.Y, topLeft.X + 1) ||
                        //(12 <= Game1.GetAnimationType(topLeft.X + 1, arrayPos.Y-1) && Game1.GetAnimationType(topLeft.X + 1, arrayPos.Y-1) <= 14) ||
                        ((nextCell == 1 ^ IsOnWall) && (springJumpPhase == -1)))
                        movingDirection = Direction.None;
                    if (springJumpPhase == -1 && Game1.board[arrayPos.Y, topLeft.X + 1] == 6 && !IsOnWall && !IsInAir) {
                        PrepareForSpring();
                    }
                    break;
            }

            #region Driller
            if (myType == DoughboyType.Driller && numToDrill > 0) {
                if (nextCell == 1 && movingDirection == Direction.None && !IsOnWall) {
                    //create particle
                    Direction tempDirection = (Direction)(currentAnim / 6);
                    Vector2 oset;
                    switch (tempDirection) {
                        case Direction.Up:
                            oset = new Vector2(32, 0);
                            break;
                        case Direction.Left:
                            oset = new Vector2(0, 16);
                            break;
                        case Direction.Down:
                            oset = new Vector2(32, 64);
                            break;
                        case Direction.Right:
                            oset = new Vector2(64, 16);
                            break;
                        default:
                            oset = Vector2.Zero;
                            break;

                    }
                    Game1.particles.Add(new Particle(
                        screenPos + oset,
                        16 * (new Vector2(-(float)Game1.rand.NextDouble(), 2 * (float)Game1.rand.NextDouble() - 1)),
                        Game1.rand.Next(0, 4) * 64));

                    if (wallTime > 1) {
                        movingDirection = tempDirection;
                        switch (movingDirection) {
                            case Direction.Up:
                                Game1.board[topLeft.Y, arrayPos.X] = 0;
                                break;
                            case Direction.Left:
                                Game1.board[arrayPos.Y, topLeft.X] = 0;
                                break;
                            case Direction.Down:
                                Game1.board[topLeft.Y + 1, arrayPos.X] = 0;
                                break;
                            case Direction.Right:
                                Game1.board[arrayPos.Y, topLeft.X + 1] = 0;
                                break;
                        }
                        numToDrill--;
                    }
                    wallTime += e;
                } else {
                    wallTime = 0;
                }
            }
            #endregion

            IntVector2 posnow = new IntVector2(trunc(screenPos + sOffset, 2));

            if (myType == DoughboyType.Bomber && (s > 5 || (jjp && !IsOnWall))) //Bombers can't blow up while on walls.
            {
                Game1.currentDoughboy++;
                //BOOOM!
                Game1.animations.Add(new Animation(6, posnow, false, 0));
                IntVector2 myPos = new IntVector2((int)trunc(arrayPos.X * 64, 64), (int)trunc(arrayPos.Y * 64, 64));
                CreateBigExplosion(myPos);
                Game1.hammieham.addCue("splosion2");

                Game1.animations.Add(new Animation(6, posnow, false, 0));
            } else if (myType == DoughboyType.Disarmer && s > 5/*10*/) {
                Game1.currentDoughboy++;
                Game1.animations.Add(new Animation(4, posnow, true, 0));
            } else if (s > 5 /*10*/) {
                Game1.currentDoughboy++;
                Game1.animations.Add(new Animation(5, posnow, true, 0));

            }

            int eps = 10;
            if (leftSideQ && IsInRegion(screenPos, new Vector2(12 * 64 - eps, 5 * 64 - eps), new Vector2(12 * 64 + eps, 7 * 64))) {
                Game1.currentDoughboy++;
                Game1.players[1].RegisterHit();
            }
            if (!leftSideQ && IsInRegion(screenPos, new Vector2(64 - eps, 5 * 64 - eps), new Vector2(64 + eps, 7 * 64))) {
                Game1.currentDoughboy++;
                Game1.players[0].RegisterHit();
            }

            bool IsDisarmed = (Game1.vars[arrayPos.Y, arrayPos.X] & 1) == 1;
            //Explosions and interactions
            if (!IsInAir && !IsOnWall || Game1.board[arrayPos.Y, arrayPos.X] == 7 || Game1.board[arrayPos.Y, arrayPos.X] == 8) {


                //Disarm things
                int[] disarmable = { 5, 6, 7, 8, 10, 11, 12 };
                int[] disarmanims = { 13, 14, 18 };
                if (myType == DoughboyType.Disarmer && Contains(disarmable, Game1.board[arrayPos.Y, arrayPos.X])) {

                    //stop any animations currently happening there
                    for (int i = Game1.animations.Count - 1; i >= 0; i--) {
                        if (Game1.animations[i].pos.X == arrayPos.X * 64 && Game1.animations[i].pos.Y == arrayPos.Y * 64 && !Contains(disarmanims, Game1.animations[i].animationID)) {
                            Game1.animations.RemoveAt(i);
                        }
                        if (((Game1.animations[i].animationID == 10 || Game1.animations[i].animationID == 11) &&
                            (Game1.animations[i].pos.X == arrayPos.X * 64 && Game1.animations[i].pos.Y == arrayPos.Y * 64 - 64))) {

                            Game1.animations.RemoveAt(i);
                        }

                    }
                    if (!IsDisarmed) {
                        //add new animation
                        if (Game1.board[arrayPos.Y, arrayPos.X] == 7) {
                            Game1.animations.Add(new Animation(13, 64 * (arrayPos - IntVector2.UnitY), false, 2));

                        } else if (Game1.board[arrayPos.Y, arrayPos.X] == 8) {

                            Game1.animations.Add(new Animation(14, 64 * (arrayPos - IntVector2.UnitY), false, 2));
                        } else if (Game1.board[arrayPos.Y, arrayPos.X] == 5) {
                            Game1.animations.Add(new Animation(18, 64 * arrayPos, false, -1));
                        }
                    }
                    Game1.vars[arrayPos.Y, arrayPos.X] |= 1;
                    IsDisarmed = true;
                }


                if (Game1.board[arrayPos.Y, arrayPos.X] == 5 && !IsDisarmed) //Mine
                {
                    Game1.currentDoughboy++;
                    Game1.hammieham.addCue("splosion2");
                    //BOOOOOM!

                    Game1.animations.Add(new Animation(6, posnow, false, 0));

                    IntVector2 minePos = new IntVector2((int)trunc(arrayPos.X * 64, 64), (int)trunc(arrayPos.Y * 64, 64));
                    CreateExplosion(minePos);

                    Game1.board[arrayPos.Y, arrayPos.X] = 0;
                }

                if (Game1.board[arrayPos.Y, arrayPos.X] == 9) //Pit
                {
                    Game1.currentDoughboy++;
                    /*if (myType == DoughboyType.Disarmer)
                        Game1.animations.Add(new Animation(4, trapPos, true));
                    else
                        Game1.animations.Add(new Animation(5, trapPos, true));*/
                    switch (myType) {
                        case DoughboyType.Regular:
                            Game1.board[arrayPos.Y, arrayPos.X] = 10;
                            Game1.animations.Add(new Animation(24, 64 * arrayPos, false, 0));
                            Game1.animations.Add(new Animation(19, 64 * arrayPos, false, 1));
                            break;
                        case DoughboyType.Driller:
                            Game1.board[arrayPos.Y, arrayPos.X] = 10;
                            Game1.animations.Add(new Animation(24, 64 * arrayPos, false, 0));
                            Game1.animations.Add(new Animation(numToDrill == 0 ? 23 : 22, 64 * arrayPos, false, 1));
                            break;
                        case DoughboyType.Disarmer:
                            Game1.board[arrayPos.Y, arrayPos.X] = 11;
                            Game1.animations.Add(new Animation(25, 64 * arrayPos, false, 0));
                            Game1.animations.Add(new Animation(20, 64 * arrayPos, false, 1));
                            break;
                        case DoughboyType.Bomber:
                            Game1.board[arrayPos.Y, arrayPos.X] = 12;
                            Game1.animations.Add(new Animation(26, 64 * arrayPos, false, 0));
                            //Game1.animations.Add(new Animation(21, 64 * arrayPos, false, 1));
                            break;
                    }
                }


                if (Game1.board[arrayPos.Y, arrayPos.X] == 6 && !IsDisarmed) //Spring
                {
                    //jumpStartTime = 0;
                    IntVector2 trapPos = 64 * arrayPos;
                    if (!Game1.AnimationTypeAtQ(trapPos.X / 64, trapPos.Y / 64, 9)) {
                        Game1.animations.Add(new Animation(9, trapPos, false, -1));
                    }
                }
            }

            #region guillotine
            bool isFacingGuillotine = false;
            if (!IsDisarmed && myType != DoughboyType.Disarmer) {
                switch (currentAnim / 6) //infer direction
                {
                    case 0:
                        if (!Game1.AnimationTypeAtQ(arrayPos.X, topLeft.Y - 1, 10)) {
                            if (Game1.board[topLeft.Y, arrayPos.X] == 7) {
                                isFacingGuillotine = true;
                                Game1.animations.Add(new Animation(10, 64 * new IntVector2(arrayPos.X, topLeft.Y - 1), false, 2));
                                Game1.animations.Add(new Animation(11, 64 * new IntVector2(arrayPos.X, topLeft.Y - 1), false, 2));
                                Game1.board[topLeft.Y, arrayPos.X] = 8;
                            }
                        }
                        break;
                    case 1:
                        if (!Game1.AnimationTypeAtQ(topLeft.X, arrayPos.Y - 1, 10)) {
                            if (Game1.board[arrayPos.Y, topLeft.X] == 7) {
                                isFacingGuillotine = true;
                                Game1.animations.Add(new Animation(10, 64 * new IntVector2(topLeft.X, arrayPos.Y - 1), false, 2));
                                Game1.animations.Add(new Animation(11, 64 * new IntVector2(topLeft.X, arrayPos.Y - 1), false, 2));
                                Game1.board[arrayPos.Y, topLeft.X] = 8;
                            }
                        }
                        break;
                    case 2:
                        if (!Game1.AnimationTypeAtQ(arrayPos.X, topLeft.Y + 1 - 1, 10)) {
                            if (Game1.board[topLeft.Y + 1, arrayPos.X] == 7) {
                                isFacingGuillotine = true;
                                Game1.animations.Add(new Animation(10, 64 * new IntVector2(arrayPos.X, topLeft.Y + 1 - 1), false, 2));
                                Game1.animations.Add(new Animation(11, 64 * new IntVector2(arrayPos.X, topLeft.Y + 1 - 1), false, 2));
                                Game1.board[topLeft.Y + 1, arrayPos.X] = 8;
                            }
                        }
                        break;
                    case 3:
                        if (!Game1.AnimationTypeAtQ(topLeft.X + 1, arrayPos.Y - 1, 10)) {
                            if (Game1.board[arrayPos.Y, topLeft.X + 1] == 7) {
                                isFacingGuillotine = true;
                                Game1.animations.Add(new Animation(10, 64 * new IntVector2(topLeft.X + 1, arrayPos.Y - 1), false, 2));
                                Game1.animations.Add(new Animation(11, 64 * new IntVector2(topLeft.X + 1, arrayPos.Y - 1), false, 2));
                                Game1.board[arrayPos.Y, topLeft.X + 1] = 8;

                            }
                        }
                        break;
                }
            }

            //Could make animation a bit smoother

            if ((Game1.board[arrayPos.Y, arrayPos.X] == 7 || Game1.AnimationTypeAtQ(arrayPos.X, arrayPos.Y - 1, 10)) && !IsDisarmed && myType != DoughboyType.Disarmer) {
                if (myType == DoughboyType.Bomber) {
                    CreateExplosion(64 * arrayPos);
                    Game1.hammieham.addCue("splosion2");
                }
                Game1.currentDoughboy++;
                return;
            }
            if (Game1.board[arrayPos.Y, arrayPos.X] == 8 && !IsInAir && !IsDisarmed && myType != DoughboyType.Disarmer) {
                if (myType == DoughboyType.Bomber) {
                    CreateExplosion(64 * arrayPos);
                    Game1.hammieham.addCue("splosion2");
                }
                Game1.currentDoughboy++; //fall down go bump
                return;
            }
            #endregion


            float d = 0.1f;//for damping

            Vector2 gridLocation = trunc(screenPos + feetOffset, 64);

            switch (movingDirection) {
                case Direction.Up:
                    screenPos.Y -= (float)(e * 64 * speed);
                    screenPos.X = (1 - d) * screenPos.X + d * gridLocation.X;
                    break;
                case Direction.Left:
                    screenPos.X -= (float)(e * 64 * speed);
                    screenPos.Y = (1 - d) * screenPos.Y + d * gridLocation.Y;
                    break;
                case Direction.Down:
                    screenPos.Y += (float)(e * 64 * speed);
                    screenPos.X = (1 - d) * screenPos.X + d * gridLocation.X;
                    break;
                case Direction.Right:
                    screenPos.X += (float)(e * 64 * speed);
                    screenPos.Y = (1 - d) * screenPos.Y + d * gridLocation.Y;
                    break;
            }


            /*SPRING JUMP PHASES
             * -1: Not next to or on a spring. This handles normal jumping.
             * 0: Jumping on a spring. This happens when a doughboy is not on a wall but is running towards spring and in the cell adjacent to it.
             * 1: Jumping off the spring.
             * 2: Special, for jumping while on a wall. In this case, special care has to be taken to not cause glitches.
             */
            jumpTime += e;
            if (springJumpPhase == -1) {
                speed = 5.0f;
                if (jumpTime > jumpSpace / speed) {
                    IsInAir = false;
                    if (jjp) //this used to be such that you could bounce around the screen like crazy. It was cute, but it would have caused a lot of bugs.
                    {
                        if (isFacingGuillotine) {
                            IsOnWall = false;
                            if (movingDirection == Direction.None)
                                movingDirection = (Direction)(currentAnim / 6);
                        }
                        if (IsOnWall) {
                            //Look two squares ahead to see if it is a valid jump.
                            IntVector2 qp = GetCellPos(1);
                            IntVector2 qqp = GetCellPos(2);
                            if (/*GetCell(2) != 1 &&*/ !ArraySquareContainsCow(qp.Y, qp.X) && !ArraySquareContainsCow(qqp.Y, qqp.X)) {
                                jumpTime = 0;
                                //if (GetCell(2) == 1) {
                                    springJumpPhase = 2;
                                //}
                                if (movingDirection == Direction.None)
                                    movingDirection = (Direction)(currentAnim / 6); //Set direction so that the doughboy moves
                            }
                        } else {
                            jumpTime = 0;
                        }
                    } else if (Game1.board[arrayPos.Y, arrayPos.X] == 6) {
                        springJumpPhase = 1;
                        jumpTime = 0;
                        if (!Game1.AnimationTypeAtQ(arrayPos.X, arrayPos.Y, 9)) {
                            Game1.animations.Add(new Animation(9, 64 * arrayPos, false, -1));
                        }
                    }
                } else {
                    IsInAir = true;
                    sOffset.Y = (float)(4 * 4 * 64 * (jumpTime) * (jumpTime - jumpSpace / speed));
                }

            }

            if (springJumpPhase == 0) {
                IsInAir = false; //it's just a hop

                speed = 2.5f;
                //apex is 64 px,
                //one root is at 0,
                //at 1/speed the doughboy is 32 px 
                float k = 186.51f * speed * speed;
                float x = 1.17157f / speed;
                sOffset.Y = (float)(k * jumpTime * (jumpTime - x));
                if ((jumpTime > 1 / speed)) {
                    if (Game1.board[arrayPos.Y, arrayPos.X] == 6) { //if I'm <i>actually on the spring</i>

                        Game1.hammieham.addCue("spring");
                        springJumpPhase = 1;
                        jumpTime = 0;
                    } else {
                        springJumpPhase = -1;
                        jumpTime = 60;
                    }
                }
            }
            if (springJumpPhase == 1) {
                IsInAir = true;
                IsOnWall = true;
                speed = 2.5f;
                //apex is 128 px,
                //one root is at 2/speed,
                //at 0 the doughboy is 32 px 
                float a = -111.426f * speed * speed;
                float b = 206.851f * speed;
                float c = 32;
                sOffset.Y = -(float)(a * jumpTime * jumpTime + b * jumpTime + c);
                if (jumpTime > 2 / speed) {
                    if (Game1.board[arrayPos.Y, arrayPos.X] == 6) {
                        //if, by some fluke of chance, we have landed on another spring, then:
                        jumpTime = 0;//do it again!
                        Game1.animations.Add(new Animation(9, new IntVector2(arrayPos.X * 64, arrayPos.Y * 64), false, -1));
                    } else {
                        springJumpPhase = -1;
                        IsInAir = false;
                        IsOnWall = Contains(Game1.wallIDs, Game1.board[arrayPos.Y, arrayPos.X]);
                    }
                }
            }
            if (springJumpPhase == 2) {
                IsInAir = true;
                IsOnWall = false; //it's jumping <i>off</i> the wall! Unless it's jumping onto another.
                sOffset.Y = (float)(4 * 4 * 64 * (jumpTime) * (jumpTime - jumpSpace / speed));
                if (jumpTime > 2 / speed) {
                    if (Game1.board[arrayPos.Y, arrayPos.X] == 6) {
                        //o hai thar
                        jumpTime = 0;
                        Game1.animations.Add(new Animation(9, new IntVector2(arrayPos.X * 64, arrayPos.Y * 64), false, -1));
                    } else {
                        springJumpPhase = -1;
                        IsInAir = false;
                        IsOnWall = (Game1.board[arrayPos.Y, arrayPos.X] == 1);
                    }
                }
            }






        }

        public void PrepareForSpring() {
            jumpTime = 0;
            springJumpPhase = 0;
            Game1.hammieham.addCue("jump");
        }

        public int GetCell(int distance) {
            Direction tempDir = (Direction)(currentAnim / 6);

            Vector2 feetOffset = new Vector2(32, 32);
            IntVector2 arrayPos = new IntVector2((int)(screenPos.X + feetOffset.X) / 64, (int)(screenPos.Y + feetOffset.Y) / 64);
            IntVector2 topLeft = new IntVector2((int)(round(screenPos.X, 64)) / 64, (int)(round(screenPos.Y, 64)) / 64);

            switch (tempDir) {
                case Direction.Up:
                    return Game1.board[topLeft.Y + (1 - distance), arrayPos.X];
                case Direction.Left:
                    return Game1.board[arrayPos.Y, topLeft.X + (1 - distance)];
                case Direction.Down:
                    return Game1.board[topLeft.Y + distance, arrayPos.X];
                case Direction.Right:
                    return Game1.board[arrayPos.Y, topLeft.X + distance];
            }
            Console.WriteLine("Okay, what just happened here? This shouldn't happen.");
            throw new Exception("PROGRAMMERISSTUPIDEXCEPTION #1");
            //return -1;
        }

        public IntVector2 GetCellPos(int distance) {
            Direction tempDir = (Direction)(currentAnim / 6);

            Vector2 feetOffset = new Vector2(32, 32);
            IntVector2 arrayPos = new IntVector2((int)(screenPos.X + feetOffset.X) / 64, (int)(screenPos.Y + feetOffset.Y) / 64);
            IntVector2 topLeft = new IntVector2((int)(round(screenPos.X, 64)) / 64, (int)(round(screenPos.Y, 64)) / 64);

            switch (tempDir) {
                case Direction.Up:
                    return new IntVector2(arrayPos.X, topLeft.Y + (1 - distance));
                case Direction.Left:
                    return new IntVector2(topLeft.X + (1 - distance), arrayPos.Y);
                case Direction.Down:
                    return new IntVector2(arrayPos.X, topLeft.Y + distance);
                case Direction.Right:
                    return new IntVector2(topLeft.X + distance, arrayPos.Y);
            }
            Console.WriteLine("Okay, what just happened here? This shouldn't happen.");
            throw new Exception("PROGRAMMERISSTUPIDEXCEPTION #1");
            //return -1;
        }

        public void CreateExplosion(IntVector2 minePos) {
            Game1.animations.Add(new Animation(6, minePos, false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(64, 0), false, 1));
            Game1.animations.Add(new Animation(6, minePos - new IntVector2(64, 0), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(0, 64), false, 1));
            Game1.animations.Add(new Animation(6, minePos - new IntVector2(0, 64), false, 1));
        }

        public void CreateBigExplosion(IntVector2 minePos) {
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(-64, -64), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(-64, 0), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(-64, 64), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(0, -64), false, 1));
            Game1.animations.Add(new Animation(6, minePos, false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(0, 64), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(64, -64), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(64, 0), false, 1));
            Game1.animations.Add(new Animation(6, minePos + new IntVector2(64, 64), false, 1));
        }

        /// <summary>
        /// ASSUMES x and y are reversed- i.e., it's in array form.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool ArraySquareContainsCow(int x, int y) {
            if (Game1.currentCow == null) return false;
            return Game1.currentCow.arrayPos.X == y && Game1.currentCow.arrayPos.Y == x;
        }



        public void Draw() {

            if (IsInAir) {
                int dir = (int)movingDirection;
                if (movingDirection == Direction.None) dir = currentAnim / 6;
                Game1.spriteBatch.Draw(Game1.doughboyjumps[numToDrill > 0 ? (int)myType : 4][dir], trunc(screenPos + sOffset, 2), Color.White);
            } else {
                Game1.spriteBatch.Draw(Game1.doughboyframes[numToDrill > 0 ? (int)myType : 4][currentFrame], trunc(screenPos + sOffset, 2), Color.White);
            }
        }
    }

}
