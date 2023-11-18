using System;
using static SDL2.SDL;
using System.Collections.Generic;
using System.Numerics;
using WGIL.IO;

namespace ABEngine.ABERuntime.Windowing
{
    public class InputDataSdl : IInputData
    {
        public List<WGIL.IO.KeyEvent> KeyEvents { get; set; }
        public List<WGIL.IO.MouseEvent> MouseEvents { get; set; }
        public List<char> KeyCharPresses { get; set; }

        public Vector2 MousePosition { get; set; }
        public float XAxis { get; set; }
        public float YAxis { get; set; }

        public InputDataSdl()
        {
            KeyEvents = new List<WGIL.IO.KeyEvent>();
            MouseEvents = new List<WGIL.IO.MouseEvent>();
            KeyCharPresses = new List<char>();
        }

        public void HandleKeyEvent(SDL_KeyboardEvent sdlKeyEvent)
        {
            KeyEvents.Add(new KeyEvent((int)sdlKeyEvent.keysym.scancode, sdlKeyEvent.state == 1));
        }

        public bool IsMouseDown(MouseButton mouseButton)
        {
            return Input.GetMouseButton(mouseButton);
        }

        public void Clear()
        {
            KeyEvents.Clear();
            MouseEvents.Clear();
            KeyCharPresses.Clear();

            XAxis = 0;
            YAxis = 0;
        }
    }
}

