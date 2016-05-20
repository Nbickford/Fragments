using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace Doughboy
{
    public class BaseRoutines
    {

        public static bool Contains(int val, int[] lis) {
            for (int i = 0; i < lis.Length; i++)
                if (lis[i] == val)
                    return true;
            return false;
        }

        public static void Copy2DArray(int[,] ar, out int[,] outar) {
            outar = new int[ar.GetLength(0), ar.GetLength(1)];
            for (int i = 0; i < ar.GetLength(0); i++)
                for (int j = 0; j < ar.GetLength(1); j++)
                    outar[i, j] = ar[i, j];
        }

        public static int[,] Copy2DArray(int[,] ar) {
            int[,] outar = new int[ar.GetLength(0), ar.GetLength(1)];
            for (int i = 0; i < ar.GetLength(0); i++)
                for (int j = 0; j < ar.GetLength(1); j++)
                    outar[i, j] = ar[i, j];
            return outar;
        }

        public static bool ArrayEqual(int[,] a, int[,] b){
            if(a.GetLength(0)!=b.GetLength(0))return false;
            if(a.GetLength(1)!=b.GetLength(1))return false;
            for(int i=0;i<a.GetLength(0);i++){
                for(int j=0;j<b.GetLength(1);j++){
                    if(a[i,j]!=b[i,j])return false;
                }
            }
            return true;
        }

        public static void SetPreference(string pref, bool val) {
            //TODO: be more efficient
            try {
                string[] preffile = File.ReadAllLines("settings.txt");
                int i=0;
                while (!preffile[i].Contains(pref)) {
                    i++;
                }
                preffile[i] = pref + " " + val.ToString();
                File.WriteAllLines("settings.txt", preffile);
            } catch (Exception ex) {
                throw new Exception("Look, I don't know what you did to your settings.txt file, but it's broken. Here's the error C# gave:\n" + ex.ToString());
            }
        }

        public static void SetPreferenceString(string pref, string val) {
            try {
                string[] preffile = File.ReadAllLines("settings.txt");
                int i = 0;
                while (!preffile[i].Contains(pref)) {
                    i++;
                }
                preffile[i] = pref + " " + val.ToString();
                File.WriteAllLines("settings.txt", preffile);
            } catch (Exception ex) {
                throw new Exception("Look, I don't know what you did to your settings.txt file, but it's broken. Here's the error C# gave:\n" + ex.ToString());
            }
        }

        public static bool ReadPreference(string pref) {
            try {
                string[] preffile = File.ReadAllLines("settings.txt");
                int i = 0;
                while (!preffile[i].Contains(pref)) {
                    i++;
                }
                return preffile[i].ToLower().Contains("true");
            } catch (Exception ex) {
                throw new Exception("Look, I don't know what you did to your settings.txt file, but it's broken. Here's the error C# gave:\n" + ex.ToString());
            }
        }

        public static string ReadPreferenceString(string pref) {
            try {
                string[] preffile = File.ReadAllLines("settings.txt");
                int i = 0;
                while (!preffile[i].Contains(pref)) {
                    i++;
                }
                String str=preffile[i].ToLower();
                return str.Substring(pref.Length + 1);
            } catch (Exception ex) {
                throw new Exception("Look, I don't know what you did to your settings.txt file, but it's broken. Here's the error C# gave:\n" + ex.ToString());
            }
        }

        public static Rectangle[] rects = new Rectangle[10]{
                new Rectangle(416,320,32+4,32+6),
                new Rectangle(0,320,32-2,32+6),
                new Rectangle(32,320,32+10,32+6),
                new Rectangle(80,320,32+6,32+6),
                new Rectangle(128,320,32+8,32+6),
                new Rectangle(176,320,32+8,32+6),
                new Rectangle(224,320,32+4,32+6),
                new Rectangle(272,320,32+6,32+6),
                new Rectangle(320,320,32+6,32+6),
                new Rectangle(368,320,32+6,32+6)};

        public static IntVector2 getMouseArrayPos()
        {
            IntVector2 result = new IntVector2(
                Game1.input.mousePosition/64);
            return result;
        }

        public static bool IsInRegion(Vector2 pos,int xlo, int xhi, int ylo, int yhi)
        {
            return (xlo <= pos.X) && (pos.X < xhi) && (ylo <= pos.Y) && (pos.Y < yhi);
        }

        public static bool IsInRegion(Vector2 pos, Vector2 lo, Vector2 hi)
        {
            return (lo.X <= pos.X) && (pos.X < hi.X) && (lo.Y <= pos.Y) && (pos.Y < hi.Y);
        }

        public bool Contains(int[] lis, int val)
        {
            for (int i = 0; i < lis.Length; i++)
            {
                if (lis[i] == val)
                    return true;
            }
            return false;
        }

        public static float trunc(float a, int m)
        {
            return a - (a % m);
        }

        public float ceil(float a, int m)
        {
            return trunc(a + 1, m); //doesn't actually work for some cases
        }

        public float round(float a, int m)
        {
            if (a % m < m / 2)
            {
                return trunc(a, m);
            }
            return ceil(a, m);
        }

        public static Vector2 trunc(Vector2 a, int m)
        {
            return new Vector2(trunc(a.X, m), trunc(a.Y, m));
        }


        /* TEXT ROUTINES*/
        public static void DrawPGoCentered(Vector2 center, int n)
        {
            Vector2 size = new Vector2(8 * 32 + (n==1?2:6), 32 + 12);
            Game1.spriteBatch.Draw(Game1.words, center - size / 2, new Rectangle(0, (n==1?0:64), (int)size.X, (int)size.Y), Color.White);
        }

        public static void DrawPWinCentered(Vector2 center, int n)
        {
            Vector2 size = new Vector2(8 * 32 + 12, 32 + 2);
            Game1.spriteBatch.Draw(Game1.words, center - size / 2, new Rectangle(0, (n == 1 ? 128 : 192), (int)size.X, (int)size.Y), Color.White);
        }

        public static void DrawRoundCentered(Vector2 center, int round)
        {
            string num = round.ToString(); //WARNING this will break if round>2^31-1who does that.
            int width = 202 + 10 + GetIntWidth(round);
            int height = 32 + 6;
            Vector2 tl = center - new Vector2(width, height) / 2;
            Game1.spriteBatch.Draw(Game1.words, tl, new Rectangle(0, 256, 202, 32 + 6), Color.White);
            DrawInt(tl+(202+10)*Vector2.UnitX, round);
        }

        public static void DrawInt(Vector2 tl, int num)
        {
            string n = num.ToString();
            int width=0;

            int d;
            for (int i = 0; i < n.Length; i++)
            {
                d = int.Parse(n[i].ToString());
                if (d == 7) width += 5;
                Game1.spriteBatch.Draw(Game1.words, tl + Vector2.UnitX * width, rects[d], Color.White);
                width += rects[d].Width;
                if (d == 7) width -= 5;
            }
        }

        public static int GetIntWidth(int num)
        {
            string n = num.ToString();
            int width = 0;
            int d;
            for (int i = 0; i < n.Length; i++)
            {
                d = int.Parse(n[i].ToString());
                width += rects[d].Width;
            }
            return width;
        }

        public static float Hermite(float p0, float m0, float p1, float m1, float t)
        {
            float t3 = t * t * t;
            float t2 = t * t;
            return (2*t3-3*t2+1)*p0+
                    (t3-2*t2+t)*m0+
                    (-2*t3+3*t2)*p1+
                    (t3-t2)*m1;

        }
    }
}
