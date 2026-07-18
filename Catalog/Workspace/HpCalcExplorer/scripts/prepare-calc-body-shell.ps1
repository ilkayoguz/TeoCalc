# Crops calc-body-layout-draft.png to calculator shell; keeps prototype chrome.
param(
  [string]$ScriptDir = $PSScriptRoot
)

$py = Join-Path $ScriptDir 'prepare-calc-body-shell.py'
python $py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
