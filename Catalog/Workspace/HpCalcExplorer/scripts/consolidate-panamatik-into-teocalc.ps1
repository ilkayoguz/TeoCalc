# Copies all Panamatik decompiled sources into TeoCalc.Panamatik/Sources/{model}/
# with per-model namespaces (Panamatik.Calc.HP65, ...) for single-assembly build.
param(
  [string]$SourceRoot = (Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik'),
  [string]$TargetRoot = (Join-Path $PSScriptRoot '..\..\..\..\TeoCalc.Panamatik\Sources')
)

$ErrorActionPreference = 'Stop'
$SourceRoot = (Resolve-Path $SourceRoot).Path
$TargetRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\TeoCalc.Panamatik')).Path + '\Sources'

$models = @(
  @{ Catalog = 'HP-01 Stainless Steel'; Id = 'HP01' }
  @{ Catalog = 'HP-19'; Id = 'HP19' }
  @{ Catalog = 'HP-21'; Id = 'HP21' }
  @{ Catalog = 'HP-22'; Id = 'HP22' }
  @{ Catalog = 'HP-25'; Id = 'HP25' }
  @{ Catalog = 'HP-27'; Id = 'HP27' }
  @{ Catalog = 'HP-29'; Id = 'HP29' }
  @{ Catalog = 'HP-31'; Id = 'HP31' }
  @{ Catalog = 'HP-32'; Id = 'HP32' }
  @{ Catalog = 'HP-33'; Id = 'HP33' }
  @{ Catalog = 'HP-34'; Id = 'HP34' }
  @{ Catalog = 'HP-35'; Id = 'HP35' }
  @{ Catalog = 'HP-37'; Id = 'HP37' }
  @{ Catalog = 'HP-38'; Id = 'HP38' }
  @{ Catalog = 'HP-45'; Id = 'HP45' }
  @{ Catalog = 'HP-55'; Id = 'HP55' }
  @{ Catalog = 'HP-65'; Id = 'HP65' }
  @{ Catalog = 'HP-67BE'; Id = 'HP67' }
  @{ Catalog = 'HP-70'; Id = 'HP70' }
  @{ Catalog = 'HP-80'; Id = 'HP80' }
)

$excludeFiles = @(
  'Program.cs'
  'AssemblyInfo.cs'
  'Resources.cs'
  'Settings.cs'
  'GlobalUsings.g.cs'
  '.AssemblyAttributes.cs'
)

if (Test-Path $TargetRoot) {
  Remove-Item -Path $TargetRoot -Recurse -Force
}

New-Item -Path $TargetRoot -ItemType Directory | Out-Null

foreach ($model in $models) {
  $sourceDir = Join-Path $SourceRoot $model.Catalog
  $targetDir = Join-Path $TargetRoot $model.Id
  if (-not (Test-Path $sourceDir)) {
    throw "Missing source folder: $sourceDir"
  }

  New-Item -Path $targetDir -ItemType Directory | Out-Null
  $namespace = "Panamatik.Calc.$($model.Id)"

  Get-ChildItem -Path $sourceDir -Recurse -File | Where-Object {
    ($_.Extension -in '.cs', '.kml', '.png', '.jpg', '.bmp', '.gif', '.ico', '.resx', '.mlist', '.bin') -and
    ($_.FullName -notmatch '\\obj\\|\\bin\\')
  } | ForEach-Object {
    $relative = $_.FullName.Substring($sourceDir.Length).TrimStart('\')
    foreach ($skip in $excludeFiles) {
      if ($relative.EndsWith($skip)) { return }
    }

    $dest = Join-Path $targetDir $relative
    $destParent = Split-Path $dest -Parent
    if (-not (Test-Path $destParent)) {
      New-Item -Path $destParent -ItemType Directory -Force | Out-Null
    }

    if ($_.Extension -eq '.cs') {
      $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
      $content = $content -replace '(?m)^namespace\s+[\w\.]+\s*;', "namespace $namespace;"
      $content = $content -replace 'private System\.Windows\.Forms\.Timer', 'private global::System.Windows.Forms.Timer'
      Set-Content -Path $dest -Value $content -NoNewline -Encoding UTF8
    }
    else {
      Copy-Item -Path $_.FullName -Destination $dest -Force
    }
  }

  Write-Host "Consolidated $($model.Id) -> $targetDir"
}

Write-Host "Panamatik consolidation complete: $TargetRoot"
