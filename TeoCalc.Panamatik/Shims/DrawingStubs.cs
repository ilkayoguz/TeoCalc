namespace System.Drawing;

public sealed class Font
{
  public Font(string familyName, float emSize)
  {
    Name = familyName;
    Size = emSize;
  }

  public Font(Font prototype, FontStyle style)
  {
    Name = prototype.Name;
    Size = prototype.Size;
    Style = style;
  }

  public Font(string familyName, float emSize, FontStyle style)
  {
    Name = familyName;
    Size = emSize;
    Style = style;
  }

  public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
  {
    Name = familyName;
    Size = emSize;
    Style = style;
  }

  public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
  {
    Name = familyName;
    Size = emSize;
    Style = style;
  }

  public string Name { get; }

  public float Size { get; }

  public FontStyle Style { get; }
}

[Flags]
public enum FontStyle
{
  Regular = 0,
  Bold = 1,
}

public enum GraphicsUnit
{
  Point,
  Pixel,
}

public abstract class Image
{
  public Size Size { get; protected set; }

  public static Image FromFile(string filename) => new Bitmap(filename);
}

public sealed class Bitmap : Image
{
  public Bitmap(string filename)
  {
    Size = new Size(220, 461);
  }
}

public sealed class Icon
{
}

public sealed class SolidBrush : Brush, IDisposable
{
  public SolidBrush(Color color)
  {
  }

  public void Dispose()
  {
  }
}

public sealed class PaintEventArgs : EventArgs
{
  public PaintEventArgs(Graphics graphics, Rectangle clipRect)
  {
    Graphics = graphics;
    ClipRectangle = clipRect;
  }

  public Graphics Graphics { get; }

  public Rectangle ClipRectangle { get; }
}

public sealed class Graphics
{
  public void FillRectangle(Brush brush, int x, int y, int width, int height)
  {
  }

  public void FillRectangle(Brush brush, Rectangle rect)
  {
  }
}

public abstract class Brush
{
}
