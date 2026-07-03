using System.Runtime.InteropServices;

namespace TeoCalc.Rendering;

/// <summary>Shows startup and fatal errors when the Silk UI is not available yet.</summary>
public static partial class FatalErrorDialog
{
  private const uint MbOk = 0x0000_0000;
  private const uint MbIconError = 0x0000_0010;

  public static void Show(Exception exception, string? title = null)
  {
    string caption = title ?? "TeoCalc — Fatal Error";
    string message = BuildMessage(exception);
    Console.Error.WriteLine(caption);
    Console.Error.WriteLine(message);
    if (OperatingSystem.IsWindows())
      _ = MessageBoxW(IntPtr.Zero, message, caption, MbOk | MbIconError);
  }

  private static string BuildMessage(Exception exception)
  {
    string text = exception.Message;
    if (!string.IsNullOrWhiteSpace(exception.StackTrace))
      text += Environment.NewLine + Environment.NewLine + exception.StackTrace;
    return text.Length > 32_000 ? text[..32_000] + "…" : text;
  }

  [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
  private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
