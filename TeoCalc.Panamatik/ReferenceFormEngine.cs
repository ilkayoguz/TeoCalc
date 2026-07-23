using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace TeoCalc.ReferenceEmulator;

public sealed class ReferenceFormEngine : IReferenceEngine
{
  private readonly Form _form;
  private readonly Type _formType;

  public ReferenceFormEngine(string modelId, string modelDirectory, Type formType)
  {
    ModelId = modelId;
    _formType = formType;
    string previousDirectory = Directory.GetCurrentDirectory();
    try
    {
      Directory.SetCurrentDirectory(modelDirectory);
      _form = (Form)Activator.CreateInstance(formType)!;
      ConfigureHeadlessForm(_form);
    }
    finally
    {
      Directory.SetCurrentDirectory(previousDirectory);
    }
  }

  public string ModelId { get; }

  public bool PowerOn { get; set; }

  public bool ProgramMode => GetProperty<bool>("HeadlessProgramMode");

  public string DisplayText => GetProperty<string>("HeadlessDisplayText") ?? string.Empty;

  public bool DisplayOn => GetProperty<bool>("HeadlessDisplayOn");

  public ReferenceEngineSnapshot Snapshot
  {
    get
    {
      object snapshot = GetProperty<object>("HeadlessSnapshot")!;
      Type snapshotType = snapshot.GetType();
      return new ReferenceEngineSnapshot(
        (ushort)snapshotType.GetProperty("ProgramCounter")!.GetValue(snapshot)!,
        (ushort)snapshotType.GetProperty("Status")!.GetValue(snapshot)!,
        (byte)snapshotType.GetProperty("KeyBuffer")!.GetValue(snapshot)!,
        (byte)snapshotType.GetProperty("Flags")!.GetValue(snapshot)!,
        (byte)snapshotType.GetProperty("P")!.GetValue(snapshot)!,
        (byte)snapshotType.GetProperty("Rom")!.GetValue(snapshot)!,
        (byte)snapshotType.GetProperty("Grp")!.GetValue(snapshot)!);
    }
  }

  public void PowerOnResume()
  {
    InvokeVoid("HeadlessPowerOn");
    PowerOn = true;
  }

  public void PowerOff()
  {
    InvokeVoid("HeadlessPowerOff");
    PowerOn = false;
  }

  public void SetProgramMode(bool programMode) =>
    InvokeVoid("HeadlessSetProgramMode", programMode);

  public void RunTimerBatch() =>
    InvokeVoid("HeadlessRunTimerBatch");

  public void PressKey(byte keyCode)
  {
    InvokeVoid("HeadlessPressKey", keyCode);
    RunTimerBatch();
  }

  public void ReleaseKey() =>
    InvokeVoid("HeadlessReleaseKey");

  public void Dispose()
  {
    _form.Dispose();
  }

  private static void ConfigureHeadlessForm(Form form)
  {
    form.Opacity = 0;
    form.ShowInTaskbar = false;
    form.FormBorderStyle = FormBorderStyle.None;
    form.StartPosition = FormStartPosition.Manual;
    form.Location = new Point(-32000, -32000);
  }

  private void InvokeVoid(string methodName, params object[] args)
  {
    MethodInfo? method = _formType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
    if (method is null)
    {
      throw new MissingMethodException(_formType.FullName, methodName);
    }

    method.Invoke(_form, args);
  }

  private T GetProperty<T>(string propertyName)
  {
    PropertyInfo? property = _formType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null)
    {
      throw new MissingMemberException(_formType.FullName, propertyName);
    }

    return (T)property.GetValue(_form)!;
  }
}
