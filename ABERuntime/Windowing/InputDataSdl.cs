using System;
using static SDL2.SDL;
using System.Collections.Generic;
using System.Numerics;
using WGIL.IO;
using System.Text;

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

        public void HandleKeyEvent(in SDL_KeyboardEvent keyEvent)
        {
            KeyEvents.Add(new KeyEvent((int)keyEvent.keysym.scancode, keyEvent.state == 1));
        }

        public void HandleMouseMotionEvent(in SDL_MouseMotionEvent mouseMotionEvent)
        {
            //Vector2 delta = new Vector2(mouseMotionEvent.xrel, mouseMotionEvent.yrel);

            MousePosition = new Vector2(mouseMotionEvent.x, mouseMotionEvent.y);
        }

        public void HandleMouseButtonEvent(in SDL_MouseButtonEvent mouseButtonEvent)
        {
            MouseEvents.Add(new MouseEvent((int)mouseButtonEvent.button, mouseButtonEvent.state == 1));
        }

        public unsafe void HandleTextInputEvent(SDL_TextInputEvent textInputEvent)
        {
            uint byteCount = 0;
            // Loop until the null terminator is found or the max size is reached.
            while (byteCount < SDL_TEXTINPUTEVENT_TEXT_SIZE && textInputEvent.text[byteCount++] != 0)
            { }

            if (byteCount > 1)
            {
                // We don't want the null terminator.
                byteCount -= 1;
                int charCount = Encoding.UTF8.GetCharCount(textInputEvent.text, (int)byteCount);
                char* charsPtr = stackalloc char[charCount];
                Encoding.UTF8.GetChars(textInputEvent.text, (int)byteCount, charsPtr, charCount);
                for (int i = 0; i < charCount; i++)
                {
                    KeyCharPresses.Add(charsPtr[i]);
                }
            }
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

