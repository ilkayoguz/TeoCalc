using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TeoCalc.Core;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// Single-process shell: launcher window + zero-or-more in-process calculator windows.
/// </summary>
public static class CalcExplorerApp
{
  private static readonly List<CalcFaceplateHost> OpenCalculators = [];

  private static readonly Queue<string> PendingOpens = new();

  private static IWindow? _launcher;

  public static int Run(string[]? args = null)
  {
    try
    {
      if (TryParseModelArg(args, out string modelId))
      {
        PendingOpens.Enqueue(modelId);
      }

      return RunHost();
    }
    catch (Exception exception)
    {
      FatalErrorDialog.Show(exception);
      return 1;
    }
  }

  /// <summary>Queue a calculator window open (processed between frames — never during Render).</summary>
  public static bool TryOpenModelWindow(string modelId, out string status)
  {
    if (_launcher is null)
    {
      status = "Launcher is not ready.";
      return false;
    }

    if (string.IsNullOrWhiteSpace(modelId))
    {
      status = "Model id is empty.";
      return false;
    }

    PendingOpens.Enqueue(modelId.Trim());
    status = $"Opening {modelId}…";
    return true;
  }

  private static void DrainPendingOpens()
  {
    if (_launcher is null)
    {
      return;
    }

    while (PendingOpens.Count > 0)
    {
      string modelId = PendingOpens.Dequeue();
      try
      {
        CalcFaceplateHost? host = CalcFaceplateHost.TryCreate(modelId, _launcher.GLContext, out string status);
        if (host is null)
        {
          FatalErrorDialog.Show(new InvalidOperationException(status), "TeoCalc");
          continue;
        }

        host.Initialize();
        OpenCalculators.Add(host);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Open Model");
      }
    }
  }

  private static bool TryParseModelArg(string[]? args, out string modelId)
  {
    modelId = string.Empty;
    if (args is null || args.Length == 0)
    {
      return false;
    }

    for (int i = 0; i < args.Length; i++)
    {
      string arg = args[i];
      if (arg.Equals("--model", StringComparison.OrdinalIgnoreCase)
          || arg.Equals("-m", StringComparison.OrdinalIgnoreCase))
      {
        if (i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
        {
          modelId = args[i + 1].Trim().Trim('"');
          return modelId.Length > 0;
        }

        return false;
      }

      if (arg.StartsWith("--model=", StringComparison.OrdinalIgnoreCase))
      {
        modelId = arg["--model=".Length..].Trim().Trim('"');
        return modelId.Length > 0;
      }
    }

    return false;
  }

  private static int RunHost()
  {
    CalcFaceplateThemeState.ApplyForModel(CalcModelCatalog.Hp65);
    CalculatorLauncherModel launcherModel = CalculatorLauncherModel.CreateDefault();

    WindowOptions options = WindowOptions.Default;
    options.Title = "TeoCalc";
    options.Size = new Vector2D<int>(460, 320);
    options.VSync = true;

    _launcher = Silk.NET.Windowing.Window.Create(options);
    GL? gl = null;
    IInputContext? input = null;
    ImGuiController? controller = null;
    double lastFrameTime = 0d;

    _launcher.Load += () =>
    {
      try
      {
        gl = _launcher.CreateOpenGL();
        input = _launcher.CreateInput();
        controller = new ImGuiController(gl, _launcher, input, onConfigureIO: CalcFaceplateFonts.Configure);
        Hp65FaceplateSvgAssets.TryInitialize(gl);
        CalcModernSvgAssets.TryInitialize(gl);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Startup Error");
        _launcher.Close();
      }
    };

    _launcher.Update += _ =>
    {
      double time = _launcher.Time;
      float delta = lastFrameTime > 0d ? (float)(time - lastFrameTime) : 0.016f;
      lastFrameTime = time;
      _launcher.MakeCurrent();
      controller?.Update(delta);
    };

    _launcher.Render += _ =>
    {
      if (gl is null || controller is null)
      {
        return;
      }

      try
      {
        _launcher.MakeCurrent();
        gl.Viewport(_launcher.FramebufferSize);
        gl.ClearColor(0.12f, 0.12f, 0.14f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        controller.MakeCurrent();
        CalculatorLauncherView.Draw(launcherModel);
        controller.Render();
        CalcFaceplatePointer.ApplyPendingCursor(input);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Render Error");
        _launcher.Close();
      }
    };

    _launcher.FramebufferResize += size => gl?.Viewport(size);

    _launcher.Closing += () =>
    {
      foreach (CalcFaceplateHost host in OpenCalculators.ToArray())
      {
        host.Dispose();
      }

      OpenCalculators.Clear();
      if (controller is not null)
      {
        controller.MakeCurrent();
        CalcFaceplateFonts.UnregisterCurrentContext();
      }

      controller?.Dispose();
      controller = null;
      input?.Dispose();
      input = null;
      Hp65FaceplateSvgAssets.Dispose();
      CalcModernSvgAssets.Dispose();
      gl?.Dispose();
      gl = null;
    };

    _launcher.Initialize();

    while (!_launcher.IsClosing)
    {
      _launcher.DoEvents();
      DrainPendingOpens();

      for (int i = OpenCalculators.Count - 1; i >= 0; i--)
      {
        if (OpenCalculators[i].IsClosing)
        {
          OpenCalculators[i].Dispose();
          OpenCalculators.RemoveAt(i);
        }
      }

      if (_launcher.IsClosing)
      {
        break;
      }

      _launcher.DoUpdate();
      _launcher.DoRender();

      foreach (CalcFaceplateHost host in OpenCalculators)
      {
        if (!host.IsClosing)
        {
          host.PumpUpdate();
          host.PumpRender();
        }
      }
    }

    foreach (CalcFaceplateHost host in OpenCalculators.ToArray())
    {
      host.Dispose();
    }

    OpenCalculators.Clear();
    _launcher.DoEvents();
    _launcher.Reset();
    _launcher.Dispose();
    _launcher = null;
    return 0;
  }
}
