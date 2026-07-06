# Copies all Panamatik decompiled sources into TeoCalc.Panamatik/Engines/{EngineKey}/
# with unique namespaces so every calculator compiles inside ONE project.
param(
  [string]$SourceRoot = (Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik'),
  [string]$TargetRoot = (Join-Path $PSScriptRoot '..\..\..\TeoCalc.Panamatik\Engines')
)

$ErrorActionPreference = 'Stop'
$SourceRoot = (Resolve-Path $SourceRoot).Path
$TargetRoot = (Resolve-Path (Split-Path $TargetRoot -Parent)).Path + '\Engines'

$models = @(
  @{ Folder = 'HP-01 Stainless Steel'; Key = 'HP01'; Ns = 'HP01'; Form = 'HP01' }
  @{ Folder = 'HP-19'; Key = 'HP19'; Ns = 'HP19C'; Form = 'HP19C' }
  @{ Folder = 'HP-21'; Key = 'HP21'; Ns = 'HP25'; Form = 'HP25' }
  @{ Folder = 'HP-22'; Key = 'HP22'; Ns = 'HP25'; Form = 'HP25' }
  @{ Folder = 'HP-25'; Key = 'HP25'; Ns = 'HP25'; Form = 'HP25' }
  @{ Folder = 'HP-27'; Key = 'HP27'; Ns = 'HP25'; Form = 'HP25' }
  @{ Folder = 'HP-29'; Key = 'HP29'; Ns = 'HP25'; Form = 'HP25' }
  @{ Folder = 'HP-31'; Key = 'HP31'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-32'; Key = 'HP32'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-33'; Key = 'HP33'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-34'; Key = 'HP34'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-37'; Key = 'HP37'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-38'; Key = 'HP38'; Ns = 'HPSpice'; Form = 'HPSpice' }
  @{ Folder = 'HP-35'; Key = 'HP35'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
  @{ Folder = 'HP-45'; Key = 'HP45'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
  @{ Folder = 'HP-55'; Key = 'HP55'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
  @{ Folder = 'HP-65'; Key = 'HP65'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
  @{ Folder = 'HP-67BE'; Key = 'HP67'; Ns = 'HP67'; Form = 'HP67' }
  @{ Folder = 'HP-70'; Key = 'HP70'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
  @{ Folder = 'HP-80'; Key = 'HP80'; Ns = 'HPCLASSIC'; Form = 'HPClassic' }
)

function Rewrite-Source([string]$Content, [string]$SourceNs, [string]$TargetNs, [string]$FormClass) {
  $updated = $Content
  $updated = $updated.Replace("namespace ${SourceNs}.Properties;", "namespace ${TargetNs}.Properties;")
  $updated = $updated.Replace("namespace ${SourceNs};", "namespace ${TargetNs};")
  $updated = $updated.Replace("using ${SourceNs};", "using ${TargetNs};")
  $updated = $updated.Replace("ResourceManager(""${SourceNs}.Properties.Resources", "ResourceManager(""${TargetNs}.Properties.Resources")
  $updated = $updated.Replace("typeof(${SourceNs}.${FormClass})", "typeof(${FormClass})")
  $updated = $updated.Replace("typeof(${SourceNs}.Properties.Resources)", "typeof(Properties.Resources)")
  return $updated
}

if (Test-Path $TargetRoot) {
  Remove-Item -Path $TargetRoot -Recurse -Force
}

New-Item -Path $TargetRoot -ItemType Directory | Out-Null

foreach ($model in $models) {
  $sourceDir = Join-Path $SourceRoot $model.Folder
  $targetDir = Join-Path $TargetRoot $model.Key
  $targetNs = "TeoCalc.Panamatik.Engines.$($model.Key)"

  if (-not (Test-Path $sourceDir)) {
    throw "Missing source folder: $sourceDir"
  }

  New-Item -Path $targetDir -ItemType Directory | Out-Null

  Get-ChildItem -Path $sourceDir -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\obj\\|\\bin\\|\\\.vs\\' -and
    $_.Name -notin @('Program.cs', 'AssemblyInfo.cs')
  } | ForEach-Object {
    $relative = $_.FullName.Substring($sourceDir.Length + 1)
    $destPath = Join-Path $targetDir $relative
    $destFolder = Split-Path $destPath -Parent
    if (-not (Test-Path $destFolder)) {
      New-Item -Path $destFolder -ItemType Directory -Force | Out-Null
    }

    if ($_.Extension -eq '.cs') {
      $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
      $content = Rewrite-Source $content $model.Ns $targetNs $model.Form
      [System.IO.File]::WriteAllText($destPath, $content)
    }
    else {
      Copy-Item -Path $_.FullName -Destination $destPath -Force
    }
  }

  Write-Host "Consolidated $($model.Key) <- $($model.Folder)"
}

Write-Host "Consolidation complete -> $TargetRoot"
