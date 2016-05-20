using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Doughboy
{
    public class Player
    {
        public int money;
        public int health;
        public int side;

        public Player(int Side, int startingCash)
        {
            money = startingCash;
            side = Side;
            health = 100;
        }

        public void RegisterHit()
        {
            health -= 10;
            IntVector2 origin = side == 0 ? new IntVector2(0, 3 * 64) : new IntVector2(12 * 64, 3 * 64);
            if (health > 0) {
                Game1.hammieham.addCue("splosion"); //ow! Additional splosions are handled by Animation.cs

                Random r = new Random();
                for (int i = 0; i < (100 - health) / 10; i++)
                    Game1.animations.Add(new Animation(16, origin + 64 * (new IntVector2(r.Next(2), r.Next(6)-1)), false, 1, (float)i / 4.0f));
            }

            if (health <= 0)
            {
                Game1.texter.AddMessage("PW" + (2-side).ToString());
                Game1.endTime = 0.0f;
                for (int x = 0; x < 2; x++) {
                    for (int y = 0; y < 6; y++) {
                        Game1.animations.Add(new Animation(16, origin + 64 * new IntVector2(x, y-1), false, 1));
                        Game1.hammieham.addCue("splosion2");
                    }
                }
            }
        }

        public void DrawBunker(int alpha)
        {
            Game1.spriteBatch.Draw(Game1.bunkerimages[side][Math.Min((100 - health) / 20,5)], side == 0 ? new Vector2(0, 3 * 64) : new Vector2(12 * 64, 3 * 64), Color.FromNonPremultiplied(255,255,255,alpha));
        }
    }
}
