# Upgrades decompiled Panamatik WinForms projects to net481 for dotnet build.
param(
  [string]$Root = (Join-Path $PSScriptRoot '..\Reference\Decompiled\Panamatik')
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path

$packageBlock = @'
  <ItemGroup>
    <PackageReference Include="System.Resources.Extensions" Version="10.0.0" />
  </ItemGroup>
'@

Get-ChildItem -Path $Root -Recurse -Filter '*.csproj' | ForEach-Object {
  $path = $_.FullName
  [xml]$doc = Get-Content -Path $path -Raw
  $propertyGroups = @($doc.Project.PropertyGroup)

  foreach ($group in $propertyGroups) {
    if ($group.TargetFramework) {
      $group.TargetFramework = 'net481'
    }

    if ($group.LangVersion) {
      $group.LangVersion = 'latest'
    }
  }

  $firstGroup = $propertyGroups | Where-Object { $_.TargetFramework } | Select-Object -First 1
  if ($null -eq $firstGroup.GenerateResourceUsePreserializedResources) {
    $resourceNode = $doc.CreateElement('GenerateResourceUsePreserializedResources')
    $resourceNode.InnerText = 'true'
    [void]$firstGroup.AppendChild($resourceNode)
  }
  else {
    $firstGroup.GenerateResourceUsePreserializedResources = 'true'
  }

  $hasPackage = $false
  foreach ($itemGroup in @($doc.Project.ItemGroup)) {
    foreach ($package in @($itemGroup.PackageReference)) {
      if ($package.Include -eq 'System.Resources.Extensions') {
        $hasPackage = $true
      }
    }
  }

  if (-not $hasPackage) {
    $itemGroup = $doc.CreateElement('ItemGroup')
    $package = $doc.CreateElement('PackageReference')
    $package.SetAttribute('Include', 'System.Resources.Extensions')
    $package.SetAttribute('Version', '10.0.0')
    [void]$itemGroup.AppendChild($package)
    [void]$doc.Project.AppendChild($itemGroup)
  }

  $doc.Save($path)
  Write-Host "Upgraded $path"
}

Get-ChildItem -Path $Root -Recurse -Filter '*.cs' | ForEach-Object {
  $content = Get-Content -Path $_.FullName -Raw
  if ($content -notmatch 'private Timer timer1;') {
    return
  }

  $updated = $content -replace 'private Timer timer1;', 'private System.Windows.Forms.Timer timer1;'
  if ($updated -ne $content) {
    Set-Content -Path $_.FullName -Value $updated -NoNewline
    Write-Host "Fixed Timer in $($_.FullName)"
  }
}

Write-Host "Panamatik upgrade complete."
