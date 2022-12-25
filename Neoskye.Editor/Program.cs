using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

using ImGuiNET;

namespace Neoskye.Editor;

class Program
{
    public static void Main(string[] args)
    {
        if (SDL_Init(SDL_INIT_VIDEO) != 0)
            Console.WriteLine($"Failed to initialize SDL2: {SDL_GetError()}");
        if (IMG_Init(IMG_InitFlags.IMG_INIT_PNG) == 0)
            Console.WriteLine($"Failed to initialize SDL2_image: {IMG_GetError()}");
        if (TTF_Init() != 0)
            Console.WriteLine($"Failed to initialize SDL2_ttf: {TTF_GetError()}");

        var window = SDL_CreateWindow("Neoskye Editor",
            SDL_WINDOWPOS_CENTERED,
            SDL_WINDOWPOS_CENTERED,
            1280,
            720,
            0
        );

        if (window == IntPtr.Zero)
            Console.WriteLine($"Window failed to initialize: {SDL_GetError()}");

        var renderer = SDL_CreateRenderer(window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        if (renderer == IntPtr.Zero)
            Console.WriteLine($"Failed to create a renderer: {SDL_GetError()}");

        Console.WriteLine("Entering main loop...");

        var imguiRenderer = new ImGUIRenderer(window, renderer);
        // main loop
        var running = true;
        while (running)
        {
            ulong start = SDL_GetPerformanceCounter();
            SDL_Event ev;
            while (SDL_PollEvent(out ev) == 1)
            {
                imguiRenderer.Event(ref ev);

                if (ev.type == SDL_EventType.SDL_QUIT)
                    running = false;
                if (
                    ev.type == SDL_EventType.SDL_WINDOWEVENT &&
                    ev.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE &&
                    ev.window.windowID == SDL_GetWindowID(window)
                )
                    running = false;
            }

            imguiRenderer.NewFrame();

            ImGui.ShowDemoWindow();

            imguiRenderer.FinishFrame();

            SDL_RenderClear(renderer);
            imguiRenderer.Render();
            SDL_RenderPresent(renderer);

            ulong end = SDL_GetPerformanceCounter();
            float elapsedtime = (end - start) / (float)SDL_GetPerformanceFrequency();
            float timing = 1000.0f / 144;
            SDL_Delay((uint)Math.Floor(timing - elapsedtime));
        }

        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        TTF_Quit();
        IMG_Quit();
        SDL_Quit();
    }
}