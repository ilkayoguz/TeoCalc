using System.Numerics;
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
/// Single-process shell: frameless launcher window + zero-or-more in-process calculator windows.
/// </summary>
public static class CalcExplorerApp
{
  private const float DragThreshold = 4f;
  private const float ResizeEdge = 8f;
  private const float ResizeCorner = 16f;
  private const int MinLauncherWidth = 360;
  private const int MinLauncherHeight = 280;

  private static readonly List<CalcFaceplateHost> OpenCalculators = [];

  private static readonly Queue<string> PendingOpens = new();

  private static IWindow? _launcher;

  private static bool _draggingWindow;

  private static bool _dragMoved;

  private static bool _resizingWindow;

  private static ResizeZone _resizeZone;

  private static int _dragStartCursorX;

  private static int _dragStartCursorY;

  private static Vector2D<int> _dragStartWindowPos;

  private static Vector2D<int> _dragStartWindowSize;

  private enum ResizeZone
  {
    None,
    N,
    S,
    E,
    W,
    NE,
    NW,
    SE,
    SW,
  }

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
    Vector2 launcherSize = CalculatorLauncherView.PreferredWindowSize(launcherModel.Entries.Count);

    WindowOptions options = WindowOptions.Default;
    options.Title = "TeoCalc";
    options.Size = new Vector2D<int>(
      Math.Max(320, (int)MathF.Round(launcherSize.X)),
      Math.Max(240, (int)MathF.Round(launcherSize.Y)));
    options.VSync = true;
    options.WindowBorder = WindowBorder.Hidden;
    options.TransparentFramebuffer = true;

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
        CalcFramelessShell.ApplyRoundedCorners(_launcher.Handle);
        Hp65FaceplateSvgAssets.TryInitialize(gl);
        CalcModernSvgAssets.TryInitialize(gl);
        CalculatorLauncherThumbnail.Initialize(gl);
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
      if (gl is null || controller is null || _launcher.IsClosing)
      {
        return;
      }

      try
      {
        _launcher.MakeCurrent();
        gl.Viewport(_launcher.FramebufferSize);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.ClearColor(0f, 0f, 0f, 0f);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        controller.MakeCurrent();

        CalcFaceplatePointer.BeginFrame();
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x00000000u);
        ImGui.Begin(
          "##teo-launcher-host",
          ImGuiWindowFlags.NoDecoration
          | ImGuiWindowFlags.NoMove
          | ImGuiWindowFlags.NoBringToFrontOnFocus
          | ImGuiWindowFlags.NoScrollbar
          | ImGuiWindowFlags.NoScrollWithMouse
          | ImGuiWindowFlags.NoBackground);

        Vector2 display = ImGui.GetIO().DisplaySize;
        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        CalcFramelessShell.RectF content = CalcFramelessShell.DrawChrome(draw, display, "TeoCalc");

        CalcWindowTitlePanelComponent.TitleAction titleAction =
          CalcWindowTitlePanelComponent.Draw(
            _launcher.WindowState == WindowState.Maximized,
            CalcFramelessShell.BeadInset,
            CalcFramelessShell.TopBandHeight,
            display.X - CalcFramelessShell.BeadInset);

        CalculatorLauncherView.DrawContent(launcherModel, content);
        HandleLauncherTitleAction(titleAction);
        if (!_launcher.IsClosing)
        {
          HandleLauncherFramelessChrome();
        }

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        if (!_launcher.IsClosing)
        {
          controller.Render();
          CalculatorLauncherThumbnail.BakePending(gl, _launcher.FramebufferSize);
          CalcFaceplatePointer.ApplyPendingCursor(input);
        }
      }
      catch (Exception exception)
      {
        if (_launcher.IsClosing)
        {
          return;
        }

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
      CalculatorLauncherThumbnail.Dispose();
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

  private static void HandleLauncherTitleAction(CalcWindowTitlePanelComponent.TitleAction action)
  {
    if (_launcher is null || _launcher.IsClosing)
    {
      return;
    }

    switch (action)
    {
      case CalcWindowTitlePanelComponent.TitleAction.Minimize:
        _launcher.WindowState = WindowState.Minimized;
        break;
      case CalcWindowTitlePanelComponent.TitleAction.ToggleMaximize:
        _launcher.WindowState = _launcher.WindowState == WindowState.Maximized
          ? WindowState.Normal
          : WindowState.Maximized;
        break;
      case CalcWindowTitlePanelComponent.TitleAction.Close:
        _launcher.Close();
        break;
    }
  }

  private static void HandleLauncherFramelessChrome()
  {
    if (_launcher is null || _launcher.IsClosing)
    {
      return;
    }

    Vector2 display = ImGui.GetIO().DisplaySize;
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool overButtons = CalcWindowTitlePanelComponent.IsOverButtons(
      mouse,
      CalcFramelessShell.BeadInset,
      CalcFramelessShell.TopBandHeight,
      display.X - CalcFramelessShell.BeadInset);
    bool onTitleStrip = mouse.Y >= 0f && mouse.Y <= CalcFramelessShell.BandTop;

    if (!_draggingWindow && !_resizingWindow && !overButtons && onTitleStrip
        && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
    {
      _launcher.WindowState = _launcher.WindowState == WindowState.Maximized
        ? WindowState.Normal
        : WindowState.Maximized;
      return;
    }

    if (_resizingWindow)
    {
      UpdateLauncherResize();
      return;
    }

    if (_draggingWindow)
    {
      UpdateLauncherDrag();
      return;
    }

    if (overButtons)
    {
      return;
    }

    ResizeZone zone = HitTestLauncherResize(mouse, display);
    if (zone != ResizeZone.None)
    {
      CalcFaceplatePointer.RequestCursor(CursorForZone(zone));
      if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
          && CalcFramelessShell.TryGetCursorPos(out int resizeX, out int resizeY))
      {
        _resizingWindow = true;
        _resizeZone = zone;
        _dragStartCursorX = resizeX;
        _dragStartCursorY = resizeY;
        _dragStartWindowPos = _launcher.Position;
        _dragStartWindowSize = _launcher.Size;
        if (_launcher.WindowState == WindowState.Maximized)
        {
          _launcher.WindowState = WindowState.Normal;
          _dragStartWindowPos = _launcher.Position;
          _dragStartWindowSize = _launcher.Size;
        }
      }

      return;
    }

    if (!onTitleStrip)
    {
      return;
    }

    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
        && CalcFramelessShell.TryGetCursorPos(out int pressX, out int pressY))
    {
      _draggingWindow = true;
      _dragMoved = false;
      _dragStartCursorX = pressX;
      _dragStartCursorY = pressY;
      _dragStartWindowPos = _launcher.Position;
    }

    CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
  }

  private static void UpdateLauncherDrag()
  {
    if (_launcher is null
        || !ImGui.IsMouseDown(ImGuiMouseButton.Left)
        || !CalcFramelessShell.TryGetCursorPos(out int cursorX, out int cursorY))
    {
      _draggingWindow = false;
      _dragMoved = false;
      return;
    }

    int dx = cursorX - _dragStartCursorX;
    int dy = cursorY - _dragStartCursorY;
    if (!_dragMoved)
    {
      if (dx * dx + dy * dy < (int)(DragThreshold * DragThreshold))
      {
        CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
        return;
      }

      _dragMoved = true;
      if (_launcher.WindowState == WindowState.Maximized)
      {
        _launcher.WindowState = WindowState.Normal;
        _dragStartWindowPos = _launcher.Position;
      }
    }

    _launcher.Position = new Vector2D<int>(_dragStartWindowPos.X + dx, _dragStartWindowPos.Y + dy);
    CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
  }

  private static void UpdateLauncherResize()
  {
    if (_launcher is null
        || !ImGui.IsMouseDown(ImGuiMouseButton.Left)
        || !CalcFramelessShell.TryGetCursorPos(out int cursorX, out int cursorY))
    {
      _resizingWindow = false;
      _resizeZone = ResizeZone.None;
      return;
    }

    int dx = cursorX - _dragStartCursorX;
    int dy = cursorY - _dragStartCursorY;
    int left = _dragStartWindowPos.X;
    int top = _dragStartWindowPos.Y;
    int right = left + _dragStartWindowSize.X;
    int bottom = top + _dragStartWindowSize.Y;

    bool moveLeft = _resizeZone is ResizeZone.W or ResizeZone.NW or ResizeZone.SW;
    bool moveRight = _resizeZone is ResizeZone.E or ResizeZone.NE or ResizeZone.SE;
    bool moveTop = _resizeZone is ResizeZone.N or ResizeZone.NW or ResizeZone.NE;
    bool moveBottom = _resizeZone is ResizeZone.S or ResizeZone.SW or ResizeZone.SE;

    if (moveLeft)
    {
      left += dx;
    }

    if (moveRight)
    {
      right += dx;
    }

    if (moveTop)
    {
      top += dy;
    }

    if (moveBottom)
    {
      bottom += dy;
    }

    int width = Math.Max(MinLauncherWidth, right - left);
    int height = Math.Max(MinLauncherHeight, bottom - top);
    if (moveLeft && !moveRight)
    {
      left = right - width;
    }

    if (moveTop && !moveBottom)
    {
      top = bottom - height;
    }

    _launcher.Position = new Vector2D<int>(left, top);
    _launcher.Size = new Vector2D<int>(width, height);
    CalcFaceplatePointer.RequestCursor(CursorForZone(_resizeZone));
  }

  private static ResizeZone HitTestLauncherResize(Vector2 mouse, Vector2 display)
  {
    bool left = mouse.X >= 0f && mouse.X <= ResizeCorner;
    bool right = mouse.X >= display.X - ResizeCorner && mouse.X <= display.X;
    bool top = mouse.Y >= 0f && mouse.Y <= ResizeCorner;
    bool bottom = mouse.Y >= display.Y - ResizeCorner && mouse.Y <= display.Y;
    bool nearLeft = mouse.X >= 0f && mouse.X <= ResizeEdge;
    bool nearRight = mouse.X >= display.X - ResizeEdge && mouse.X <= display.X;
    bool nearTop = mouse.Y >= 0f && mouse.Y <= ResizeEdge;
    bool nearBottom = mouse.Y >= display.Y - ResizeEdge && mouse.Y <= display.Y;

    if (top && left)
    {
      return ResizeZone.NW;
    }

    if (top && right)
    {
      return ResizeZone.NE;
    }

    if (bottom && left)
    {
      return ResizeZone.SW;
    }

    if (bottom && right)
    {
      return ResizeZone.SE;
    }

    if (nearTop)
    {
      return ResizeZone.N;
    }

    if (nearBottom)
    {
      return ResizeZone.S;
    }

    if (nearLeft)
    {
      return ResizeZone.W;
    }

    if (nearRight)
    {
      return ResizeZone.E;
    }

    return ResizeZone.None;
  }

  private static StandardCursor CursorForZone(ResizeZone zone) => zone switch
  {
    ResizeZone.N or ResizeZone.S => StandardCursor.VResize,
    ResizeZone.E or ResizeZone.W => StandardCursor.HResize,
    ResizeZone.NE or ResizeZone.SW => StandardCursor.NeswResize,
    ResizeZone.NW or ResizeZone.SE => StandardCursor.NwseResize,
    _ => StandardCursor.Default,
  };
}
