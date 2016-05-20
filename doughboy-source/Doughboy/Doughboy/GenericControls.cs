using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Doughboy {
    class Button:BaseRoutines {
        public Texture2D tex;
        public Vector2 pos;
        public Vector2 offset;
        public bool IsHighlighted;
        public bool IsClicked;
        public bool IsJustClicked;
        public bool IsRect;

        public Button(Texture2D texture, Vector2 position, bool rectangular) {
            tex = texture;
            pos = position;
            IsRect = rectangular;
            offset = Vector2.Zero;
        }

        public void Draw() {
            if (IsHighlighted) {
                Game1.spriteBatch.Draw(tex, offset+pos, Color.Gray);
            } else {
                Game1.spriteBatch.Draw(tex, offset + pos, Color.White);
            }
        }

        public void Update() {
            IsHighlighted = false;
            IsClicked = false;
            IsJustClicked = false;

            if (IsInRegion(Game1.input.mousePosition, pos+offset, pos+offset + (new Vector2(tex.Width, tex.Height)))) {
                if (IsRect) {
                    IsHighlighted = true;
                } else {
                    Color[] pix=new Color[1];
                    tex.GetData<Color>(0, new Rectangle((int)Game1.input.mousePosition.X,(int)Game1.input.mousePosition.Y, 1, 1), pix, 1, 1);
                    if (pix[0].A > 128) {
                        IsHighlighted = true;
                    }
                }
            }

            if (IsHighlighted && Game1.input.mouseState.LeftButton == ButtonState.Pressed) {
                IsClicked = true;
                if (Game1.input.lastMouseState.LeftButton == ButtonState.Released) {
                    IsJustClicked = true;
                }
            }
        }
    }
}
