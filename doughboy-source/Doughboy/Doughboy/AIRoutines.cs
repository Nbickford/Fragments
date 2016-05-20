using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Doughboy {
    public class AIRoutines {
        //Questions you might ask yourself:
        /* Can I reach this place in 5 seconds?
         * What is the minimum cost of reaching this place, that is, how much must I expend to reach this place?
         * How do I reach this place?
         * How do I reach this place with the minimum cost?
         * How can I attack my opponent without letting him attack me?
         * Which places can I reach in 5 seconds?
         * Which places can I reach in one square?
         * What are the costliest places to get to?
         */
        //The AIRoutines class has methods which answer each of these questions in a reasonable amount of time.
        //TODO: Completely revamp this section. I clearly have no idea what I'm doing.
        /*
         * Method #1: reaching places in the minimum amount of time.
         * This is sort of BFS - actually, it's structured A*.
         * We treat each cell on the board as a node, with a certain time
         * and an AIFrame for the board state and player position.
         * Until we run out of active nodes, we expand the one with the least
         * time spent - that is, we compute uncomputed children for each direction
         * and add those to the queue of nodes. Then, we store each node's min time 
         * in each cell, giving us the minimum time to reach and the path to get to
         * each location on the board.
         * This should be /really/ fast, since the only lists we need are lists of nodes
         * and there isn't really any construction or unboxing involved.
         * That said, this method can't handle cases where squares can be revisited 
         * (e.g. press a button and a square is suddenly accessible). We don't have
         * any items which can do things like that, though.
         * It also doesn't handle things like drilling through walls optimally.
         * 
         * We can also keep track of other variables, like money earned.
         */

        private static AIFrameComparer aic = new AIFrameComparer();
        private static DuplicateKeyComparer<float> dfc = new DuplicateKeyComparer<float>();

        //list <-> time, position
        private static SortedList<float, IntVector2> nodes;
        private static AIFrame[,] routes;

        public static AIFrame[,] ComputeRoutes(IntVector2 pos, DoughboyType doughboyType, int whoseTurn, bool canJump)
        {
            routes = new AIFrame[Game1.boardHeight, Game1.boardWidth];
            routes[pos.Y, pos.X] = new AIFrame(Game1.board, Game1.vars, 3, 0, pos, 0);
            nodes = new SortedList<float, IntVector2>(dfc);
            nodes.Add(0, pos);

            while (nodes.Count > 0)
            {
                KeyValuePair<float, IntVector2> timepos=nodes.First();
                float time = timepos.Key;
                IntVector2 dpos = timepos.Value.Duplicate();
                nodes.RemoveAt(0);
                FindNewPositions(routes[dpos.Y,dpos.X],timepos.Key,whoseTurn,doughboyType, canJump);
                
            }
            
            return routes;

            //replace cell if null or less time
        }

        /// <summary>
        /// Finds positions 1 step away from the current one where all new states take less than maxTime time in total.
        /// </summary>
        /// <param name="aiframe"></param>
        /// <param name="maxTime"></param>
        /// <param name="whoseTurn">0 if Left, 1 if Right, 15 if Game (universal knowledge)</param>
        private static void FindNewPositions(AIFrame aiframe, float currentTime, int whoseTurn, DoughboyType doughboyType, bool canJump) {
            //this is the function which adds those possible frames one step away from the aiframe given.

            //At each step:

            //If we're on a trap:
            //Unless this is a Disarmer and the trap is not an empty pit OR the trap is disarmed
            //we immediately die. Disable the trap in the |current| cell.
            
            //RUNNING:
            //If we're on a normal ground square or filled pit, we can move up, down, left, or right, provided it's not into
            //a wall (unless we're a Driller and we have more than 0 walls to go)
            //If we're on a wall, though, we can't run off the edge.
            //Finally, if we're on a spring, we immediately jump. The situation is identical to if we had jumped off a wall.
            //and for all of those, provided it's not into a forbidden space.
            
            //JUMPING:
            //If we're on a spring or a wall, we can jump onto anything. Otherwise, we can only jump onto non-wall squares.
            //If we jump into or above a live guillotine, we immediately die.

            //A FEW TRICKS (which give the player an edge over the AI):
            //SPRING PUNTING: Once you've jumped off a square, move right, then quickly left to move only one square.
            
            //Disarmed things are treated like they aren't there.

            //First, test if we're on a live trap.
            int[] traps = new int[] { 5, 7, 8, 9 };
            int currentBoard = aiframe.GetCurrentBoard();
            int currentVar=aiframe.GetCurrentVar();
            int currentItemVar=currentVar>>8;
            int currentOwner=(currentVar&0xf0)>>4;
            int currentDisarmed=currentVar&0x0f;
            if (BaseRoutines.Contains(currentBoard, traps) && currentDisarmed==0) {
                //If I can't see the mine, I don't think I'm going to die...
                if(currentBoard!=5 || (currentOwner==whoseTurn || currentOwner==15)){
                    //die, unless a disarmer!
                    if(doughboyType==DoughboyType.Disarmer && currentBoard!=9){
                        aiframe.vars[aiframe.pos.Y,aiframe.pos.X]|=0x1; //it is now disarmed! We keep on going.
                    }else{
                        return;
                    }
                }
            }

            float timePerSquare = (1/Doughboy.MaxSpeed);
            float timePerJumpSquare=timePerSquare; //unless we're on a spring, in which case this is doubled.
            bool isUpTop = (currentBoard == 1); //if we're on a spring this gets set to True as well.
            int[,] newlocs = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } }; //UDLR, using Konami ordering.

            //Moving: Split into 3 cases.
            if (currentBoard == 0 || currentBoard == 4) { //if we're on the ground
                
                for (int i = 0; i < 4; i++) {
                    int proposedBoard = aiframe.GetRelativeBoard(newlocs[i, 0], newlocs[i, 1]);
                    if(aiframe.CanExistInRelativeSquare(newlocs[i,0],newlocs[i,1],doughboyType,canJump)){ //this is "extensible"
                        IntVector2 newpos=new IntVector2(aiframe.pos.X+newlocs[i,0],aiframe.pos.Y+newlocs[i,1]);
                        if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + timePerSquare)) {
                            AIFrame newframe = aiframe.Duplicate();
                            newframe.MoveTo(newpos, timePerSquare);
                            if (proposedBoard == 1) newframe.wallsRemaining--;
                            SetAIFrame(newframe);
                        }
                    }
                }
            } else if (currentBoard == 1) { //if we're on a wall!
                for (int i = 0; i < 4; i++) {
                    int proposedBoard = aiframe.GetRelativeBoard(newlocs[i, 0], newlocs[i, 1]);
                    if (proposedBoard == 1) { //if we'll still be on the wall
                        IntVector2 newpos = new IntVector2(aiframe.pos.X + newlocs[i, 0], aiframe.pos.Y + newlocs[i, 1]);
                        if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + timePerSquare)) {
                            AIFrame newframe = aiframe.Duplicate();
                            newframe.MoveTo(newpos, timePerSquare);
                            SetAIFrame(newframe);
                        }

                    }
                }
            } else if (currentBoard == 6) { //springboard!
                timePerJumpSquare*=2;
                isUpTop = true; //we can't move anywhere except by jumping
            }

            //Jumping! This is kind of tricky, so I'm going to do it tomorrow.
            //Essentially, we can move in a radius-2 Manhattan square,
            //but some squares are only accessible if others are open.
            //"Open", in this case, means we can get to them without going through a wall(unless upTop) or frigging guillotine.
            //Here's a diagram:
            //  0    0=A
            // 1A2   1=A|B 2=A|C
            //3B C4  3=B   4=C
            // 5D6   5=B|D 6=C|D
            //  7    7=D
            //Furthermore, if any of A,B,C,D is a frigging live guillotine, we have to add another state for stopping there
            //(in case of kamikazi doughboy)

            if (canJump) {
                int[] adjboard = new int[4];
                for (int i = 0; i < 4; i++) {
                    if (aiframe.IsCowAtRelativeSquare(newlocs[i, 0], newlocs[i, 1])) {
                        adjboard[i] = 0;
                    } else {
                        adjboard[i] = aiframe.GetRelativeBoard(newlocs[i, 0], newlocs[i, 1]);
                        if (adjboard[i] == 7) { //if it's an unsprung guillotine
                            if (((aiframe.GetRelativeVar(newlocs[i, 0], newlocs[i, 1] & 1) == 1))) { //if it's live as well!
                                //if you /wanted/ to go there...
                                IntVector2 newpos = new IntVector2(aiframe.pos.X + newlocs[i, 0], aiframe.pos.Y + newlocs[i, 1]);
                                if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + timePerJumpSquare)) {
                                    AIFrame newframe = aiframe.Duplicate();
                                    newframe.MoveTo(newpos, timePerJumpSquare);
                                    SetAIFrame(newframe);
                                }
                                adjboard[i] = 3; //no going over this.
                            }
                        }
                        //change adjboard to can-I-jump-over-this?
                        adjboard[i] = ((adjboard[i] == 1 && !isUpTop) || (adjboard[i] == 2) || (adjboard[i] == 3)) ? 0 : 1;
                        
                        //and while we're at it, tackle cases 0, 3, 4, and 7 above.
                        if (adjboard[i] == 1) {
                            int proposedBoard = aiframe.GetRelativeBoard(2 * newlocs[i, 0], 2 * newlocs[i, 1]);
                            if (!((proposedBoard == 1 && !isUpTop) || (proposedBoard == 2) || (proposedBoard == 3) ||
                                  aiframe.IsCowAtRelativeSquare(2*newlocs[i,0],2*newlocs[i,1]) )) {
                                IntVector2 newpos = new IntVector2(aiframe.pos.X + 2 * newlocs[i, 0], aiframe.pos.Y + 2 * newlocs[i, 1]);
                                if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + 2 * timePerJumpSquare)) {
                                    AIFrame newframe = aiframe.Duplicate();
                                    newframe.MoveTo(newpos, 2 * timePerJumpSquare);
                                    SetAIFrame(newframe);
                                }
                            }
                        }
                    }
                }

                //Handle corner cases.
                for (int i = 0; i < 4; i++) {
                    //We go clockwise around the center, starting from 2: 2,6,5,1.
                    //2 is U or R, 6 is R or D, etc.
                    bool ud = (i == 0 || i == 3); //true=up
                    bool lr = (i > 1); //true=left

                    //first of all, can we even get there?
                    //Remember, we don't care if it's a live trap or not (since that's handled at the start)
                    //we just care if a doughboy /can/ be there.
                    int bval = aiframe.GetRelativeBoard(lr ? -1 : 1, ud ? -1 : 1);
                    if (!((bval == 1 && !isUpTop) || bval == 2 || bval == 3 || 
                        aiframe.IsCowAtRelativeSquare(lr?-1:1, ud?-1:1) )) {
                        //alright! now how do we get there? or more simply, which path works?
                        if (adjboard[lr ? 2 : 3] == 1) {
                            IntVector2 newpos = new IntVector2(aiframe.pos.X + (lr ? -1 : 1), aiframe.pos.Y + (ud ? -1 : 1));
                            if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + 2 * timePerJumpSquare)) {
                                AIFrame newframe = aiframe.Duplicate();
                                newframe.MoveTo(new IntVector2(aiframe.pos.X + (lr ? -1 : 1), aiframe.pos.Y), timePerJumpSquare);
                                newframe.MoveTo(new IntVector2(aiframe.pos.X + (lr ? -1 : 1), aiframe.pos.Y + (ud ? -1 : 1)), timePerJumpSquare);
                                SetAIFrame(newframe);
                            }
                        } else if (adjboard[ud ? 0 : 1] == 1) {
                            IntVector2 newpos = new IntVector2(aiframe.pos.X + (lr ? -1 : 1), aiframe.pos.Y + (ud ? -1 : 1));
                            if (OKToCopy(newpos.X, newpos.Y, aiframe.totalSeconds + 2 * timePerJumpSquare)) {
                                AIFrame newframe = aiframe.Duplicate();
                                newframe.MoveTo(new IntVector2(aiframe.pos.X, aiframe.pos.Y + (ud ? -1 : 1)), timePerJumpSquare);
                                newframe.MoveTo(new IntVector2(aiframe.pos.X + (lr ? -1 : 1), aiframe.pos.Y + (ud ? -1 : 1)), timePerJumpSquare);
                                SetAIFrame(newframe);
                            }

                        }
                    }
                }


            }


            
        }

        private static bool OKToCopy(int xpos, int ypos, float time) {
            if (routes[ypos, xpos] == null) return true;
            if (routes[ypos, xpos].totalSeconds > time) {
                Console.WriteLine("WOAH!");
            }
            return (routes[ypos, xpos].totalSeconds > time);
        }

        private static void SetAIFrame(AIFrame aiframe) {
            routes[aiframe.pos.Y,aiframe.pos.X] = aiframe;
            nodes.Add(aiframe.totalSeconds, aiframe.pos);

            if (aiframe.pos.Y == 2 && aiframe.pos.X == 9) {
                Console.WriteLine("debugmode");
            }
        }
    }

    public class AIFrame {
        public int[,] board;
        public int[,] vars;
        public int wallsRemaining;
        public int moneyEarned;
        public IntVector2 pos;
        public float totalSeconds;
        public IntVector2[] prevPositions;
        

        public AIFrame(int[,] Board, int[,] Vars, int WallsRemaining, int MoneyEarned, IntVector2 DoughboyPosition, float TotalTime) {
            board=BaseRoutines.Copy2DArray(Board);
            vars = BaseRoutines.Copy2DArray(Vars);
            wallsRemaining = WallsRemaining;
            moneyEarned = MoneyEarned;
            pos = DoughboyPosition.Duplicate();
            totalSeconds = TotalTime;
            prevPositions = new IntVector2[0];
        }

        public AIFrame Duplicate() {
            AIFrame newframe=new AIFrame(board, vars, wallsRemaining, moneyEarned, pos, totalSeconds);
            IntVector2[] newPrevPositions = new IntVector2[prevPositions.Length];
            prevPositions.CopyTo(newPrevPositions,0);
            newframe.prevPositions=newPrevPositions;
            return newframe;
        }

        public void SetValue(AIFrame aif, int newseconds, IntVector2 newpos) {
            board = BaseRoutines.Copy2DArray(aif.board);
            vars = BaseRoutines.Copy2DArray(aif.vars);
            wallsRemaining = aif.wallsRemaining;
            moneyEarned = aif.moneyEarned;
            pos.X = aif.pos.X;
            pos.Y = aif.pos.Y;
            totalSeconds = newseconds;
            prevPositions = new IntVector2[aif.prevPositions.Length + 1];
            for (int i = 0; i < aif.prevPositions.Length; i++) {
                prevPositions[i] = aif.prevPositions[i];
            }
            prevPositions[aif.prevPositions.Length] = newpos;
        }

        public void MoveTo(IntVector2 newpos, float elapsedTime) {
            IntVector2[] newPrevPositions = new IntVector2[prevPositions.Length+1];
            for (int i = 0; i < prevPositions.Length; i++) {
                newPrevPositions[i] = prevPositions[i]; //yes, this is extremely wasteful. 
                //TODO: Benchmark test this vs List or LinkedList.
            }
            newPrevPositions[prevPositions.Length] = pos.Duplicate();
            prevPositions = newPrevPositions;

            pos = newpos.Duplicate();
            totalSeconds += elapsedTime;
        }

        public bool CanExistInRelativeSquare(int dx, int dy, DoughboyType doughboyType, bool canJump) {
            int proposedBoard = GetRelativeBoard(dx, dy);
            if (IsCowAtRelativeSquare(dx, dy)) return false;
            return !((proposedBoard == 1 && !(doughboyType==DoughboyType.Driller && wallsRemaining>0)) 
                        || proposedBoard == 2 || proposedBoard == 3 || (proposedBoard==6 && !canJump) );
        }

        public bool IsCowAtRelativeSquare(int dx, int dy) {
            if (Game1.currentCow != null) {
                if (Game1.currentCow.arrayPos.X == pos.X + dx && Game1.currentCow.arrayPos.Y == pos.Y + dy) {
                    return true; //COW!
                }
            }
            return false;
        }

        public int GetRelativeBoard(int dx, int dy) {
            if ((pos.X + dx >= Game1.boardWidth) || (pos.X + dx < 0) ||
                (pos.Y + dy >= Game1.boardHeight) || (pos.Y + dy < 0)) {
                return 2;
            } else {
                return board[pos.Y + dy, pos.X + dx];
            }
        }

        public int GetRelativeVar(int dx, int dy) {
            if ((pos.X + dx >= Game1.boardWidth) || (pos.X + dx < 0) ||
                (pos.Y + dy >= Game1.boardHeight) || (pos.Y + dy < 0))
            {
                return 0xf0;
            } else {
                return vars[pos.Y + dy, pos.X + dx];
            }
        }

        public int GetCurrentBoard() {
            return board[pos.Y, pos.X];
        }

        public int GetCurrentVar() {
            return vars[pos.Y, pos.X];
        }

       
    }

    /// <summary>
    /// Comparer for comparing two keys, handling equality as beeing greater
    /// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class DuplicateKeyComparer<TKey>
                    :
                 IComparer<TKey> where TKey : IComparable {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y) {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }

        #endregion
    }

    public class AIFrameComparer : IEqualityComparer<AIFrame> {
        public bool Equals(AIFrame a, AIFrame b) {
            if (a.pos != b.pos) return false;
            if (a.wallsRemaining != b.wallsRemaining) return false;
            if (!BaseRoutines.ArrayEqual(a.board, b.board)) return false;
            return true;
        }

        public int GetHashCode(AIFrame a) {
            //These hash codes are formatted!
            //We don't really care about totalMS, but wallsRemaining will be between 0 and 3 (2 bits),
            //the x and y coordinates of doughboyPos take 4 bits each, leaving 32-10=22 bits for the board.
            //This is enough to encode the positions of three changes, but we'll just use j.random has for now.
            int sum = 0;
            sum = (sum << 2) + a.wallsRemaining;
            sum = (sum << 8) + a.pos.Y * 16 + a.pos.X;
            int arhash = 0;
            for(int i=0;i<a.board.GetLength(0);i++){
                for (int j = 0; j < a.board.GetLength(1); j++) {
                    arhash = (arhash << 9) | (arhash >> (22-9)); 
                    //where does 9 come from? It's made up, but is coprime with 22.
                    arhash ^= a.board[i, j];
                }
            }
            sum = (sum << 22) + (arhash & 0x3fffff)+a.moneyEarned; //new 2014-11-15: moneyEarned
            return sum;
        }
    }
}
