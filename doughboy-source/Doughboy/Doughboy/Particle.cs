using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Doughboy
{
    public class Particle:BaseRoutines
    {
        Vector2 pos, vel;
        Color c;
        float t;
        static Vector2 gravity = new Vector2(0, 256f);
        static float maxtime = 0.5f;

        public bool removeplz;

        public Particle(Vector2 Position, Vector2 Velocity, Color color)
        {
            pos = Position;
            vel = Velocity;
            c = color;
            t = 0;
            removeplz = false;
        }

        public Particle(Vector2 Position, Vector2 Velocity, int tone)
        {
            pos = Position;
            vel = Velocity;
            c = Color.FromNonPremultiplied(tone, tone, tone, 255);
            t = 0;
            removeplz = false;
        }

        public void Update(GameTime gameTime)
        {
            float e=(float)gameTime.ElapsedGameTime.TotalSeconds;
            t += e;
            //c.A = (byte)(255 * (maxtime - t));
            removeplz = (t > maxtime);
            pos += e * vel;
            vel += e*gravity;
        }

        public void Draw()
        {
            Game1.spriteBatch.Draw(Game1.pixeltex, trunc(pos,2), c);
        }
    }
}
