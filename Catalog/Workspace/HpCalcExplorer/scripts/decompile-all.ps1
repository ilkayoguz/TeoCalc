#Requires -Version 7.0
param(
  [string]$Model,
  [string]$IlSpyCmd = "ilspycmd"
)

$ErrorActionPreference = "Stop"
$workspaceRoot = Split-Path $PSScriptRoot -Parent
$exeDir = Join-Path $workspaceRoot "Reference/Exe"
$outRoot = Join-Path $workspaceRoot "Reference/Decompiled"

if (-not (Get-Command $IlSpyCmd -ErrorAction SilentlyContinue)) {
  throw "ilspycmd not found. Install: dotnet tool install -g ilspycmd --version 10.1.0.8386"
}

if (-not (Test-Path $exeDir)) {
  New-Item -ItemType Directory -Path $exeDir -Force | Out-Null
  Write-Host "Created $exeDir — drop HP *.exe files here."
  exit 0
}

$exes = if ($Model) {
  $path = Join-Path $exeDir "$Model.exe"
  if (-not (Test-Path $path)) { throw "Not found: $path" }
  @(Get-Item $path)
} else {
  Get-ChildItem -Path $exeDir -Filter "*.exe" | Sort-Object Name
}

if ($exes.Count -eq 0) {
  Write-Host "No exes in $exeDir"
  exit 0
}

New-Item -ItemType Directory -Path $outRoot -Force | Out-Null

foreach ($exe in $exes) {
  $name = [System.IO.Path]::GetFileNameWithoutExtension($exe.Name)
  $outDir = Join-Path $outRoot $name
  if (Test-Path $outDir) {
    Remove-Item -Recurse -Force $outDir
  }
  Write-Host "Decompiling $($exe.Name) -> $outDir"
  & $IlSpyCmd -p --nested-directories -o $outDir $exe.FullName
  if ($LASTEXITCODE -ne 0) { throw "ilspycmd failed for $($exe.Name)" }
}

Write-Host "Done. $($exes.Count) assembly(ies) -> $outRoot"
