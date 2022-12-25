using ImGuiNET;
using static SDL2.SDL;

public partial class ImGUIRenderer
{
    private IntPtr win;
    private IntPtr ren;

    public ImGUIRenderer(IntPtr win, IntPtr ren)
    {
        mouseButtons = new();
        mouseCursors = new();
        fontTexture = IntPtr.Zero;

        this.win = win;
        this.ren = ren;
        time = 0;
        frequency = SDL_GetPerformanceFrequency();

        mouseButtons.Add(0, false);
        mouseButtons.Add(1, false);
        mouseButtons.Add(2, false);
        mouseButtons.Add(3, false);
        mouseButtons.Add(4, false);

        mouseCursors.Add(ImGuiMouseCursor.Arrow, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW));
        mouseCursors.Add(ImGuiMouseCursor.TextInput, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM));
        mouseCursors.Add(ImGuiMouseCursor.ResizeAll, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL));
        mouseCursors.Add(ImGuiMouseCursor.ResizeNS, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS));
        mouseCursors.Add(ImGuiMouseCursor.ResizeNESW, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW));
        mouseCursors.Add(ImGuiMouseCursor.ResizeNWSE, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE));
        mouseCursors.Add(ImGuiMouseCursor.Hand, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND));
        mouseCursors.Add(ImGuiMouseCursor.NotAllowed, SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO));

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        ImGui.StyleColorsDark();

        InitForSDLRenderer();
        Init();
    }

    public void NewFrame()
    {
        RendererNewFrame();
        SDL2NewFrame();
        ImGui.NewFrame();
    }

    public void Event(ref SDL_Event ev) =>
        HandleEvent(ref ev);

    public void FinishFrame() =>
        ImGui.Render();

    public void Render()
    {
        RenderDrawData();
    }
}