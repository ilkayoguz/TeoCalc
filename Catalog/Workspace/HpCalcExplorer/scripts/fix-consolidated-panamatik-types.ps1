# Fix stale typeof() references after namespace consolidation.
param(
  [string]$SourcesRoot = (Join-Path $PSScriptRoot '..\..\..\..\TeoCalc.Panamatik\Sources')
)

$ErrorActionPreference = 'Stop'
$SourcesRoot = (Resolve-Path $SourcesRoot).Path

$replacements = @(
  @{ Pattern = 'typeof\(HPCLASSIC\.HPClassic\)'; Replacement = 'typeof(HPClassic)' }
  @{ Pattern = 'typeof\(global::HP67\.HP67\)'; Replacement = 'typeof(HP67)' }
  @{ Pattern = 'typeof\(HP67\.HP67\)'; Replacement = 'typeof(HP67)' }
  @{ Pattern = 'typeof\(HPSpice\.HPSpice\)'; Replacement = 'typeof(HPSpice)' }
  @{ Pattern = 'typeof\(HP25\.HP25\)'; Replacement = 'typeof(HP25)' }
  @{ Pattern = 'typeof\(HP19C\.HP19C\)'; Replacement = 'typeof(HP19C)' }
  @{ Pattern = 'typeof\(global::HPSpice\.HPSpice\)'; Replacement = 'typeof(HPSpice)' }
  @{ Pattern = 'typeof\(global::HP25\.HP25\)'; Replacement = 'typeof(HP25)' }
  @{ Pattern = 'typeof\(global::HP19C\.HP19C\)'; Replacement = 'typeof(HP19C)' }
  @{ Pattern = 'typeof\(global::HP01\.HP01\)'; Replacement = 'typeof(HP01)' }
)

Get-ChildItem -Path $SourcesRoot -Recurse -Filter '*.cs' | ForEach-Object {
  $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
  $updated = $content
  foreach ($rule in $replacements) {
    $updated = $updated -replace $rule.Pattern, $rule.Replacement
  }

  if ($updated -ne $content) {
    Set-Content -Path $_.FullName -Value $updated -NoNewline -Encoding UTF8
    Write-Host "Fixed $($_.FullName)"
  }
}

Write-Host "Consolidated type references updated."
