using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

public static class CalcExplorerApp
{
  public static int Run()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    CalcExplorerSession session = new(engineRoot);

    WindowOptions options = WindowOptions.Default;
    options.Title = "TeoCalc Explorer";
    options.Size = new Vector2D<int>(1280, 800);
    options.VSync = true;

    using IWindow window = Window.Create(options);
    GL? gl = null;
    IInputContext? input = null;
    ImGuiController? controller = null;

    window.Load += () =>
    {
      gl = window.CreateOpenGL();
      input = window.CreateInput();
      controller = new ImGuiController(gl, window, input, onConfigureIO: CalcFaceplateFonts.Configure);
      Hp65FaceplateSvgAssets.TryInitialize(gl);
    };

    window.Update += _ =>
    {
      controller?.Update((float)window.Time);
    };

    window.Render += _ =>
    {
      if (gl is null || controller is null)
      {
        return;
      }

      gl.ClearColor(0.12f, 0.12f, 0.14f, 1f);
      gl.Clear(ClearBufferMask.ColorBufferBit);
      controller.MakeCurrent();
      CalcExplorerView.Draw(session);
      controller.Render();
      CalcFaceplatePointer.ApplyPendingCursor(input);
    };

    window.FramebufferResize += size =>
    {
      gl?.Viewport(size);
    };

    window.Run();
    Hp65FaceplateSvgAssets.Dispose();
    controller?.Dispose();
    input?.Dispose();
    gl?.Dispose();
    return 0;
  }
}
