using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace ABEngine.ABERuntime
{
    public static class Input
    {
        private static HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private static HashSet<Key> _newKeysThisFrame = new HashSet<Key>();
        private static HashSet<Key> _keyUpThisFrame = new HashSet<Key>();

        private static HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private static HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();
        private static HashSet<MouseButton> _mouseUpThisFrame = new HashSet<MouseButton>();

        private static Dictionary<string, List<Key>> buttonMappings = new Dictionary<string, List<Key>>();
        private static Dictionary<string, List<AxisMapping>> axisMappings = new Dictionary<string, List<AxisMapping>>();

        public static Vector2 MousePosition;
        public static float MouseScrollDelta;
        public static InputSnapshot FrameSnapshot { get; private set; }

        private static float _XAxis;
        private static float _YAxis;

        public static float XAxis
        { get
            {
                foreach (var axisMap in axisMappings["XAxis"])
                {
                    if (GetKey(axisMap.key))
                    {
                        _XAxis += axisMap.axisWeight;
                    }
                }

                return _XAxis;
            }
        }

        public static float YAxis
        {
            get
            {
                foreach (var axisMap in axisMappings["YAxis"])
                {
                    if (GetKey(axisMap.key))
                    {
                        _YAxis += axisMap.axisWeight;
                    }
                }

                return _YAxis;
            }
        }

        public static float AxisDeadzone = 0.1f;

        static Input()
        {
            List<AxisMapping> xMappings = new List<AxisMapping>()
            {
                new AxisMapping() { key = Key.D, axisWeight = 1 },
                new AxisMapping() { key = Key.Right, axisWeight = 1 },
                new AxisMapping() { key = Key.A, axisWeight = -1 },
                new AxisMapping() { key = Key.Left, axisWeight = -1 }
            };

            List<AxisMapping> yMappings = new List<AxisMapping>()
            {
                new AxisMapping() { key = Key.W, axisWeight = 1 },
                new AxisMapping() { key = Key.Up, axisWeight = 1 },
                new AxisMapping() { key = Key.S, axisWeight = -1 },
                new AxisMapping() { key = Key.Down, axisWeight = -1 }
            };

            axisMappings.Add("XAxis", xMappings);
            axisMappings.Add("YAxis", yMappings);
        }

        public static Vector2 GetMousePosition()
        {
            return new Vector2(MousePosition.X, Game.screenSize.Y - MousePosition.Y);
        }

        public static bool GetKey(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        public static bool GetKeyUp(Key key)
        {
            return _keyUpThisFrame.Contains(key);
        }

        public static bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            return _mouseUpThisFrame.Contains(button);
        }

        public static bool GetButton(string button)
        {
            if (buttonMappings.TryGetValue(button, out List<Key> keys))
                foreach (var key in keys)
                    if (GetKey(key))
                        return true;

            return false;
        }

        public static bool GetButtonDown(string button)
        {
            if (buttonMappings.TryGetValue(button, out List<Key> keys))
                foreach (var key in keys)
                    if (GetKeyDown(key))
                        return true;

            return false;
        }


        public static bool GetButtonUp(string button)
        {
            if (buttonMappings.TryGetValue(button, out List<Key> keys))
                foreach (var key in keys)
                    if (GetKeyUp(key))
                        return true;

            return false;
        }

        public static void AddButtonMapping(ButtonMapping buttonMapping)
        {
            if (buttonMappings.ContainsKey(buttonMapping.buttonName))
                buttonMappings[buttonMapping.buttonName] = buttonMapping.keys;
            else
                buttonMappings.Add(buttonMapping.buttonName, buttonMapping.keys);
        }


        public static void UpdateFrameInput(InputSnapshot snapshot)
        {
            FrameSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();
            _mouseUpThisFrame.Clear();
            _keyUpThisFrame.Clear();

            MousePosition = snapshot.MousePosition;
            MouseScrollDelta = snapshot.WheelDelta;
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent ke = snapshot.KeyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }

            _XAxis = NormalizeAxis(snapshot.XAxis);
            _YAxis = NormalizeAxis(snapshot.YAxis);
        }

        private static float NormalizeAxis(float value)
        {
            float absVal = MathF.Abs(value);
            float sign = MathF.Sign(value);

            float normValue = Math.Clamp(absVal / 32767f - AxisDeadzone, 0f, 1f);
            return sign * (normValue / (1f - AxisDeadzone));
            
        }

        private static void MouseUp(MouseButton mouseButton)
        {
            _currentlyPressedMouseButtons.Remove(mouseButton);
            _newMouseButtonsThisFrame.Remove(mouseButton);
            _mouseUpThisFrame.Add(mouseButton);
        }

        private static void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private static void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
            _keyUpThisFrame.Add(key);
        }

        private static void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
            }
        }
    }

    public class AxisMapping
    {
        public Key key { get; set; }
        public float axisWeight { get; set; }
        public string axisName;
    }

    public class ButtonMapping
    {
        public List<Key> keys;
        public string buttonName;

        public ButtonMapping()
        {
            keys = new List<Key>();
        }
    }
}