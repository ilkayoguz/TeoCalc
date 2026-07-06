$root = Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik'
$root = (Resolve-Path $root).Path

Get-ChildItem $root -Recurse -Filter '*.csproj' | ForEach-Object {
  [xml]$doc = Get-Content -Path $_.FullName -Raw
  foreach ($group in @($doc.Project.PropertyGroup)) {
    if ($group.OutputType) {
      $group.OutputType = 'Library'
    }

    if ($group.Prefer32Bit) {
      $null = $group.RemoveChild($group.Prefer32Bit)
    }

    if ($group.PlatformTarget) {
      $null = $group.RemoveChild($group.PlatformTarget)
    }
  }

  $doc.Save($_.FullName)
  Write-Host "Library + AnyCPU -> $($_.FullName)"
}

Write-Host 'Panamatik library output configured.'
