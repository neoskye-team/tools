using System.Runtime.InteropServices;

using ImGuiNET;

using static SDL2.SDL;

public partial class ImGUIRenderer
{
    private Dictionary<ImGuiMouseCursor, IntPtr> mouseCursors;
    private Dictionary<int, bool> mouseButtons;
    private int pendingMouseLeaveFrame;
    private ulong time;
    private ulong frequency;

    internal bool InitForSDLRenderer()
    {
        var io = ImGui.GetIO();
        var sdlBackend = SDL_GetCurrentVideoDriver();
        bool mouseCanUseGlobalState =
            new string[] { "windows", "cocoa", "x11", "DIVE", "VMAN" }
            .Contains(sdlBackend);
        // io.BackendPlatformName = "imgui_sdlrenderer_csharp";
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;

        // io.SetClipboardTextFn = ImGui_ImplSDL2_SetClipboardText;
        // io.GetClipboardTextFn = ImGui_ImplSDL2_GetClipboardText;
        // io.ClipboardUserData = IntPtr.Zero;

#if WIN32
        SDL_SysWMinfo inf = new();
        SDL_VERSION(out inf.version);
        if (SDL_GetWindowWMInfo(win, ref inf) == SDL_bool.SDL_TRUE)
        {
            var vwp = ImGui.GetMainViewport();
            vwp.PlatformHandleRaw = inf.info.win.window;
        }
#endif

#if SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH
        SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1")
#endif

#if SDL_HINT_MOUSE_AUTO_CAPTURE
        SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
#endif

        return true;
    }

    private unsafe bool HandleEvent(ref SDL_Event ev)
    {
        var io = ImGui.GetIO();

        switch (ev.type)
        {
            case SDL_EventType.SDL_MOUSEMOTION:
                io.AddMousePosEvent((float)ev.motion.x, (float)ev.motion.y);
                return true;
            case SDL_EventType.SDL_MOUSEWHEEL:
                float wheelX = (ev.wheel.x > 0) ? 1.0f : (ev.wheel.x < 0) ? -1.0f : 0.0f;
                float wheelY = (ev.wheel.y > 0) ? 1.0f : (ev.wheel.y < 0) ? -1.0f : 0.0f;
                io.AddMouseWheelEvent(wheelX, wheelY);
                return true;
            case SDL_EventType.SDL_MOUSEBUTTONDOWN:
            case SDL_EventType.SDL_MOUSEBUTTONUP:
                int mouseButton = -1;
                var btn = ev.button.button;
                if (btn == SDL_BUTTON_LEFT) btn = 0;
                if (btn == SDL_BUTTON_RIGHT) btn = 1;
                if (btn == SDL_BUTTON_MIDDLE) btn = 2;
                if (btn == SDL_BUTTON_X1) btn = 3;
                if (btn == SDL_BUTTON_X2) btn = 4;
                if (mouseButton == -1)
                    return false;
                io.AddMouseButtonEvent(mouseButton, (ev.type == SDL_EventType.SDL_MOUSEBUTTONDOWN));
                mouseButtons[mouseButton] = ev.type == SDL_EventType.SDL_MOUSEBUTTONDOWN;
                return true;
            case SDL_EventType.SDL_TEXTINPUT:
                fixed (byte* ptr = ev.text.text)
                    io.AddInputCharactersUTF8(
                        Marshal.PtrToStringUTF8((nint)ptr)
                    );
                return true;
            case SDL_EventType.SDL_KEYDOWN:
            case SDL_EventType.SDL_KEYUP:
                UpdateKeyModifiers(ev.key.keysym.mod);
                var key = KeycodeToImGUIKey(ev.key.keysym.sym);
                io.AddKeyEvent(key, ev.type == SDL_EventType.SDL_KEYDOWN);
                io.SetKeyEventNativeData(key, (int)ev.key.keysym.sym, (int)ev.key.keysym.scancode, (int)ev.key.keysym.scancode);
                return true;
            case SDL_EventType.SDL_WINDOWEVENT:
                var windowevent = ev.window.windowEvent;
                if (windowevent == SDL_WindowEventID.SDL_WINDOWEVENT_ENTER)
                    pendingMouseLeaveFrame = 0;
                if (windowevent == SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
                    pendingMouseLeaveFrame = ImGui.GetFrameCount() + 1;
                if (windowevent == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
                    io.AddFocusEvent(true);
                else if (windowevent == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
                    io.AddFocusEvent(false);
                return true;
        }
        return false;
    }

    private void UpdateKeyModifiers(SDL_Keymod keymod)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ImGuiMod_Ctrl, (keymod & SDL_Keymod.KMOD_CTRL) != 0);
        io.AddKeyEvent(ImGuiKey.ImGuiMod_Shift, (keymod & SDL_Keymod.KMOD_SHIFT) != 0);
        io.AddKeyEvent(ImGuiKey.ImGuiMod_Alt, (keymod & SDL_Keymod.KMOD_ALT) != 0);
        io.AddKeyEvent(ImGuiKey.ImGuiMod_Super, (keymod & SDL_Keymod.KMOD_GUI) != 0);
    }

    private ImGuiKey KeycodeToImGUIKey(SDL_Keycode keycode)
    {
        switch (keycode)
        {
            case SDL_Keycode.SDLK_TAB: return ImGuiKey.Tab;
            case SDL_Keycode.SDLK_LEFT: return ImGuiKey.LeftArrow;
            case SDL_Keycode.SDLK_RIGHT: return ImGuiKey.RightArrow;
            case SDL_Keycode.SDLK_UP: return ImGuiKey.UpArrow;
            case SDL_Keycode.SDLK_DOWN: return ImGuiKey.DownArrow;
            case SDL_Keycode.SDLK_PAGEUP: return ImGuiKey.PageUp;
            case SDL_Keycode.SDLK_PAGEDOWN: return ImGuiKey.PageDown;
            case SDL_Keycode.SDLK_HOME: return ImGuiKey.Home;
            case SDL_Keycode.SDLK_END: return ImGuiKey.End;
            case SDL_Keycode.SDLK_INSERT: return ImGuiKey.Insert;
            case SDL_Keycode.SDLK_DELETE: return ImGuiKey.Delete;
            case SDL_Keycode.SDLK_BACKSPACE: return ImGuiKey.Backspace;
            case SDL_Keycode.SDLK_SPACE: return ImGuiKey.Space;
            case SDL_Keycode.SDLK_RETURN: return ImGuiKey.Enter;
            case SDL_Keycode.SDLK_ESCAPE: return ImGuiKey.Escape;
            case SDL_Keycode.SDLK_QUOTE: return ImGuiKey.Apostrophe;
            case SDL_Keycode.SDLK_COMMA: return ImGuiKey.Comma;
            case SDL_Keycode.SDLK_MINUS: return ImGuiKey.Minus;
            case SDL_Keycode.SDLK_PERIOD: return ImGuiKey.Period;
            case SDL_Keycode.SDLK_SLASH: return ImGuiKey.Slash;
            case SDL_Keycode.SDLK_SEMICOLON: return ImGuiKey.Semicolon;
            case SDL_Keycode.SDLK_EQUALS: return ImGuiKey.Equal;
            case SDL_Keycode.SDLK_LEFTBRACKET: return ImGuiKey.LeftBracket;
            case SDL_Keycode.SDLK_BACKSLASH: return ImGuiKey.Backslash;
            case SDL_Keycode.SDLK_RIGHTBRACKET: return ImGuiKey.RightBracket;
            case SDL_Keycode.SDLK_BACKQUOTE: return ImGuiKey.GraveAccent;
            case SDL_Keycode.SDLK_CAPSLOCK: return ImGuiKey.CapsLock;
            case SDL_Keycode.SDLK_SCROLLLOCK: return ImGuiKey.ScrollLock;
            case SDL_Keycode.SDLK_NUMLOCKCLEAR: return ImGuiKey.NumLock;
            case SDL_Keycode.SDLK_PRINTSCREEN: return ImGuiKey.PrintScreen;
            case SDL_Keycode.SDLK_PAUSE: return ImGuiKey.Pause;
            case SDL_Keycode.SDLK_KP_0: return ImGuiKey.Keypad0;
            case SDL_Keycode.SDLK_KP_1: return ImGuiKey.Keypad1;
            case SDL_Keycode.SDLK_KP_2: return ImGuiKey.Keypad2;
            case SDL_Keycode.SDLK_KP_3: return ImGuiKey.Keypad3;
            case SDL_Keycode.SDLK_KP_4: return ImGuiKey.Keypad4;
            case SDL_Keycode.SDLK_KP_5: return ImGuiKey.Keypad5;
            case SDL_Keycode.SDLK_KP_6: return ImGuiKey.Keypad6;
            case SDL_Keycode.SDLK_KP_7: return ImGuiKey.Keypad7;
            case SDL_Keycode.SDLK_KP_8: return ImGuiKey.Keypad8;
            case SDL_Keycode.SDLK_KP_9: return ImGuiKey.Keypad9;
            case SDL_Keycode.SDLK_KP_PERIOD: return ImGuiKey.KeypadDecimal;
            case SDL_Keycode.SDLK_KP_DIVIDE: return ImGuiKey.KeypadDivide;
            case SDL_Keycode.SDLK_KP_MULTIPLY: return ImGuiKey.KeypadMultiply;
            case SDL_Keycode.SDLK_KP_MINUS: return ImGuiKey.KeypadSubtract;
            case SDL_Keycode.SDLK_KP_PLUS: return ImGuiKey.KeypadAdd;
            case SDL_Keycode.SDLK_KP_ENTER: return ImGuiKey.KeypadEnter;
            case SDL_Keycode.SDLK_KP_EQUALS: return ImGuiKey.KeypadEqual;
            case SDL_Keycode.SDLK_LCTRL: return ImGuiKey.LeftCtrl;
            case SDL_Keycode.SDLK_LSHIFT: return ImGuiKey.LeftShift;
            case SDL_Keycode.SDLK_LALT: return ImGuiKey.LeftAlt;
            case SDL_Keycode.SDLK_LGUI: return ImGuiKey.LeftSuper;
            case SDL_Keycode.SDLK_RCTRL: return ImGuiKey.RightCtrl;
            case SDL_Keycode.SDLK_RSHIFT: return ImGuiKey.RightShift;
            case SDL_Keycode.SDLK_RALT: return ImGuiKey.RightAlt;
            case SDL_Keycode.SDLK_RGUI: return ImGuiKey.RightSuper;
            case SDL_Keycode.SDLK_APPLICATION: return ImGuiKey.Menu;
            case SDL_Keycode.SDLK_0: return ImGuiKey._0;
            case SDL_Keycode.SDLK_1: return ImGuiKey._1;
            case SDL_Keycode.SDLK_2: return ImGuiKey._2;
            case SDL_Keycode.SDLK_3: return ImGuiKey._3;
            case SDL_Keycode.SDLK_4: return ImGuiKey._4;
            case SDL_Keycode.SDLK_5: return ImGuiKey._5;
            case SDL_Keycode.SDLK_6: return ImGuiKey._6;
            case SDL_Keycode.SDLK_7: return ImGuiKey._7;
            case SDL_Keycode.SDLK_8: return ImGuiKey._8;
            case SDL_Keycode.SDLK_9: return ImGuiKey._9;
            case SDL_Keycode.SDLK_a: return ImGuiKey.A;
            case SDL_Keycode.SDLK_b: return ImGuiKey.B;
            case SDL_Keycode.SDLK_c: return ImGuiKey.C;
            case SDL_Keycode.SDLK_d: return ImGuiKey.D;
            case SDL_Keycode.SDLK_e: return ImGuiKey.E;
            case SDL_Keycode.SDLK_f: return ImGuiKey.F;
            case SDL_Keycode.SDLK_g: return ImGuiKey.G;
            case SDL_Keycode.SDLK_h: return ImGuiKey.H;
            case SDL_Keycode.SDLK_i: return ImGuiKey.I;
            case SDL_Keycode.SDLK_j: return ImGuiKey.J;
            case SDL_Keycode.SDLK_k: return ImGuiKey.K;
            case SDL_Keycode.SDLK_l: return ImGuiKey.L;
            case SDL_Keycode.SDLK_m: return ImGuiKey.M;
            case SDL_Keycode.SDLK_n: return ImGuiKey.N;
            case SDL_Keycode.SDLK_o: return ImGuiKey.O;
            case SDL_Keycode.SDLK_p: return ImGuiKey.P;
            case SDL_Keycode.SDLK_q: return ImGuiKey.Q;
            case SDL_Keycode.SDLK_r: return ImGuiKey.R;
            case SDL_Keycode.SDLK_s: return ImGuiKey.S;
            case SDL_Keycode.SDLK_t: return ImGuiKey.T;
            case SDL_Keycode.SDLK_u: return ImGuiKey.U;
            case SDL_Keycode.SDLK_v: return ImGuiKey.V;
            case SDL_Keycode.SDLK_w: return ImGuiKey.W;
            case SDL_Keycode.SDLK_x: return ImGuiKey.X;
            case SDL_Keycode.SDLK_y: return ImGuiKey.Y;
            case SDL_Keycode.SDLK_z: return ImGuiKey.Z;
            case SDL_Keycode.SDLK_F1: return ImGuiKey.F1;
            case SDL_Keycode.SDLK_F2: return ImGuiKey.F2;
            case SDL_Keycode.SDLK_F3: return ImGuiKey.F3;
            case SDL_Keycode.SDLK_F4: return ImGuiKey.F4;
            case SDL_Keycode.SDLK_F5: return ImGuiKey.F5;
            case SDL_Keycode.SDLK_F6: return ImGuiKey.F6;
            case SDL_Keycode.SDLK_F7: return ImGuiKey.F7;
            case SDL_Keycode.SDLK_F8: return ImGuiKey.F8;
            case SDL_Keycode.SDLK_F9: return ImGuiKey.F9;
            case SDL_Keycode.SDLK_F10: return ImGuiKey.F10;
            case SDL_Keycode.SDLK_F11: return ImGuiKey.F11;
            case SDL_Keycode.SDLK_F12: return ImGuiKey.F12;
        }
        return ImGuiKey.None;
    }

    private bool SDL2NewFrame()
    {
        var io = ImGui.GetIO();
        int w, h, dispW, dispH;
        SDL_GetWindowSize(win, out w, out h);
        // i think
        if (((SDL_WindowFlags)SDL_GetWindowFlags(win) & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
            w = h = 0;
        SDL_GetRendererOutputSize(ren, out dispW, out dispH);
        io.DisplaySize = new((float)w, (float)h);
        if (w > 0 && h > 0)
            io.DisplayFramebufferScale = new((float)dispW / w, (float)dispH / h);

        var currentTime = SDL_GetPerformanceCounter();
        io.DeltaTime = time > 0 ? (float)((double)(currentTime - time) / frequency) : (float)(1.0f / 60.0f);
        time = currentTime;
        var amountButtonsDown = mouseButtons.Count(x => x.Value) != 0;
        if (pendingMouseLeaveFrame != 0 && pendingMouseLeaveFrame >= ImGui.GetFrameCount())
        {
            io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
            pendingMouseLeaveFrame = 0;
        }

        UpdateMouseData();
        UpdateMouseCursor();

        return true;
    }

    private unsafe void UpdateMouseData()
    {
        var io = ImGui.GetIO();
        var areAnyButtonsDown = mouseButtons.Count(x => x.Value) != 0;
        SDL_CaptureMouse((areAnyButtonsDown && (nint)ImGui.GetDragDropPayload().NativePtr == nint.Zero) ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);
        var focused_window = SDL_GetKeyboardFocus();
        bool is_app_focused = win == focused_window;

        if (is_app_focused && io.WantSetMousePos)
            SDL_WarpMouseInWindow(win, (int)io.MousePos.X, (int)io.MousePos.Y);


    }

    private void UpdateMouseCursor()
    {
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
            return;

        ImGuiMouseCursor imguiCursor = ImGui.GetMouseCursor();
        if (io.MouseDrawCursor || imguiCursor == ImGuiMouseCursor.None)
            SDL_ShowCursor(0);
        else
        {
            // Show OS mouse cursor
            SDL_SetCursor(mouseCursors[imguiCursor] != IntPtr.Zero ? mouseCursors[imguiCursor] : mouseCursors[ImGuiMouseCursor.Arrow]);
            SDL_ShowCursor(1);
        }
    }
}