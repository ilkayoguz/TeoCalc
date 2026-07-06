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
  private enum CalcAppScreen
  {
    Launcher,
    Explorer,
  }

  public static int Run()
  {
    try
    {
      return RunCore();
    }
    catch (Exception exception)
    {
      FatalErrorDialog.Show(exception);
      return 1;
    }
  }

  private static int RunCore()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    CalcExplorerSession session = new(engineRoot);
    CalculatorLauncherModel launcher = CalculatorLauncherModel.CreateDefault();
    CalcAppScreen screen = CalcAppScreen.Launcher;

    WindowOptions options = WindowOptions.Default;
    options.Title = "TeoCalc";
    options.Size = new Vector2D<int>(1440, 900);
    options.VSync = true;

    using IWindow window = Window.Create(options);
    GL? gl = null;
    IInputContext? input = null;
    ImGuiController? controller = null;

    window.Load += () =>
    {
      try
      {
        gl = window.CreateOpenGL();
        input = window.CreateInput();
        controller = new ImGuiController(gl, window, input, onConfigureIO: CalcFaceplateFonts.Configure);
        Hp65FaceplateSvgAssets.TryInitialize(gl);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Startup Error");
        window.Close();
      }
    };

    double lastFrameTime = 0d;
    window.Update += _ =>
    {
      double time = window.Time;
      float delta = lastFrameTime > 0d ? (float)(time - lastFrameTime) : 0.016f;
      lastFrameTime = time;
      if (screen == CalcAppScreen.Explorer)
      {
        session.Tick(delta);
      }

      controller?.Update(delta);
    };

    window.Render += _ =>
    {
      if (gl is null || controller is null)
      {
        return;
      }

      try
      {
        gl.ClearColor(0.12f, 0.12f, 0.14f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        controller.MakeCurrent();
        if (screen == CalcAppScreen.Launcher)
        {
          if (CalculatorLauncherView.Draw(launcher))
          {
            OpenExplorer(session, launcher);
            screen = CalcAppScreen.Explorer;
          }
        }
        else
        {
          CalcExplorerView.Draw(session, () => screen = CalcAppScreen.Launcher);
        }

        controller.Render();
        CalcFaceplatePointer.ApplyPendingCursor(input);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Render Error");
        window.Close();
      }
    };

    window.FramebufferResize += size =>
    {
      gl?.Viewport(size);
    };

    window.Closing += () =>
    {
      controller?.Dispose();
      controller = null;
      input?.Dispose();
      input = null;
      Hp65FaceplateSvgAssets.Dispose();
      gl?.Dispose();
      gl = null;
    };

    window.Run();
    return 0;
  }

  private static void OpenExplorer(CalcExplorerSession session, CalculatorLauncherModel launcher)
  {
    if (launcher.SelectedEntry is not { } entry)
    {
      return;
    }

    int modelIndex = Array.FindIndex(
      session.Models,
      modelId => string.Equals(modelId, entry.TeoCalcModelId, StringComparison.OrdinalIgnoreCase));
    if (modelIndex >= 0 && !string.Equals(session.Model.Model, entry.TeoCalcModelId, StringComparison.OrdinalIgnoreCase))
    {
      session.LoadModel(modelIndex);
    }
  }
}
