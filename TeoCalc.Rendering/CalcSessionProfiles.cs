namespace TeoCalc.Rendering;

/// <summary>
/// Combobox profiles + feature toggles + Save as. Persists via <see cref="CalcUserSettingsStore"/>.
/// </summary>
public static class CalcSessionProfiles
{
  private static CalcSessionProfile? _working;
  private static string? _workingId;

  public static string ActiveProfileId
  {
    get
    {
      EnsureWorking();
      return _workingId!;
    }
  }

  public static CalcSessionProfile Active
  {
    get
    {
      EnsureWorking();
      return _working!;
    }
  }

  public static IReadOnlyList<CalcSessionProfile> List()
  {
    List<CalcSessionProfile> list = [.. CalcSessionProfile.BuiltIns.Select(p => p.Clone())];
    foreach (CalcSessionProfile custom in CalcUserSettingsStore.LoadCustomSessionProfiles())
    {
      list.Add(custom.Clone());
    }

    return list;
  }

  public static void Select(string profileId, CalcExplorerSession? session = null)
  {
    CalcSessionProfile? found = Find(profileId);
    if (found is null)
    {
      found = Find(CalcSessionProfile.StandardId) ?? CalcSessionProfile.BuiltIns[1].Clone();
      profileId = found.Id;
    }

    _working = found.Clone();
    _workingId = profileId;
    CalcUserSettingsStore.SaveActiveSessionProfileId(profileId);
    if (session is not null)
    {
      ApplyTo(session);
    }
  }

  public static void SetControlExecutionSpeed(bool enabled)
  {
    EnsureWorking();
    if (_working!.ControlExecutionSpeed == enabled)
    {
      return;
    }

    _working.ControlExecutionSpeed = enabled;
    PersistWorkingIfCustom();
  }

  public static void SetExecutionSpeedIndex(int index)
  {
    EnsureWorking();
    int clamped = Math.Clamp(index, 0, CalcExplorerSession.ExecutionSpeedStepCount - 1);
    if (_working!.ExecutionSpeedIndex == clamped)
    {
      return;
    }

    _working.ExecutionSpeedIndex = clamped;
    PersistWorkingIfCustom();
  }

  /// <summary>
  /// Clone working profile (speed from session when present) under a new name and select it.
  /// </summary>
  public static bool TrySaveAs(string name, CalcExplorerSession? session, out string? error)
  {
    error = null;
    name = name.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
      error = "Name required.";
      return false;
    }

    EnsureWorking();
    string id = "custom-" + Guid.NewGuid().ToString("N")[..8];
    int speedIndex = session?.ExecutionSpeedIndex ?? _working!.ExecutionSpeedIndex;
    CalcSessionProfile created = new()
    {
      Id = id,
      Name = name,
      ExecutionSpeedIndex = speedIndex,
      ControlExecutionSpeed = _working!.ControlExecutionSpeed,
      IsBuiltIn = false,
    };

    List<CalcSessionProfile> customs = [.. CalcUserSettingsStore.LoadCustomSessionProfiles()];
    if (customs.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
    {
      error = "A profile with that name already exists.";
      return false;
    }

    customs.Add(created);
    CalcUserSettingsStore.SaveCustomSessionProfiles(customs);
    Select(id, session);
    return true;
  }

  public static void ApplyTo(CalcExplorerSession session)
  {
    EnsureWorking();
    if (_working!.ControlExecutionSpeed)
    {
      session.SetExecutionSpeedIndex(_working.ExecutionSpeedIndex);
    }
  }

  public static string FormatSpeedLabel(int executionSpeedIndex) =>
    CalcExplorerSession.FormatExecutionSpeedLabel(executionSpeedIndex);

  private static void PersistWorkingIfCustom()
  {
    if (_working is null || _working.IsBuiltIn || _workingId is null)
    {
      return;
    }

    List<CalcSessionProfile> customs = [.. CalcUserSettingsStore.LoadCustomSessionProfiles()];
    int i = customs.FindIndex(p => p.Id == _workingId);
    if (i < 0)
    {
      return;
    }

    customs[i] = _working.Clone(builtIn: false);
    CalcUserSettingsStore.SaveCustomSessionProfiles(customs);
  }

  private static void EnsureWorking()
  {
    if (_working is not null && _workingId is not null)
    {
      return;
    }

    string id = CalcUserSettingsStore.LoadActiveSessionProfileId();
    CalcSessionProfile? found = Find(id);
    if (found is null)
    {
      found = CalcSessionProfile.BuiltIns.First(p => p.Id == CalcSessionProfile.StandardId).Clone();
      id = found.Id;
    }

    _working = found;
    _workingId = id;
  }

  private static CalcSessionProfile? Find(string profileId)
  {
    CalcSessionProfile? builtIn = CalcSessionProfile.BuiltIns
      .FirstOrDefault(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
    if (builtIn is not null)
    {
      return builtIn.Clone();
    }

    return CalcUserSettingsStore.LoadCustomSessionProfiles()
      .FirstOrDefault(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase))
      ?.Clone();
  }
}
