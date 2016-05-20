using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Doughboy
{
    public class InputHandler
    {

        public KeyboardState lastKeyboardState;
        public KeyboardState keyboardState;
        public MouseState lastMouseState;
        public MouseState mouseState;
        GamePadState lastGamePadState;
        GamePadState GamePadState;
        public Dictionary<Keys, int> keyvals;
        public Keys[] knownKeys;
        public Vector2 mousePosition;
        public Vector2 lastMousePosition;
        public float scale;


        public InputHandler()
        {

            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;
            knownKeys = new Keys[] { Keys.W, Keys.A, Keys.S, Keys.D, Keys.F24 };
            keyvals = new Dictionary<Keys, int>();
            foreach (Keys k in knownKeys)
            {
                keyvals.Add(k, 0);
            }
            scale = 1.0f;
        }

        public void Update(bool IsInFocus)
        {
            //TIP: IsInFocus=Game.IsActive
            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            //Supress clicks if window is not on top

            lastMousePosition = mousePosition;
            mousePosition = new Vector2(mouseState.X, mouseState.Y); //But it's not that simple...
            Vector2 windowSize = new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
            Vector2 gameSize = Game1.screenSize.ToVector2();
            scale = 1.0f;
            if (windowSize.X<gameSize.X || windowSize.Y<gameSize.Y) {
                gameSize *= 0.5f;
                scale = 0.5f;
            }
            mousePosition -= (windowSize - gameSize) / 2; //get top left and subtract
            mousePosition /= scale;
            foreach (Keys k in knownKeys)
            {
                if (IsKeyPressed(k))
                {
                    keyvals[k]++;
                }
                else
                {
                    keyvals[k] = 0;
                }
            }

            //lastly, if the window is no longer in focus then it's not clicked on
            if (!IsInFocus) {
                lastMouseState = new MouseState(lastMouseState.X, lastMouseState.Y, lastMouseState.ScrollWheelValue,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released); //what I say five times is true
                mouseState = new MouseState(mouseState.X, mouseState.Y, mouseState.ScrollWheelValue,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            }
        }

        public bool KeyJustPressed(Keys key)
        {
            return lastKeyboardState.IsKeyUp(key) && keyboardState.IsKeyDown(key);
        }
        public bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }
        public bool IsShiftPressed()
        {
            return keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        }
        public bool IsNav(Keys key)
        {
            return KeyJustPressed(key) || IsKeyHeldDown(key);
        }
        public bool IsKeyHeldDown(Keys key) { return IsKeyHeldDown(key, 30, 10); }
        public bool IsKeyHeldDown(Keys key, int minlen, int mod)
        {
            return keyvals[key] > minlen && (keyvals[key] % mod == 0);
        }

        /*public bool ButtonJustPressed(Buttons button)
         * 
        {
            
        }*/
    }
}
