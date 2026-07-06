# Prepares all 20 Panamatik projects for in-process TeoCalc hosting:
# - unique AssemblyName per model (Panamatik.HP35, ...)
# - PanamatikHeadlessSnapshot.cs per namespace
# - Headless API on each main emulator form
param(
  [string]$Root = (Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik')
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path

function Get-SnapshotSource([string]$Namespace) {
  @"
namespace $Namespace;

public readonly struct PanamatikHeadlessSnapshot
{
  public PanamatikHeadlessSnapshot(
    ushort programCounter,
    ushort status,
    byte keyBuffer,
    byte flags,
    byte p,
    byte rom,
    byte grp)
  {
    ProgramCounter = programCounter;
    Status = status;
    KeyBuffer = keyBuffer;
    Flags = flags;
    P = p;
    Rom = rom;
    Grp = grp;
  }

  public ushort ProgramCounter { get; }

  public ushort Status { get; }

  public byte KeyBuffer { get; }

  public byte Flags { get; }

  public byte P { get; }

  public byte Rom { get; }

  public byte Grp { get; }
}
"@
}

function Get-HeadlessBlock([string]$Variant) {
  switch ($Variant) {
    'classic35' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			act_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 1;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_rom,
			act_grp);
'@
    }
    'classic45' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			classic_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 1;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_rom,
			act_grp);
'@
    }
    'classic55' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			classic_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 1;
			}

			if (prgmmode)
			{
				act_s |= 8;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_rom,
			act_grp);
'@
    }
    'woodstock' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			act_execute_instruction();
			act_s |= 32;
			if (!prgmmode)
			{
				act_s |= 8;
			}

			if (buttonpressed)
			{
				act_s |= 32768;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_del_rom,
			0);
'@
    }
    'spice' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			act_execute_instruction();
			if (!prgmmode)
			{
				act_s |= 8;
			}

			if (buttonpressed)
			{
				act_s |= 32768;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_del_rom,
			0);
'@
    }
    'hp67' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
		timermode = false;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			if (buttonpressed)
			{
				act_s |= 32768;
			}

			act_execute_instruction();
		}

		if (clearprefix)
		{
			clearprefix = false;
			act_s &= 56879;
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_del_rom,
			0);
'@
    }
    'hp19c' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		buttonpressed = false;
		buttondown = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
		buttondown = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
		buttondown = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmrunmode = programMode ? (byte)1 : (byte)0;
	}

	public bool HeadlessProgramMode => prgmrunmode != 0;

	public void HeadlessRunTimerBatch()
	{
		timer1_Tick(this, EventArgs.Empty);
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_del_rom,
			0);
'@
    }
    'hp01' {
      return @'

	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		keycode = 0;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
	}

	public void HeadlessReleaseKey()
	{
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
	}

	public bool HeadlessProgramMode => false;

	public void HeadlessRunTimerBatch()
	{
		timer1_Tick(this, EventArgs.Empty);
		if (RefreshDisplay)
		{
			RefreshDisplay = false;
			ShowDisplay();
		}
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			0,
			0);
'@
    }
    default { throw "Unknown variant $Variant" }
  }
}

$models = @(
  @{ Folder = 'HP-35'; Project = 'HP35.csproj'; Assembly = 'Panamatik.HP35'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'classic35' }
  @{ Folder = 'HP-45'; Project = 'HP45.csproj'; Assembly = 'Panamatik.HP45'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'classic45' }
  @{ Folder = 'HP-55'; Project = 'HP55.csproj'; Assembly = 'Panamatik.HP55'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'classic55' }
  @{ Folder = 'HP-65'; Project = 'HP65_105.csproj'; Assembly = 'Panamatik.HP65'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'skip' }
  @{ Folder = 'HP-70'; Project = 'HP70.csproj'; Assembly = 'Panamatik.HP70'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'classic45' }
  @{ Folder = 'HP-80'; Project = 'HP80.csproj'; Assembly = 'Panamatik.HP80'; FormRel = 'HPCLASSIC\HPClassic.cs'; Namespace = 'HPCLASSIC'; Variant = 'classic45' }
  @{ Folder = 'HP-21'; Project = 'HP21.csproj'; Assembly = 'Panamatik.HP21'; FormRel = 'HP25\HP25.cs'; Namespace = 'HP25'; Variant = 'woodstock' }
  @{ Folder = 'HP-22'; Project = 'HP22.csproj'; Assembly = 'Panamatik.HP22'; FormRel = 'HP25\HP25.cs'; Namespace = 'HP25'; Variant = 'woodstock' }
  @{ Folder = 'HP-25'; Project = 'HP25.csproj'; Assembly = 'Panamatik.HP25'; FormRel = 'HP25\HP25.cs'; Namespace = 'HP25'; Variant = 'woodstock' }
  @{ Folder = 'HP-27'; Project = 'HP27.csproj'; Assembly = 'Panamatik.HP27'; FormRel = 'HP25\HP25.cs'; Namespace = 'HP25'; Variant = 'woodstock' }
  @{ Folder = 'HP-29'; Project = 'HP29.csproj'; Assembly = 'Panamatik.HP29'; FormRel = 'HP25\HP25.cs'; Namespace = 'HP25'; Variant = 'woodstock' }
  @{ Folder = 'HP-31'; Project = 'HP31E.csproj'; Assembly = 'Panamatik.HP31'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-32'; Project = 'HP32E.csproj'; Assembly = 'Panamatik.HP32'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-33'; Project = 'HP33.csproj'; Assembly = 'Panamatik.HP33'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-34'; Project = 'HP34C.csproj'; Assembly = 'Panamatik.HP34'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-37'; Project = 'HP37E.csproj'; Assembly = 'Panamatik.HP37'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-38'; Project = 'HP38E.csproj'; Assembly = 'Panamatik.HP38'; FormRel = 'HPSpice\HPSpice.cs'; Namespace = 'HPSpice'; Variant = 'spice' }
  @{ Folder = 'HP-67BE'; Project = 'HP67_104.csproj'; Assembly = 'Panamatik.HP67'; FormRel = 'HP67\HP67.cs'; Namespace = 'HP67'; Variant = 'hp67' }
  @{ Folder = 'HP-19'; Project = 'HP19C.csproj'; Assembly = 'Panamatik.HP19'; FormRel = 'HP19C\HP19C.cs'; Namespace = 'HP19C'; Variant = 'hp19c' }
  @{ Folder = 'HP-01 Stainless Steel'; Project = 'HP01StainlessSteel.csproj'; Assembly = 'Panamatik.HP01'; FormRel = 'HP01\HP01.cs'; Namespace = 'HP01'; Variant = 'hp01' }
)

foreach ($model in $models) {
  $modelRoot = Join-Path $Root $model.Folder
  $projectPath = Join-Path $modelRoot $model.Project
  if (-not (Test-Path $projectPath)) {
    throw "Missing project: $projectPath"
  }

  [xml]$doc = Get-Content -Path $projectPath -Raw
  $assemblySet = $false
  foreach ($group in @($doc.Project.PropertyGroup)) {
    if ($null -ne $group.AssemblyName) {
      $group.AssemblyName = $model.Assembly
      $assemblySet = $true
    }
  }

  if (-not $assemblySet) {
    $group = $doc.CreateElement('PropertyGroup')
    $assemblyNode = $doc.CreateElement('AssemblyName')
    $assemblyNode.InnerText = $model.Assembly
    [void]$group.AppendChild($assemblyNode)
    $firstItemGroup = @($doc.Project.ItemGroup)[0]
    if ($null -ne $firstItemGroup) {
      [void]$doc.Project.InsertBefore($group, $firstItemGroup)
    }
    else {
      [void]$doc.Project.AppendChild($group)
    }
  }

  $doc.Save($projectPath)
  Write-Host "AssemblyName $($model.Assembly) -> $projectPath"

  $snapshotPath = Join-Path $modelRoot ($model.Namespace + '\PanamatikHeadlessSnapshot.cs')
  if (-not (Test-Path $snapshotPath)) {
    Set-Content -Path $snapshotPath -Value (Get-SnapshotSource $model.Namespace) -Encoding UTF8
    Write-Host "Added snapshot $snapshotPath"
  }

  if ($model.Variant -eq 'skip') {
    continue
  }

  $formPath = Join-Path $modelRoot $model.FormRel
  if (-not (Test-Path $formPath)) {
    throw "Missing form: $formPath"
  }

  $content = Get-Content -Path $formPath -Raw
  if ($content -match 'HeadlessPowerOn') {
    Write-Host "Headless API already present: $formPath"
    continue
  }

  $marker = "`r`n`tprivate void timer1_Tick"
  if ($content -notmatch [regex]::Escape($marker.Trim())) {
    $marker = "`n`tprivate void timer1_Tick"
  }

  $insertAt = $content.IndexOf('private void timer1_Tick')
  if ($insertAt -lt 0) {
    throw "timer1_Tick not found in $formPath"
  }

  $headless = Get-HeadlessBlock $model.Variant
  $updated = $content.Insert($insertAt, $headless + "`r`n`t")
  Set-Content -Path $formPath -Value $updated -NoNewline -Encoding UTF8
  Write-Host "Injected headless API ($($model.Variant)) -> $formPath"
}

Write-Host 'Panamatik runtime setup complete.'
