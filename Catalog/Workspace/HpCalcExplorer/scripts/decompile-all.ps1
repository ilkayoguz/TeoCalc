param(
  [string]$Model,
  [switch]$AllExes,
  [string]$SourceRoot,
  [string]$OutputRoot,
  [string]$IlSpyCmd = "ilspycmd"
)

$ErrorActionPreference = "Stop"
$workspaceRoot = Split-Path $PSScriptRoot -Parent
$panamatikRoot = if ($SourceRoot) { $SourceRoot } else { Join-Path $workspaceRoot "Reference/Panamatik" }
$outRoot = if ($OutputRoot) { $OutputRoot } else { Join-Path $workspaceRoot "Reference/Decompiled/Panamatik" }

if (-not (Get-Command $IlSpyCmd -ErrorAction SilentlyContinue)) {
  throw "ilspycmd not found. Install: dotnet tool install -g ilspycmd --version 10.1.0.8386"
}

if (-not (Test-Path $panamatikRoot)) {
  New-Item -ItemType Directory -Path $panamatikRoot -Force | Out-Null
  Write-Host "Created $panamatikRoot - add one folder per model (e.g. HP-65/) with its exe and assets."
  exit 0
}

function Get-ModelFolderName {
  param([string]$Name)
  if ($Name -match '^HP-') { return $Name }
  if ($Name -match '^HP(\d)') { return "HP-$($Matches[1])$($Name.Substring($Matches[0].Length))" }
  return $Name
}

function Get-PrimaryExe {
  param(
    [System.IO.FileInfo[]]$Candidates,
    [string]$ModelFolder
  )

  if ($Candidates.Count -eq 1) { return $Candidates[0] }

  $modelToken = ($ModelFolder -replace '-', '').ToUpperInvariant()
  $filtered = $Candidates | Where-Object {
    $base = [System.IO.Path]::GetFileNameWithoutExtension($_.Name).ToUpperInvariant()
    $base -notmatch '101$' -and $base -notlike '*STAINLESS*'
  }
  if ($filtered.Count -eq 0) { $filtered = $Candidates }

  $byPrefix = @($filtered | Where-Object {
    [System.IO.Path]::GetFileNameWithoutExtension($_.Name).ToUpperInvariant().StartsWith($modelToken)
  })
  if ($byPrefix.Count -ge 1) {
    return $byPrefix | Sort-Object Length -Descending | Select-Object -First 1
  }

  return $filtered | Sort-Object Length -Descending | Select-Object -First 1
}

$modelDirs = if ($Model) {
  $folderName = Get-ModelFolderName $Model
  $path = Join-Path $panamatikRoot $folderName
  if (-not (Test-Path $path)) { throw "Model folder not found: $path" }
  @(Get-Item $path)
} else {
  Get-ChildItem -Path $panamatikRoot -Directory | Sort-Object Name
}

if ($modelDirs.Count -eq 0) {
  Write-Host "No model folders in $panamatikRoot"
  exit 0
}

New-Item -ItemType Directory -Path $outRoot -Force | Out-Null
$decompiled = 0

foreach ($modelDir in $modelDirs) {
  $exes = @(Get-ChildItem -Path $modelDir.FullName -Filter "*.exe" -File | Sort-Object Name)
  if ($exes.Count -eq 0) {
    Write-Warning "Skip $($modelDir.Name): no .exe"
    continue
  }

  $targets = if ($AllExes) { $exes } else { @(Get-PrimaryExe -Candidates $exes -ModelFolder $modelDir.Name) }

  foreach ($exe in $targets) {
    $outDir = Join-Path $outRoot $modelDir.Name
    if ($AllExes -or $targets.Count -gt 1) {
      $outDir = Join-Path $outDir ([System.IO.Path]::GetFileNameWithoutExtension($exe.Name))
    }
    if (Test-Path $outDir) {
      Remove-Item -Recurse -Force $outDir
    }

    Write-Host "Decompiling $($modelDir.Name)/$($exe.Name) -> $outDir"
    & $IlSpyCmd -p --nested-directories -r $modelDir.FullName -o $outDir $exe.FullName
    if ($LASTEXITCODE -ne 0) { throw "ilspycmd failed for $($modelDir.Name)/$($exe.Name)" }
    $decompiled++
  }
}

Write-Host "Done. $decompiled assembly(ies) -> $outRoot"
