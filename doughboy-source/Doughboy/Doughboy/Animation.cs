using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Doughboy
{
    public class Animation
    {
        public int animationID;
        int currentFrame;
        int frameRate = 10;
        double totalTime;

        int numFrames;

        public IntVector2 pos;
        bool IsReversed;

        bool fade;
        public bool isOnGrid;

        public bool isDone;
        public int layer;
        /// <summary>
        /// Layers: -1 is back (below all sprites), 0 is below bunkers, 1 is above all. 2 is special: these are drawn as the grid is drawn.
        /// </summary>
        /// <param name="animID"></param>
        /// <param name="position"></param>
        /// <param name="fadeOut"></param>
        /// <param name="Layer"></param>
        public Animation(int animID, IntVector2 position, bool fadeOut, int Layer)
        {
            //If this belongs to a square, choose the animation automatically
            GetAnimationID(position, animID);
            currentFrame = 0;
            totalTime = 0;
            isDone = false;
            pos = position;
            numFrames = Game1.animationframes[animationID].Length;

            fade=fadeOut;

            layer = Layer;
            IsReversed = false;
        }

        public Animation(int animID, IntVector2 position, bool fadeOut, int Layer, bool reverseQ) {
            //If this belongs to a square, choose the animation automatically
            GetAnimationID(position, animID);
            IsReversed = true;
            numFrames = Game1.animationframes[animationID].Length;
            currentFrame = numFrames - 1;
            totalTime = 0;
            isDone = false;
            pos = position;


            fade = fadeOut;

            layer = Layer;
        }

        public Animation(int animID, IntVector2 position, bool fadeOut, int Layer, float timeDelay)
        {
            //If this belongs to a square, choose the animation automatically
            GetAnimationID(position, animID);
            currentFrame = -240; //large neg num
            totalTime = -timeDelay;
            isDone = false;
            pos = position;
            numFrames = Game1.animationframes[animationID].Length;

            fade = fadeOut;

            layer = Layer;
            IsReversed = false;
        }

        private void GetAnimationID(IntVector2 position,int defaultID)
        {
            if (position.X % 64 == 0 && position.Y % 64 == 0)
            {
                int y = position.Y / 64;
                int x = position.X / 64;
                isOnGrid = true;
                if (defaultID >= 6 && defaultID <= 8 || defaultID==16)
                {
                    switch (Game1.board[y, x])
                    {
                        case 1:
                            animationID = 8;
                            break;
                        case 5:
                            animationID = 7;
                            break;
                        default:
                            animationID = defaultID;
                            break;
                    }
                }
                else
                {
                    animationID = defaultID;
                }
            }
            else
            {
                animationID = defaultID;
                isOnGrid = false;
            }
        }

        public void Update(GameTime gameTime)
        {
            totalTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (currentFrame<0 && ((int)(totalTime*frameRate)>=0) && animationID==16)
            {
                string ts = "splosion" + Game1.rand.Next(1, 3).ToString();
                if (ts[8] == '1')
                    ts = ts.Substring(0, 8);
                Game1.hammieham.addCue(ts);
            }

            if (IsReversed) {
                currentFrame = numFrames-(int)(totalTime * frameRate+1);
            } else {
                currentFrame = (int)(totalTime * frameRate); //WARNING this will crash if you leave the game running for more than 68 years
            }

            if(fade){
                isDone = (currentFrame >= numFrames+4);
            }else{
                if (IsReversed) {
                    isDone = (currentFrame<0);
                } else {
                    isDone = (currentFrame >= numFrames);
                }
            }
        }

        public void Draw()
        {
            if (!isDone && animationID==18) {
                Console.WriteLine(currentFrame);
                Console.WriteLine(pos.ToVector2());
            }
            if (!isDone && currentFrame>=0)
            {
                if (currentFrame >= numFrames) {
                        Game1.spriteBatch.Draw(Game1.animationframes[animationID][numFrames - 1], pos.ToVector2(), Color.FromNonPremultiplied(255, 255, 255, 255 - 32 * (currentFrame - numFrames)));
                    } else {
                        Game1.spriteBatch.Draw(Game1.animationframes[animationID][currentFrame], pos.ToVector2(), Color.White);
                    }
                
            }
        }
    }
}
