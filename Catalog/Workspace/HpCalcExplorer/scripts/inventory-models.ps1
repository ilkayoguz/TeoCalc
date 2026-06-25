#Requires -Version 5.1
param(
  [string]$SourceRoot,
  [string]$OutFile
)

$ErrorActionPreference = "Stop"
$workspaceRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$panamatikRoot = if ($SourceRoot) { $SourceRoot } else { Join-Path $workspaceRoot "Reference/Panamatik New" }

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
  throw "ilspycmd not found."
}

$rows = New-Object System.Collections.Generic.List[object]

Get-ChildItem -Path $panamatikRoot -Directory | Sort-Object Name | ForEach-Object {
  $exes = @(Get-ChildItem -Path $_.FullName -Filter "*.exe" -File)
  if ($exes.Count -eq 0) {
    $rows.Add([PSCustomObject]@{
        Model     = $_.Name
        Exe       = "(none)"
        Namespaces = ""
        MainClass = ""
        Notes     = "missing exe"
      })
    return
  }

  foreach ($exe in $exes) {
    $listed = @(ilspycmd -l c $exe.FullName 2>&1)
    $namespaces = @($listed | Where-Object { $_ -match "^Class ([^\.]+)\." } | ForEach-Object {
        if ($_ -match "^Class ([^\.]+)\.") { $Matches[1] }
      } | Sort-Object -Unique)
    $main = @($listed | Where-Object {
        $_ -match "^Class \w+\.(HP\w+|HPClassic|HPSpice)" -and $_ -notmatch "Program|Properties"
      } | Select-Object -First 1)
    $mainName = if ($main) { ($main[0] -replace "^Class ", "") } else { "" }

    $notes = ""
    if ($namespaces.Count -gt 1) {
      $notes = "multiple namespaces in one exe (see Readme)"
    }

    $rows.Add([PSCustomObject]@{
        Model      = $_.Name
        Exe        = $exe.Name
        Namespaces = ($namespaces -join ", ")
        MainClass  = $mainName
        Notes      = $notes
      })
  }
}

$table = $rows | Format-Table -AutoSize | Out-String
Write-Output $table

if ($OutFile) {
  $dir = Split-Path $OutFile -Parent
  if ($dir -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
  }
  $rows | Export-Csv -Path $OutFile -NoTypeInformation -Encoding UTF8
  Write-Host "Wrote $OutFile"
}
