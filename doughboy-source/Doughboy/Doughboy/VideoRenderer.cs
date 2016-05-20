using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;

namespace Doughboy {
    public class VideoRenderer {
        LinkedList<Texture2D> frames;
        int frameCount;
        Thread vr;
        String foldername = @"C:\\DoughboyFrames\";
        //bool IsWritingFrame;

        public VideoRenderer() {
            frameCount = 0;
            //IsWritingFrame = false;
            frames = new LinkedList<Texture2D>();
            if (!Directory.Exists(foldername)) {
                Directory.CreateDirectory(foldername);
            }
            vr = new Thread(new ThreadStart(SaveFrames));
            vr.Start();
        }

        public void AddCopiedFrame(Texture2D tex) {
            //IsWritingFrame = true;
            Color[] pixels = new Color[tex.Width * tex.Height];
            tex.GetData<Color>(pixels);
            Texture2D ntex = new Texture2D(tex.GraphicsDevice, tex.Width, tex.Height);
            ntex.SetData<Color>(pixels);
            frames.AddLast(ntex);
            //IsWritingFrame = false;
        }

        private void SaveFrames() {
            while (true) {
                System.Threading.Thread.Sleep(10);
                if (frames.Count > 0) {
                    Stream stream = File.Open(foldername+frameCount.ToString().PadLeft(6, '0') + ".png", FileMode.Create);
                    Texture2D frame = frames.First.Value;
                    frame.SaveAsPng(stream, frame.Width, frame.Height);
                    stream.Close();
                    frame.Dispose();
                    frames.RemoveFirst();
                    frameCount++;
                    if (frames.Count == 0) {
                        Console.WriteLine("Video renderer finished.");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }

        public void StopThreads() {
            vr.Abort();
        }


        /*public static void TextureToPng(this Texture2D texture, int width, int height, ImageFormat imageFormat, string filename) {
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb)) {
                byte blue;
                IntPtr safePtr;
                BitmapData bitmapData;
                Rectangle rect = new Rectangle(0, 0, width, height);
                byte[] textureData = new byte[4 * width * height];

                texture.GetData<byte>(textureData);
                for (int i = 0; i < textureData.Length; i += 4) {
                    blue = textureData[i];
                    textureData[i] = textureData[i + 2];
                    textureData[i + 2] = blue;
                }
                bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                safePtr = bitmapData.Scan0;
                Marshal.Copy(textureData, 0, safePtr, textureData.Length);
                bitmap.UnlockBits(bitmapData);
                bitmap.Save(filename, imageFormat);
            }
        }*/
    }
}
