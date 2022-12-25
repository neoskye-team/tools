using ImGuiNET;

using System.Numerics;
using System.Runtime.InteropServices;

using static SDL2.SDL;

public partial class ImGUIRenderer
{
    private IntPtr fontTexture;

    private bool Init()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        return true;
    }

    private bool RendererNewFrame()
    {
        if (fontTexture == IntPtr.Zero)
            CreateDeviceObjects();
        return true;
    }

    private unsafe bool CreateDeviceObjects()
    {
        var io = ImGui.GetIO();
        byte* pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);
        fontTexture = SDL_CreateTexture(ren, SDL_PIXELFORMAT_ABGR8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, width, height);
        if (fontTexture == IntPtr.Zero)
        {
            SDL_Log("error creating texture");
            return false;
        }
        SDL_UpdateTexture(fontTexture, IntPtr.Zero, (nint)pixels, 4 * width);
        SDL_SetTextureBlendMode(fontTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetTextureScaleMode(fontTexture, SDL_ScaleMode.SDL_ScaleModeLinear);
        io.Fonts.SetTexID(fontTexture);
        return true;
    }

    private unsafe bool RenderDrawData()
    {
        var code = 0;
        // Console.WriteLine("Entered RenderDrawData");
        var drawData = ImGui.GetDrawData();
        float rsx = 1.0f;
        float rsy = 1.0f;
        SDL_RenderGetScale(ren, out rsx, out rsy);
        Vector2 renderScale = new(
            (rsx == 1.0f) ? drawData.FramebufferScale.X : 1.0f,
            (rsy == 1.0f) ? drawData.FramebufferScale.Y : 1.0f
        );

        int fb_width = (int)(drawData.DisplaySize.X * renderScale.X);
        int fb_height = (int)(drawData.DisplaySize.Y * renderScale.Y);
        if (fb_width == 0 || fb_height == 0)
            return false;

        SDL_Rect oldViewport, oldClipRect;
        bool clipEnabled = SDL_RenderIsClipEnabled(ren) == SDL_bool.SDL_TRUE;
        code = SDL_RenderGetViewport(ren, out oldViewport);
        CheckSDLError(code);
        SDL_RenderGetClipRect(ren, out oldClipRect);

        Vector2 clip_off = drawData.DisplayPos;         // (0,0) unless using multi-viewports
        Vector2 clip_scale = renderScale;

        SetupRenderState();

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            // Console.WriteLine("Drawing cmdlists");
            var cmdList = drawData.CmdListsRange[i];
            var vtxBuffer = cmdList.VtxBuffer.Data;
            var idxBuffer = cmdList.IdxBuffer.Data;
            // Console.WriteLine("Buffers loaded");
            for (var cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                // Console.WriteLine("Loaded cmd");
                var pcmd = cmdList.CmdBuffer[cmd_i];

                if (pcmd.UserCallback != IntPtr.Zero)
                    Console.WriteLine("UserCallback not implemented");
                var clipRect = pcmd.ClipRect;
                Vector2 clip_min = new(
                    (pcmd.ClipRect.X - clip_off.X) * clip_scale.X,
                    (pcmd.ClipRect.Y - clip_off.Y) * clip_scale.Y
                );
                Vector2 clip_max = new(
                    (pcmd.ClipRect.Z - clip_off.X) * clip_scale.X,
                    (pcmd.ClipRect.W - clip_off.Y) * clip_scale.Y
                );
                if (clip_min.X < 0.0f) clip_min.X = 0.0f;
                if (clip_min.Y < 0.0f) clip_min.Y = 0.0f;
                if (clip_max.X > (float)fb_width) clip_max.X = (float)fb_width;
                if (clip_max.Y > (float)fb_height) clip_max.Y = (float)fb_height;
                if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
                    continue;
                SDL_Rect r;
                r.x = (int)clip_min.X;
                r.y = (int)clip_min.Y;
                r.w = (int)(clip_max.X - clip_min.Y);
                r.h = (int)(clip_max.Y - clip_min.Y);
                SDL_RenderSetClipRect(ren, ref r);
                // Console.WriteLine("Clipping calculated");
                var offsetOfPos = Marshal.OffsetOf(typeof(ImDrawVert), "pos");
                var offsetOfUv = Marshal.OffsetOf(typeof(ImDrawVert), "uv");
                var offsetOfCol = Marshal.OffsetOf(typeof(ImDrawVert), "col");
                var xy = (float*)(void*)((char*)(vtxBuffer + pcmd.VtxOffset) + offsetOfPos);
                var uv = (float*)(void*)((char*)(vtxBuffer + pcmd.VtxOffset) + offsetOfUv);
                var col = (int*)(void*)((char*)(vtxBuffer + pcmd.VtxOffset) + offsetOfCol);
                // Console.WriteLine("Got xy/uv/color");


                var tex = pcmd.GetTexID();

                // uv breaks here (and probably other things)
                code = SDL_RenderGeometryRaw(ren, tex,
                    xy, sizeof(ImDrawVert),
                    col, sizeof(ImDrawVert),
                    uv, sizeof(ImDrawVert),
                    (int)(cmdList.VtxBuffer.Size - pcmd.VtxOffset),
                    (IntPtr)(idxBuffer + pcmd.IdxOffset), (int)pcmd.ElemCount, sizeof(ushort)
                );
                CheckSDLError(code);
                // Console.WriteLine("Rendered geometry");
            }
        }

        return true;
    }

    private bool SetupRenderState()
    {
        int w, h;
        SDL_GetRendererOutputSize(ren, out w, out h);
        SDL_Rect wholeScreen;
        wholeScreen.x = 0;
        wholeScreen.y = 0;
        wholeScreen.w = w;
        wholeScreen.h = h;
        SDL_RenderSetViewport(ren, ref wholeScreen);
        SDL_RenderSetClipRect(ren, IntPtr.Zero);
        return true;
    }

    private void CheckSDLError(int status, string msg = "SDL Error")
    {
        if (status != 0)
            Console.WriteLine($"{msg}: {SDL_GetError()}");
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static unsafe extern int SDL_RenderGeometryRaw(
        IntPtr renderer, IntPtr texture,
        [In] float* xy, int xy_stride,
        [In] int* color, int color_stride,
        [In] float* uv, int uv_stride,
        int num_vertices,
        IntPtr indices, int num_indices, int size_indices
    );
}