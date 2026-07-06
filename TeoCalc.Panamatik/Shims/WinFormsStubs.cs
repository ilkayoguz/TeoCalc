using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms;

public class Form : Control, IDisposable
{
  private bool _disposed;

  public FormBorderStyle FormBorderStyle { get; set; }

  public Color TransparencyKey { get; set; }

  public Size MaximumSize { get; set; }

  public Size MinimumSize { get; set; }

  public double Opacity { get; set; } = 1;

  public bool ShowInTaskbar { get; set; } = true;

  public FormStartPosition StartPosition { get; set; }

  public AutoScaleMode AutoScaleMode { get; set; }

  public Size ClientSize
  {
    get => Size;
    set => Size = value;
  }

  public SizeF AutoScaleDimensions { get; set; }

  public Icon? Icon { get; set; }

  protected IContainer components { get; set; } = new Container();

  public new DialogResult DialogResult { get; set; }

  public event FormClosedEventHandler? FormClosed;

  public void SuspendLayout()
  {
  }

  public void ResumeLayout(bool performLayout)
  {
  }

  public void PerformLayout()
  {
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed)
    {
      return;
    }

    if (disposing)
    {
      components?.Dispose();
    }

    _disposed = true;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}

public class Control
{
  public string Name { get; set; } = string.Empty;

  public string Text { get; set; } = string.Empty;

  public Point Location { get; set; }

  public Size Size { get; set; }

  public Color BackColor { get; set; }

  public Color ForeColor { get; set; }

  public Font? Font { get; set; }

  public Cursor Cursor { get; set; } = Cursors.Default;

  public int TabIndex { get; set; }

  public bool TabStop { get; set; } = true;

  public bool Visible { get; set; } = true;

  public int Width
  {
    get => Size.Width;
    set => Size = new Size(value, Size.Height);
  }

  public int Height
  {
    get => Size.Height;
    set => Size = new Size(Size.Width, value);
  }

  public ControlCollection Controls { get; } = new();

  public event EventHandler? Click;

  public event KeyEventHandler? KeyDown;

  public event KeyEventHandler? KeyUp;

  public event KeyPressEventHandler? KeyPress;

  public event MouseEventHandler? MouseDown;

  public event MouseEventHandler? MouseMove;

  public event MouseEventHandler? MouseUp;

  public event MouseEventHandler? MouseClick;

  public event MouseEventHandler? MouseDoubleClick;

  public event PaintEventHandler? Paint;

  public void Update()
  {
  }

  public void Invalidate()
  {
  }
}

public sealed class ControlCollection : List<Control>
{
}

public class TextBox : Control
{
  public BorderStyle BorderStyle { get; set; }

  public bool ReadOnly { get; set; }

  public bool WordWrap { get; set; }

  public bool Multiline { get; set; }

  public string[] Lines { get; set; } = [];

  public int MaxLength { get; set; } = int.MaxValue;

  public void Focus()
  {
  }
}

public class Label : Control
{
  public bool AutoSize { get; set; }
}

public class Button : Control
{
  public bool UseVisualStyleBackColor { get; set; }
}

public class PictureBox : Control, ISupportInitialize
{
  public Image? Image { get; set; }

  public Image? InitialImage { get; set; }

  public void BeginInit()
  {
  }

  public void EndInit()
  {
  }
}

public sealed class Timer : IDisposable
{
  public int Interval { get; set; } = 50;

  public bool Enabled { get; set; }

  public event EventHandler? Tick;

  public void Start() => Enabled = true;

  public void Stop() => Enabled = false;

  public void Dispose()
  {
  }

  public Timer(IContainer? container = null)
  {
  }
}

public static class Application
{
  public static void EnableVisualStyles()
  {
  }

  public static void SetCompatibleTextRenderingDefault(bool defaultValue)
  {
  }

  public static void Run(Form form)
  {
  }

  public static void Exit()
  {
  }
}

public static class MessageBox
{
  public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
    DialogResult.OK;
}

public sealed class OpenFileDialog
{
  public string Filter { get; set; } = string.Empty;

  public int FilterIndex { get; set; }

  public bool RestoreDirectory { get; set; }

  public string FileName { get; set; } = string.Empty;

  public DialogResult ShowDialog() => DialogResult.Cancel;
}

public sealed class SaveFileDialog
{
  public string Filter { get; set; } = string.Empty;

  public int FilterIndex { get; set; }

  public bool RestoreDirectory { get; set; }

  public string FileName { get; set; } = string.Empty;

  public DialogResult ShowDialog() => DialogResult.Cancel;
}

public enum FormBorderStyle
{
  None,
  FixedSingle,
  FixedDialog,
  Sizable,
}

public enum FormStartPosition
{
  Manual,
  CenterScreen,
}

public enum AutoScaleMode
{
  Font,
  None,
}

public enum BorderStyle
{
  None,
  FixedSingle,
}

public enum DialogResult
{
  None,
  OK,
  Cancel,
}

public enum MessageBoxButtons
{
  OK,
}

public enum MessageBoxIcon
{
  Hand,
}

public static class Cursors
{
  public static Cursor Default { get; } = new();
  public static Cursor IBeam { get; } = new();
}

public sealed class Cursor
{
}

public delegate void KeyEventHandler(object? sender, KeyEventArgs e);

public delegate void KeyPressEventHandler(object? sender, KeyPressEventArgs e);

public delegate void MouseEventHandler(object? sender, MouseEventArgs e);

public delegate void PaintEventHandler(object? sender, PaintEventArgs e);

public delegate void FormClosedEventHandler(object? sender, FormClosedEventArgs e);

public sealed class KeyEventArgs : EventArgs
{
  public KeyEventArgs(Keys keyData) => KeyData = keyData;

  public Keys KeyData { get; }
}

public sealed class KeyPressEventArgs : EventArgs
{
  public KeyPressEventArgs(char keyChar) => KeyChar = keyChar;

  public char KeyChar { get; }

  public bool Handled { get; set; }
}

public sealed class MouseEventArgs : EventArgs
{
  public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
  {
    Button = button;
    X = x;
    Y = y;
  }

  public MouseButtons Button { get; }

  public int X { get; }

  public int Y { get; }

  public Point Location => new(X, Y);
}

public sealed class FormClosedEventArgs : EventArgs
{
  public FormClosedEventArgs(CloseReason closeReason) => CloseReason = closeReason;

  public CloseReason CloseReason { get; }
}

public enum CloseReason
{
  None,
  UserClosing,
}

public enum Keys
{
  None,
  Escape,
}

public enum MouseButtons
{
  None,
  Left,
}
