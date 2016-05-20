using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Doughboy {
    public class SmartMine {
        //A SmartMine is a sort of robotic mine.
        /* Once activated, the smartmine will pursue a Doughboy by one of two methods:
         * (1) Under the current implementation, the SmartMine simply attempts to get as close to 
         *     where the Doughboy will be as possible. 
         * (2) A more advanced technique would be to have the SmartMine search for the quickest path
         *     to get to the Doughboy by searching through all positions it knows of.
         *     
         * These SmartMines are ruthless (the in-game explanation is that they've been developed by
         * Justin Hammer) - although they don't know the positions of the other player's bombs, they
         * will run into one (or a spring) if it will stop the Doughboy. SmartMines, of course, 
         * will explode once they're close enough to the Doughboy. If the doughboy escapes or another
         * SmartMine catches the doughboy, 
         * 
         * Internally, the Game class has a List<SmartMine> which contains all of the smartmines. If two
         * collide, accidentally or not, they immediately explode.
         * 
         * I'm hoping the animations for the SmartMine will be fairly complicated- depends on what Kris
         * does, anyways. I'm envisioning this sort of insect-like hexapod which tends to gallop but
         * occasionally rolls very quickly in a straight line. Not menacing like a street thug; more 
         * like the land embodiment of a Predator drone. Should also skid into corners.
         */

        public int startTurn;
        public int owner;
        public Vector2 screenPos;
        public IntVector2 gridPos;
        public bool isActive;
        public float activationTime;

        public int animationPhase;
        public float animationTime;

        public static int activationRadius = 5;

        public SmartMine(int CurrentTurn, IntVector2 gridPosition) {
            startTurn = (CurrentTurn+4)%6;//turns turn 0 (p1 shop) to 4 (p2 run), and vice versa.
            //So: If the mine was placed by player 1, it wil activate for player 2.
            owner = CurrentTurn / 3;
            gridPos = gridPosition;
            screenPos = 64 * gridPosition.ToVector2();
            isActive = false;
        }

        public bool ShouldActivate() {
            if (Game1.whoseTurn != startTurn) return false;
            if (Game1.currentDoughboy < 0 || Game1.currentDoughboy >= Game1.breadbox.doughboys.Count) return false;
            Vector2 dpos = Game1.breadbox.doughboys[Game1.currentDoughboy].screenPos;
            if ((screenPos - dpos).Length() > 64 * activationRadius) return false;
            return true;
        }

        public void Update(GameTime gameTime) {
        }

        public void Draw(GameTime gameTime) {
        }
    }
}

/*Back, back on the set and coverin' all bets
  Smartbomb

  We came to drop bombs
  Calling every man to arms
  And yo sound the alarm
  Smartbomb*/

