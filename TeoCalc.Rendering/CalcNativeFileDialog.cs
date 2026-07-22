using System.Runtime.InteropServices;
using System.Text;

namespace TeoCalc.Rendering;

/// <summary>Card export target for native save dialogs.</summary>
public enum CardExportFormat
{
  CuveSoftXml,
  TeoJson,
}

/// <summary>Windows native open/save dialogs for card files.</summary>
public static class CalcNativeFileDialog
{
  public static bool TryPickCardFile(string? initialDirectory, out string path) =>
    TryPick(initialDirectory, save: false, suggestedFileName: null, exportFormat: null, out path);

  public static bool TrySaveCardFile(string? initialDirectory, string? suggestedFileName, out string path) =>
    TryPick(initialDirectory, save: true, suggestedFileName, exportFormat: null, out path);

  public static bool TryExportCardFile(
    string? initialDirectory,
    string? suggestedFileName,
    CardExportFormat format,
    out string path) =>
    TryPick(initialDirectory, save: true, suggestedFileName, format, out path);

  /// <summary>Windows folder picker (IFileOpenDialog + FOS_PICKFOLDERS).</summary>
  public static bool TryPickFolder(string? initialDirectory, out string path)
  {
    path = string.Empty;
    if (!OperatingSystem.IsWindows())
    {
      return false;
    }

    return WindowsPickFolder(initialDirectory, out path);
  }

  private static bool TryPick(
    string? initialDirectory,
    bool save,
    string? suggestedFileName,
    CardExportFormat? exportFormat,
    out string path)
  {
    path = string.Empty;
    if (!OperatingSystem.IsWindows())
    {
      return false;
    }

    return save
      ? WindowsSave(initialDirectory, suggestedFileName, exportFormat, out path)
      : WindowsOpen(initialDirectory, out path);
  }

  private static bool WindowsPickFolder(string? initialDirectory, out string path)
  {
    path = string.Empty;
    nint title = Marshal.StringToCoTaskMemUni("Choose cards folder");
    nint display = Marshal.AllocCoTaskMem(260 * 2);
    try
    {
      BrowseInfo bi = new()
      {
        HwndOwner = 0,
        PidlRoot = 0,
        DisplayName = display,
        Title = title,
        Flags = BifReturnOnlyFsDirs | BifNewDialogStyle,
        Callback = 0,
        LParam = 0,
        Image = 0,
      };

      nint pidl = SHBrowseForFolderW(ref bi);
      if (pidl == 0)
      {
        return false;
      }

      try
      {
        nint pathBuf = Marshal.AllocCoTaskMem(260 * 2);
        try
        {
          if (!SHGetPathFromIDListW(pidl, pathBuf))
          {
            return false;
          }

          path = Marshal.PtrToStringUni(pathBuf) ?? string.Empty;
          return path.Length > 0 && Directory.Exists(path);
        }
        finally
        {
          Marshal.FreeCoTaskMem(pathBuf);
        }
      }
      finally
      {
        CoTaskMemFree(pidl);
      }
    }
    finally
    {
      Marshal.FreeCoTaskMem(title);
      Marshal.FreeCoTaskMem(display);
    }
  }

  private const int BifReturnOnlyFsDirs = 0x0001;
  private const int BifNewDialogStyle = 0x0040;

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct BrowseInfo
  {
    public nint HwndOwner;
    public nint PidlRoot;
    public nint DisplayName;
    public nint Title;
    public int Flags;
    public nint Callback;
    public nint LParam;
    public int Image;
  }

#pragma warning disable SYSLIB1054
  [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
  private static extern nint SHBrowseForFolderW(ref BrowseInfo lpbi);

  [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
  private static extern bool SHGetPathFromIDListW(nint pidl, nint pszPath);

  [DllImport("ole32.dll")]
  private static extern void CoTaskMemFree(nint pv);
#pragma warning restore SYSLIB1054

  private static bool WindowsOpen(string? initialDirectory, out string path)
  {
    path = string.Empty;
    const int maxPath = 1024;
    nint fileBuffer = Marshal.AllocCoTaskMem(maxPath * 2);
    nint filter = Marshal.StringToCoTaskMemUni(BuildOpenFilter());
    nint title = Marshal.StringToCoTaskMemUni("Load card");
    nint initialDir = AllocDir(initialDirectory);

    try
    {
      OpenFileName dialog = new()
      {
        StructSize = OpenFileNameStructSize,
        Filter = filter,
        File = fileBuffer,
        MaxFile = maxPath,
        InitialDir = initialDir,
        Title = title,
        Flags = OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir,
      };

      if (!GetOpenFileNameW(ref dialog))
      {
        return false;
      }

      path = Marshal.PtrToStringUni(fileBuffer) ?? string.Empty;
      return path.Length > 0;
    }
    finally
    {
      Marshal.FreeCoTaskMem(fileBuffer);
      Marshal.FreeCoTaskMem(filter);
      Marshal.FreeCoTaskMem(title);
      FreeDir(initialDir);
    }
  }

  private static bool WindowsSave(
    string? initialDirectory,
    string? suggestedFileName,
    CardExportFormat? exportFormat,
    out string path)
  {
    path = string.Empty;
    const int maxPath = 1024;
    nint fileBuffer = Marshal.AllocCoTaskMem(maxPath * 2);
    if (!string.IsNullOrWhiteSpace(suggestedFileName))
    {
      byte[] bytes = Encoding.Unicode.GetBytes(Path.GetFileName(suggestedFileName));
      Marshal.Copy(bytes, 0, fileBuffer, Math.Min(bytes.Length, (maxPath * 2) - 2));
    }

    string filterText = exportFormat switch
    {
      CardExportFormat.CuveSoftXml => BuildExportCuveSoftFilter(),
      CardExportFormat.TeoJson => BuildExportTeoJsonFilter(),
      _ => BuildSaveFilter(),
    };
    string titleText = exportFormat switch
    {
      CardExportFormat.CuveSoftXml => "Export CuveSoft card",
      CardExportFormat.TeoJson => "Export Teo card",
      _ => "Save card",
    };
    string defExtText = exportFormat switch
    {
      CardExportFormat.CuveSoftXml => "xml",
      CardExportFormat.TeoJson => "json",
      _ => "t65",
    };

    nint filter = Marshal.StringToCoTaskMemUni(filterText);
    nint title = Marshal.StringToCoTaskMemUni(titleText);
    nint initialDir = AllocDir(initialDirectory);
    nint defExt = Marshal.StringToCoTaskMemUni(defExtText);

    try
    {
      OpenFileName dialog = new()
      {
        StructSize = OpenFileNameStructSize,
        Filter = filter,
        File = fileBuffer,
        MaxFile = maxPath,
        InitialDir = initialDir,
        Title = title,
        DefExt = defExt,
        Flags = OfnExplorer | OfnPathMustExist | OfnNoChangeDir | OfnOverwritePrompt,
      };

      if (!GetSaveFileNameW(ref dialog))
      {
        return false;
      }

      path = Marshal.PtrToStringUni(fileBuffer) ?? string.Empty;
      return path.Length > 0;
    }
    finally
    {
      Marshal.FreeCoTaskMem(fileBuffer);
      Marshal.FreeCoTaskMem(filter);
      Marshal.FreeCoTaskMem(title);
      FreeDir(initialDir);
      Marshal.FreeCoTaskMem(defExt);
    }
  }

  private static string BuildOpenFilter() =>
    "T-65 cards (*.t65)\0*.t65\0" +
    "T-67 cards (*.t67)\0*.t67\0" +
    "CuveSoft (*.xml;*.plist;*.rpn65)\0*.xml;*.plist;*.rpn65\0" +
    "Teo JSON (*.json)\0*.json\0" +
    "Legacy card text (*.t6x)\0*.t6x\0" +
    "All files (*.*)\0*.*\0\0";

  private static string BuildSaveFilter() =>
    "T-65 card (*.t65)\0*.t65\0" +
    "T-67 card (*.t67)\0*.t67\0" +
    "All files (*.*)\0*.*\0\0";

  private static string BuildExportCuveSoftFilter() =>
    "CuveSoft (*.xml)\0*.xml\0" +
    "All files (*.*)\0*.*\0\0";

  private static string BuildExportTeoJsonFilter() =>
    "Teo JSON (*.json)\0*.json\0" +
    "All files (*.*)\0*.*\0\0";

  private static nint AllocDir(string? initialDirectory) =>
    !string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory)
      ? Marshal.StringToCoTaskMemUni(initialDirectory)
      : 0;

  private static void FreeDir(nint initialDir)
  {
    if (initialDir != 0)
    {
      Marshal.FreeCoTaskMem(initialDir);
    }
  }

  private static int OpenFileNameStructSize => IntPtr.Size == 8 ? 152 : 76;

  private const int OfnExplorer = 0x00080000;
  private const int OfnFileMustExist = 0x00001000;
  private const int OfnPathMustExist = 0x00000800;
  private const int OfnNoChangeDir = 0x00000008;
  private const int OfnOverwritePrompt = 0x00000002;

#pragma warning disable SYSLIB1054
  [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetOpenFileNameW")]
  private static extern bool GetOpenFileNameW(ref OpenFileName dialog);

  [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetSaveFileNameW")]
  private static extern bool GetSaveFileNameW(ref OpenFileName dialog);
#pragma warning restore SYSLIB1054

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct OpenFileName
  {
    public int StructSize;
    public nint Owner;
    public nint Instance;
    public nint Filter;
    public nint CustomFilter;
    public int MaxCustomFilter;
    public int FilterIndex;
    public nint File;
    public int MaxFile;
    public nint FileTitle;
    public int MaxFileTitle;
    public nint InitialDir;
    public nint Title;
    public int Flags;
    public short FileOffset;
    public short FileExtension;
    public nint DefExt;
    public nint CustData;
    public nint Hook;
    public nint TemplateName;
    public nint Reserved0;
    public int Reserved1;
    public int FlagsEx;
  }
}
