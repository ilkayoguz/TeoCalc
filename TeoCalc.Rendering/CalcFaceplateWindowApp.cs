using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Game.Explorer;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// In-process calculator faceplate window. Frameless, rounded, aspect-locked resize.
/// </summary>
public sealed class CalcFaceplateHost : IDisposable
{
  private const float DefaultWindowScale = 0.92f;
  private const float ResizeEdge = 8f;
  private const float ResizeCorner = 16f;
  private const float DragThreshold = 4f;
  private const int MinWindowWidth = 280;

  /// <summary>
  /// Shared faceplate content width (px) for initial open. All models use this body width when
  /// they fit in the work area, preserving each model's aspect ratio (height = width / aspect).
  /// Kept modest so tall/narrow models rarely hit the height clamp (which would shrink width).
  /// </summary>
  private const float UnifiedBodyWidth = 500f;

  /// <summary>Outer bead frame thickness in screen px: 2q black + 1q gray + 1q light-gray = 4q.</summary>
  private const float BeadInset = Calc00dWireStyle.FitilWidthRef * 4f;

  /// <summary>Fixed dark-gray top band inside the bead frame: caption button height plus a small margin.</summary>
  private static float TopBandHeight => CalcWindowTitlePanelComponent.Height + 6f;

  /// <summary>Fixed bottom logo band inside the bead frame — mirrors the top title bar height.</summary>
  private static float LogoBandHeight => TopBandHeight;

  /// <summary>Left / right / bottom bead inset around the calc body.</summary>
  private const float BandSide = BeadInset;

  /// <summary>Top inset for content: bead frame plus the fixed dark-gray band.</summary>
  private static float BandTop => BeadInset + TopBandHeight;

  /// <summary>Bottom inset for content: fixed logo band plus the bottom bead inset.</summary>
  private static float BandBottom => LogoBandHeight + BeadInset;

  /// <summary>Total non-body chrome width (left + right bead insets).</summary>
  private static float ChromeWidth => BandSide * 2f;

  /// <summary>Total non-body chrome height (top band + bottom logo band + bottom bead inset).</summary>
  private static float ChromeHeight => BandTop + LogoBandHeight + BeadInset;

  private static int _cascade;

  private readonly CalcExplorerSession _session;

  private readonly CalcExplorerPresenter _explorerPresenter;

  private readonly float _aspect;

  private readonly IWindow _window;

  private readonly string _catalogModelId;

  /// <summary>False when this window shares the launcher GL context — must not dispose GL.</summary>
  private readonly bool _ownsGl;

  private GL? _gl;

  private IInputContext? _input;

  private ImGuiController? _controller;

  private bool _applyingAspect;

  private bool _ignoreNextResize;

  private bool _loaded;

  private double _lastFrameTime;

  private bool _draggingWindow;

  private bool _dragMoved;

  private bool _resizingWindow;

  private ResizeZone _resizeZone;

  private bool _fittedToWorkArea;

  private bool _closeRequested;

  private bool _disposed;

  private Vector2D<int> _restorePosition;

  private Vector2D<int> _restoreSize;

  private POINT _dragStartCursor;

  private Vector2D<int> _dragStartWindowPos;

  private Vector2D<int> _dragStartWindowSize;

  private CalcFaceplateHost(CalcExplorerSession session, float aspect, IWindow window, string catalogModelId, bool ownsGl)
  {
    _session = session;
    _explorerPresenter = new CalcExplorerPresenter(session);
    _aspect = aspect;
    _window = window;
    _catalogModelId = catalogModelId;
    _ownsGl = ownsGl;
    Wire();
  }

  /// <summary>Passive explorer VM kept in sync by <see cref="CalcExplorerPresenter"/>.</summary>
  public CalcExplorerViewModel ExplorerViewModel => _explorerPresenter.ViewModel;

  public IWindow NativeWindow => _window;

  public bool IsClosing => _closeRequested || _window.IsClosing || _disposed;

  public static CalcFaceplateHost? TryCreate(string modelId, IGLContext? sharedContext, out string status)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    CalcExplorerSession session = new(engineRoot);
    if (!TryLoadModel(session, modelId))
    {
      session.Dispose();
      status = $"Model '{modelId}' is not available.";
      return null;
    }

    CalcFaceplateThemeState.ApplyForModel(CalcModelCatalog.Resolve(session.Model, modelId));
    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(session.Model, modelId);
    CalcBodyLayout bodyLayout = CalcBodyLayoutCatalog.ResolveForFaceplate(
      faceplateModel,
      session.Model.Family,
      session.Model.Model);
    float aspect = bodyLayout.ReferenceWidth / MathF.Max(1f, bodyLayout.ReferenceHeight);
    Vector2D<int> initial = InitialSize(aspect);

    WindowOptions options = WindowOptions.Default;
    options.Title = CalcModelIds.ToProductLabel(modelId);
    options.Size = initial;
    options.VSync = true;
    options.WindowBorder = WindowBorder.Hidden;
    options.TransparentFramebuffer = true;
    if (sharedContext is not null)
    {
      options.SharedContext = sharedContext;
    }

    int cascade = _cascade++;
    if (TryGetWorkArea(out int workX, out int workY, out _, out _))
    {
      options.Position = new Vector2D<int>(
        workX + 40 + (cascade % 8) * 28,
        workY + 40 + (cascade % 8) * 28);
    }

    IWindow native = Silk.NET.Windowing.Window.Create(options);
    status = $"Opened {CalcModelIds.ToProductLabel(modelId)}.";
    return new CalcFaceplateHost(session, aspect, native, modelId, ownsGl: sharedContext is null);
  }

  public void Initialize()
  {
    if (_loaded)
    {
      return;
    }

    _window.Initialize();
  }

  public void PumpUpdate()
  {
    if (_window.IsClosing)
    {
      return;
    }

    _window.DoUpdate();
  }

  public void PumpRender()
  {
    if (_window.IsClosing)
    {
      return;
    }

    _window.DoRender();
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
    _closeRequested = true;
    TearDownGraphicsAndSession();
    CloseAndDisposeWindow();
  }

  /// <summary>
  /// Fast path for process exit: dispose session/firmware only; skip ImGui/GL/input Dispose
  /// (GPU teardown stalls exit). Closing handler must not re-run full TearDown afterward.
  /// </summary>
  public void DisposeForAppExit()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
    _closeRequested = true;
    _session.Dispose();
    _controller = null;
    _input = null;
    _gl = null;
    CloseAndDisposeWindow();
  }

  private void CloseAndDisposeWindow()
  {
    if (!_window.IsClosing)
    {
      _window.Close();
    }

    // Avoid Reset()/DoEvents — they pump VSync frames and make app exit feel stalled.
    try
    {
      _window.Dispose();
    }
    catch
    {
      // Window may already be torn down by the platform.
    }
  }

  private void TearDownGraphicsAndSession()
  {
    if (_controller is not null)
    {
      try
      {
        _window.MakeCurrent();
        _controller.MakeCurrent();
        CalcFaceplateFonts.UnregisterCurrentContext();
        _controller.Dispose();
      }
      catch
      {
        // Window/GL may already be tearing down.
      }

      _controller = null;
    }

    _input?.Dispose();
    _input = null;

    // Shared launcher context must stay alive for launcher ImGui/SVG teardown.
    if (_ownsGl)
    {
      _gl?.Dispose();
    }

    _gl = null;
    _session.Dispose();
  }

  /// <summary>Close after the current frame — never call <see cref="IWindow.Close"/> during Render.</summary>
  private void RequestClose() => _closeRequested = true;

  private void Wire()
  {
    _window.Load += () =>
    {
      try
      {
        _gl = _window.CreateOpenGL();
        _input = _window.CreateInput();
        _controller = new ImGuiController(_gl, _window, _input, onConfigureIO: CalcFaceplateFonts.Configure);
        CalcFramelessShell.ApplyRoundedCorners(_window.Handle);
        // Shared GL context: launcher already owns SVG textures — do not rebind/dispose them here.
        _loaded = true;
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Startup Error");
        RequestClose();
      }
    };

    _window.Update += _ =>
    {
      double time = _window.Time;
      float delta = _lastFrameTime > 0d ? (float)(time - _lastFrameTime) : 0.016f;
      _lastFrameTime = time;
      _explorerPresenter.Tick(delta);
      _window.MakeCurrent();
      _controller?.Update(delta);
    };

    _window.Render += _ =>
    {
      if (_gl is null || _controller is null)
      {
        return;
      }

      try
      {
        _window.MakeCurrent();
        _gl.Viewport(_window.FramebufferSize);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.ClearColor(0f, 0f, 0f, 0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _controller.MakeCurrent();

        CalcFaceplatePointer.BeginFrame();
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x00000000u);
        ImGui.Begin(
          "##calc-faceplate-host",
          ImGuiWindowFlags.NoDecoration
          | ImGuiWindowFlags.NoMove
          | ImGuiWindowFlags.NoBringToFrontOnFocus
          | ImGuiWindowFlags.NoScrollbar
          | ImGuiWindowFlags.NoScrollWithMouse
          | ImGuiWindowFlags.NoBackground);

        CalcExplorerGlobalKeyboard.Update(_session);

        System.Numerics.Vector2 display = ImGui.GetIO().DisplaySize;
        DrawUnifiedChrome(ImGui.GetWindowDrawList(), display);

        CalcWindowTitlePanelComponent.TitleAction titleAction =
          CalcWindowTitlePanelComponent.Draw(
            _fittedToWorkArea,
            BeadInset,
            TopBandHeight,
            display.X - BeadInset);

        ImGui.SetCursorPos(new System.Numerics.Vector2(BandSide, BandTop));
        CalcFaceplateView.Draw(
          _session,
          new System.Numerics.Vector2(
            MathF.Max(1f, display.X - BandSide * 2f),
            MathF.Max(1f, display.Y - BandTop - LogoBandHeight - BeadInset)));
        HandleTitleAction(titleAction);
        HandleFramelessChrome();

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        _controller.Render();
        CalcFaceplatePointer.ApplyPendingCursor(_input);
      }
      catch (Exception exception)
      {
        FatalErrorDialog.Show(exception, "TeoCalc — Render Error");
        RequestClose();
      }
    };

    _window.FramebufferResize += size =>
    {
      _gl?.Viewport(size);
      if (_ignoreNextResize)
      {
        _ignoreNextResize = false;
        return;
      }

      if (_resizingWindow || _fittedToWorkArea)
      {
        return;
      }

      EnforceAspect(size);
    };

    _window.StateChanged += state =>
    {
      if (state != WindowState.Maximized)
      {
        return;
      }

      _window.WindowState = WindowState.Normal;
      ToggleFitToWorkArea();
    };

    _window.Closing += () =>
    {
      // Dispose / DisposeForAppExit own teardown; Closing may fire from Close() during either.
      // After DisposeForAppExit, _disposed is set so we skip full GPU TearDown.
      if (!_disposed)
      {
        TearDownGraphicsAndSession();
      }
    };
  }

  /// <summary>
  /// Unified calc chrome at window level: outer bead frame (2q black → 1q gray →
  /// 1q light-gray, fixed px, rounded to match the DWM corners), a fixed-height
  /// top title band (same fill as the switch panel) with rounded top corners, the
  /// inner body fill, then a fixed-height bottom logo band with rounded bottom corners.
  /// </summary>
  private void DrawUnifiedChrome(ImDrawListPtr draw, System.Numerics.Vector2 display)
  {
    float q = Calc00dWireStyle.FitilWidthRef;
    RectF frame = new(0f, 0f, display.X, display.Y);
    float radius = Calc00dWireStyle.OuterRadiusRef;

    RectF cursor = DrawBead(draw, frame, radius, q * 2f, Calc00dWireStyle.BlackFitilFill, Calc00dWireStyle.BlackFitilShine);
    radius = MathF.Max(0f, radius - q * 2f);

    cursor = DrawBead(draw, cursor, radius, q, Calc00dWireStyle.GrayFitilFill, Calc00dWireStyle.GrayFitilShine);
    radius = MathF.Max(0f, radius - q);

    cursor = DrawBead(draw, cursor, radius, q, Calc00dWireStyle.LightGrayFitilFill, Calc00dWireStyle.LightGrayFitilShine);
    radius = MathF.Max(0f, radius - q);

    FillRoundedRect(draw, cursor, radius, Calc00dWireStyle.InnerBodyFill);

    RectF band = new(cursor.X, cursor.Y, cursor.Width, TopBandHeight);
    draw.AddRectFilled(band.Min, band.Max, Calc00dWireStyle.SwitchPanelFill, radius, ImDrawFlags.RoundCornersTop);

    // Fixed-height bottom logo band mirroring the top title bar: inside the bead frame,
    // rounded bottom corners, filled with the brushed-aluminum logo plate.
    RectF logoBand = new(
      cursor.X,
      cursor.Y + cursor.Height - LogoBandHeight,
      cursor.Width,
      LogoBandHeight);
    draw.AddRectFilled(logoBand.Min, logoBand.Max, Calc00dWireStyle.DarkGrayBandFill, radius, ImDrawFlags.RoundCornersBottom);

    float logoScale = LogoBandHeight / CalcLogoPanelComponent.HeightRef;
    CalcModelDefinition model = CalcModelCatalog.Resolve(_session.Model, _catalogModelId);
    CalcLogoPanelComponent.Draw(draw, logoBand, logoScale, model);
  }

  private static RectF DrawBead(
    ImDrawListPtr draw,
    RectF outer,
    float radius,
    float width,
    uint bodyColor,
    uint shineColor)
  {
    float shine = MathF.Max(1f, width * Calc00dWireStyle.FitilShineFraction);
    FillRoundedRect(draw, outer, radius, shineColor);
    RectF body = Inset(outer, shine);
    FillRoundedRect(draw, body, MathF.Max(0f, radius - shine), bodyColor);
    return Inset(outer, width);
  }

  private static RectF Inset(RectF rect, float amount) => new(
    rect.X + amount,
    rect.Y + amount,
    MathF.Max(0f, rect.Width - amount * 2f),
    MathF.Max(0f, rect.Height - amount * 2f));

  private static void FillRoundedRect(ImDrawListPtr draw, RectF rect, float radius, uint color) =>
    draw.AddRectFilled(rect.Min, rect.Max, color, radius, ImDrawFlags.RoundCornersAll);

  private void HandleTitleAction(CalcWindowTitlePanelComponent.TitleAction action)
  {
    switch (action)
    {
      case CalcWindowTitlePanelComponent.TitleAction.Minimize:
        _window.WindowState = WindowState.Minimized;
        break;
      case CalcWindowTitlePanelComponent.TitleAction.ToggleMaximize:
        ToggleFitToWorkArea();
        break;
      case CalcWindowTitlePanelComponent.TitleAction.Close:
        RequestClose();
        break;
    }
  }

  private void HandleFramelessChrome()
  {
    System.Numerics.Vector2 display = ImGui.GetIO().DisplaySize;
    System.Numerics.Vector2 mouse = ImGui.GetIO().MousePos;
    bool overButtons = CalcWindowTitlePanelComponent.IsOverButtons(
      mouse,
      BeadInset,
      TopBandHeight,
      display.X - BeadInset);
    bool onTitleStrip = mouse.Y >= 0f && mouse.Y <= BandTop;

    // A double-click on the calc's top bar always toggles maximize/restore.
    if (!_resizingWindow && !overButtons && onTitleStrip
        && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
    {
      _draggingWindow = false;
      ToggleFitToWorkArea();
      return;
    }

    if (_resizingWindow)
    {
      UpdateResize();
      return;
    }

    if (_draggingWindow)
    {
      UpdateDrag();
      return;
    }

    if (overButtons)
    {
      // Buttons manage their own hand cursor + clicks.
      return;
    }

    ResizeZone zone = HitTestResize(mouse, display);

    // Edge / corner: show the real resize cursor on hover; start resizing on click.
    if (zone != ResizeZone.None)
    {
      CalcFaceplatePointer.RequestCursor(CursorForZone(zone));
      if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
      {
        BeginResize(zone);
      }

      return;
    }

    // Keys and switches keep their own hand cursor and consume their own clicks.
    bool canStartDrag = onTitleStrip || !CalcFaceplatePointer.IsOverInteractive;
    if (!canStartDrag)
    {
      return;
    }

    // Begin tracking a drag on press (actual window move waits for the threshold in
    // UpdateDrag, so a stationary tap or double-click never nudges the window).
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && TryGetCursorPos(out POINT press))
    {
      _draggingWindow = true;
      _dragMoved = false;
      _dragStartCursor = press;
      _dragStartWindowPos = _window.Position;
    }

    CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
  }

  private void UpdateDrag()
  {
    if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) || !TryGetCursorPos(out POINT cursor))
    {
      _draggingWindow = false;
      _dragMoved = false;
      return;
    }

    int dx = cursor.X - _dragStartCursor.X;
    int dy = cursor.Y - _dragStartCursor.Y;
    if (!_dragMoved)
    {
      if (dx * dx + dy * dy < (int)(DragThreshold * DragThreshold))
      {
        CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
        return;
      }

      _dragMoved = true;
      // Leave fit mode only once an actual move starts; preserve the restore baseline.
      _fittedToWorkArea = false;
    }

    _window.Position = new Vector2D<int>(_dragStartWindowPos.X + dx, _dragStartWindowPos.Y + dy);
    CalcFaceplatePointer.RequestCursor(StandardCursor.ResizeAll);
  }

  private void BeginResize(ResizeZone zone)
  {
    if (!TryGetCursorPos(out _dragStartCursor))
    {
      return;
    }

    _resizingWindow = true;
    _resizeZone = zone;
    _dragStartWindowPos = _window.Position;
    _dragStartWindowSize = _window.Size;
    if (_fittedToWorkArea)
    {
      _fittedToWorkArea = false;
    }

    CalcFaceplatePointer.RequestCursor(CursorForZone(zone));
  }

  private void UpdateResize()
  {
    if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) || !TryGetCursorPos(out POINT cursor))
    {
      _resizingWindow = false;
      _resizeZone = ResizeZone.None;
      return;
    }

    int dx = cursor.X - _dragStartCursor.X;
    int dy = cursor.Y - _dragStartCursor.Y;
    ApplyResizeDelta(dx, dy);
    CalcFaceplatePointer.RequestCursor(CursorForZone(_resizeZone));
  }

  private void ApplyResizeDelta(int dx, int dy)
  {
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

    int width = Math.Max(1, right - left);
    int height = Math.Max(1, bottom - top);

    // Lock aspect on the calc body; the window also carries the fixed frame bands.
    int chromeW = (int)ChromeWidth;
    int chromeH = (int)ChromeHeight;
    bool horizontal = moveLeft || moveRight;
    bool vertical = moveTop || moveBottom;
    if (horizontal && vertical)
    {
      if (MathF.Abs(dx) >= MathF.Abs(dy))
      {
        height = Math.Max(1, (int)MathF.Round((width - chromeW) / _aspect)) + chromeH;
      }
      else
      {
        width = Math.Max(1, (int)MathF.Round((height - chromeH) * _aspect)) + chromeW;
      }
    }
    else if (horizontal)
    {
      height = Math.Max(1, (int)MathF.Round((width - chromeW) / _aspect)) + chromeH;
    }
    else
    {
      width = Math.Max(1, (int)MathF.Round((height - chromeH) * _aspect)) + chromeW;
    }

    int minW = MinWindowWidth;
    int minH = Math.Max(1, (int)MathF.Round((minW - chromeW) / _aspect)) + chromeH;
    if (width < minW || height < minH)
    {
      width = minW;
      height = minH;
    }

    if (moveLeft && !moveRight)
    {
      left = right - width;
    }
    else if (moveRight && !moveLeft)
    {
      right = left + width;
    }
    else
    {
      // Corner with both: keep the fixed opposite edges.
      if (moveLeft)
      {
        left = right - width;
      }
      else
      {
        right = left + width;
      }
    }

    if (moveTop && !moveBottom)
    {
      top = bottom - height;
    }
    else if (moveBottom && !moveTop)
    {
      bottom = top + height;
    }
    else
    {
      if (moveTop)
      {
        top = bottom - height;
      }
      else
      {
        bottom = top + height;
      }
    }

    _applyingAspect = true;
    _ignoreNextResize = true;
    try
    {
      _window.Position = new Vector2D<int>(left, top);
      _window.Size = new Vector2D<int>(width, height);
    }
    finally
    {
      _applyingAspect = false;
    }
  }

  private static ResizeZone HitTestResize(System.Numerics.Vector2 mouse, System.Numerics.Vector2 display)
  {
    bool left = mouse.X >= 0f && mouse.X <= ResizeCorner;
    bool right = mouse.X >= display.X - ResizeCorner && mouse.X <= display.X;
    bool top = mouse.Y >= 0f && mouse.Y <= ResizeCorner;
    bool bottom = mouse.Y >= display.Y - ResizeCorner && mouse.Y <= display.Y;

    bool leftEdge = mouse.X >= 0f && mouse.X <= ResizeEdge;
    bool rightEdge = mouse.X >= display.X - ResizeEdge && mouse.X <= display.X;
    bool topEdge = mouse.Y >= 0f && mouse.Y <= ResizeEdge;
    bool bottomEdge = mouse.Y >= display.Y - ResizeEdge && mouse.Y <= display.Y;

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

    if (leftEdge)
    {
      return ResizeZone.W;
    }

    if (rightEdge)
    {
      return ResizeZone.E;
    }

    if (topEdge)
    {
      return ResizeZone.N;
    }

    if (bottomEdge)
    {
      return ResizeZone.S;
    }

    return ResizeZone.None;
  }

  private static StandardCursor CursorForZone(ResizeZone zone) => zone switch
  {
    ResizeZone.N or ResizeZone.S => StandardCursor.VResize,
    ResizeZone.E or ResizeZone.W => StandardCursor.HResize,
    ResizeZone.NW or ResizeZone.SE => StandardCursor.NwseResize,
    ResizeZone.NE or ResizeZone.SW => StandardCursor.NeswResize,
    _ => StandardCursor.Default,
  };

  private void EnforceAspect(Vector2D<int> size)
  {
    if (_applyingAspect || size.X <= 0 || size.Y <= 0)
    {
      return;
    }

    float bodyW = MathF.Max(1f, size.X - ChromeWidth);
    int targetH = (int)MathF.Round(bodyW / _aspect) + (int)ChromeHeight;
    if (Math.Abs(targetH - size.Y) <= 1)
    {
      return;
    }

    _applyingAspect = true;
    try
    {
      _window.Size = new Vector2D<int>(size.X, targetH);
    }
    finally
    {
      _applyingAspect = false;
    }
  }

  private void ToggleFitToWorkArea()
  {
    if (_fittedToWorkArea)
    {
      RestoreFromFit();
      return;
    }

    FitToWorkArea();
  }

  private void FitToWorkArea()
  {
    if (!TryGetWorkArea(out int workX, out int workY, out int workW, out int workH))
    {
      return;
    }

    if (!_fittedToWorkArea)
    {
      _restorePosition = _window.Position;
      _restoreSize = _window.Size;
    }

    // Fill the work area height; keep the current horizontal position (no recentring).
    float bodyMaxW = workW - ChromeWidth;
    float bodyMaxH = workH - ChromeHeight;
    float bodyH = bodyMaxH;
    float bodyW = bodyH * _aspect;
    if (bodyW > bodyMaxW)
    {
      bodyW = bodyMaxW;
      bodyH = bodyW / _aspect;
    }

    float width = bodyW + ChromeWidth;
    float height = bodyH + ChromeHeight;

    int w = Math.Max(MinWindowWidth, (int)MathF.Round(width));
    int h = Math.Max((int)ChromeHeight, (int)MathF.Round(height));
    int x = Math.Clamp(_window.Position.X, workX, Math.Max(workX, workX + workW - w));
    int y = Math.Clamp(_window.Position.Y, workY, Math.Max(workY, workY + workH - h));

    _applyingAspect = true;
    _ignoreNextResize = true;
    try
    {
      _window.Position = new Vector2D<int>(x, y);
      _window.Size = new Vector2D<int>(w, h);
      _fittedToWorkArea = true;
    }
    finally
    {
      _applyingAspect = false;
    }
  }

  private void RestoreFromFit()
  {
    if (_restoreSize.X <= 0 || _restoreSize.Y <= 0)
    {
      _restoreSize = InitialSize(_aspect);
      if (TryGetWorkArea(out int workX, out int workY, out int workW, out int workH))
      {
        _restorePosition = new Vector2D<int>(
          workX + Math.Max(0, (workW - _restoreSize.X) / 2),
          workY + Math.Max(0, (workH - _restoreSize.Y) / 2));
      }
    }

    _applyingAspect = true;
    _ignoreNextResize = true;
    try
    {
      _window.Position = _restorePosition;
      _window.Size = _restoreSize;
      _fittedToWorkArea = false;
    }
    finally
    {
      _applyingAspect = false;
    }
  }

  private static bool TryLoadModel(CalcExplorerSession session, string modelId)
  {
    int modelIndex = Array.FindIndex(
      session.Models,
      id => string.Equals(id, modelId, StringComparison.OrdinalIgnoreCase));
    if (modelIndex < 0)
    {
      return false;
    }

    if (!string.Equals(session.Model.Model, modelId, StringComparison.OrdinalIgnoreCase))
    {
      session.LoadModel(modelIndex);
    }

    return true;
  }

  private static Vector2D<int> InitialSize(float aspect)
  {
    float chromeW = ChromeWidth;
    float chromeH = ChromeHeight;
    // Fixed body width across models → uniform scale for shared column metrics; height follows aspect.
    float bodyW = UnifiedBodyWidth;
    float bodyH = bodyW / MathF.Max(0.01f, aspect);

    if (TryGetWorkArea(out _, out _, out int workW, out int workH))
    {
      float bodyMaxW = workW * DefaultWindowScale - chromeW;
      float bodyMaxH = workH * DefaultWindowScale - chromeH;
      bodyW = MathF.Min(UnifiedBodyWidth, bodyMaxW);
      bodyH = bodyW / MathF.Max(0.01f, aspect);
      if (bodyH > bodyMaxH)
      {
        bodyH = bodyMaxH;
        bodyW = bodyH * aspect;
      }
    }

    return new Vector2D<int>(
      Math.Max(MinWindowWidth, (int)MathF.Round(bodyW + chromeW)),
      Math.Max((int)chromeH, (int)MathF.Round(bodyH + chromeH)));
  }

  private static bool TryGetWorkArea(out int x, out int y, out int width, out int height)
  {
    x = y = width = height = 0;
    if (!OperatingSystem.IsWindows())
    {
      return false;
    }

    RECT rect = default;
    if (!SystemParametersInfo(SpiGetWorkArea, 0, ref rect, 0))
    {
      return false;
    }

    x = rect.Left;
    y = rect.Top;
    width = rect.Right - rect.Left;
    height = rect.Bottom - rect.Top;
    return width > 0 && height > 0;
  }

  private static bool TryGetCursorPos(out POINT point)
  {
    point = default;
    if (!CalcFramelessShell.TryGetCursorPos(out int x, out int y))
    {
      return false;
    }

    point = new POINT { X = x, Y = y };
    return true;
  }

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

  private const uint SpiGetWorkArea = 0x0030;

  [StructLayout(LayoutKind.Sequential)]
  private struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;
    public int Y;
  }

  [DllImport("user32.dll", SetLastError = true)]
  private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);
}
