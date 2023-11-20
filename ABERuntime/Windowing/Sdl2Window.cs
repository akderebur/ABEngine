using System;
using WGIL;
using static SDL2.SDL;

namespace ABEngine.ABERuntime.Windowing
{
    // Adapted from Veldrid.SDL2
    public class Sdl2Window
    {
        public IntPtr Handle;
        uint WindowID;

        public event Action Closing;
        public event Action Resized;
        public event Action FocusGained;
        public event Action FocusLost;

        nint metalView = IntPtr.Zero;

        public Sdl2Window(string title, int x, int y, int width, int height, SDL_WindowFlags flags, out RawWindowInfo windowInfo)
        {
            SDL_SetHint("SDL_MOUSE_FOCUS_CLICKTHROUGH", "1");

            Handle = SDL_CreateWindow(title, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, width, height, flags);
            WindowID = SDL_GetWindowID(Handle);

            if ((flags & SDL_WindowFlags.SDL_WINDOW_SHOWN) == SDL_WindowFlags.SDL_WINDOW_SHOWN)
            {
                SDL_ShowWindow(Handle);
            }

            SDL_SysWMinfo sysWmInfo = new SDL_SysWMinfo();
            SDL_VERSION(out sysWmInfo.version);
            SDL_GetWindowWMInfo(Handle, ref sysWmInfo);

            // Physical Size
            SDL_GL_GetDrawableSize(Handle, out int pw, out int ph);

            windowInfo = new()
            {
                physicalWidth = (uint)pw,
                physicalHeight = (uint)ph
            };

            switch (sysWmInfo.subsystem)
            {
                case SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS:
                    windowInfo.windowHandle = sysWmInfo.info.win.window;
                    windowInfo.viewHandle = sysWmInfo.info.win.hinstance;
                    windowInfo.windowOS = WindowOS.Windows;
                    break;
                case SDL_SYSWM_TYPE.SDL_SYSWM_COCOA:
                    windowInfo.windowHandle = sysWmInfo.info.cocoa.window;
                    metalView = SDL_Metal_CreateView(Handle);
                    windowInfo.viewHandle = metalView;
                    windowInfo.windowOS = WindowOS.MacOS;
                    break;
                default:
                    break;
            }
        }

        public void ProcessEvents(InputDataSdl inputData)
        {
            SDL_Event ev;
            while (SDL_PollEvent(out ev) == 1)
            {
                switch (ev.type)
                {
                    case SDL_EventType.SDL_QUIT:
                        Close();
                        break;
                    case SDL_EventType.SDL_WINDOWEVENT:
                        HandleWindowEvent(ev.window);
                        break;
                    case SDL_EventType.SDL_KEYDOWN:
                    case SDL_EventType.SDL_KEYUP:
                        inputData.HandleKeyEvent(ev.key);
                        break;
                    case SDL_EventType.SDL_MOUSEMOTION:
                        inputData.HandleMouseMotionEvent(ev.motion);
                        break;
                    case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    case SDL_EventType.SDL_MOUSEBUTTONUP:
                        inputData.HandleMouseButtonEvent(ev.button);
                        break;
                    case SDL_EventType.SDL_TEXTINPUT:
                        inputData.HandleTextInputEvent(ev.text);
                        break;
                    default:
                        break;
                }
            }
        }

        void HandleWindowEvent(in SDL_WindowEvent e)
        {
            switch (e.windowEvent)
            {
                case SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                case SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                case SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                case SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                    Resize();
                    break;
                case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    FocusGained?.Invoke();
                    break;
                case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    FocusLost?.Invoke();
                    break;
                default:
                    break;
            }
        }

        void Resize()
        {
            Resized?.Invoke();
        }

        void Close()
        {
            Closing?.Invoke();
            if (metalView != IntPtr.Zero)
                SDL_Metal_DestroyView(metalView);
            SDL_DestroyWindow(Handle);
        }
    }
}

