$root = Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik'
$root = (Resolve-Path $root).Path

Get-ChildItem $root -Recurse -Include HPClassic.cs, HP25.cs, HPSpice.cs, HP67.cs, HP01.cs | ForEach-Object {
  $content = Get-Content -Path $_.FullName -Raw
  $updated = $content
  $updated = $updated.Replace("`t`ttimermode = false;`r`n", '')
  $updated = $updated.Replace("`t`ttimermode = false;`n", '')
  $updated = $updated.Replace("`t`t`tact_grp);", "`t`t`t0);")
  if ($updated -ne $content) {
    Set-Content -Path $_.FullName -Value $updated -NoNewline -Encoding utf8
    Write-Host "Fixed $($_.FullName)"
  }
}

Write-Host 'Headless patch fixes complete.'
