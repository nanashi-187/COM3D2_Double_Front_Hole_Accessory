param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,
    [string]$OutputPath = (Join-Path $PSScriptRoot 'bin\COM3D2.DoubleFrontHole.dll'),
    [string]$CscPath
)

$ErrorActionPreference = 'Stop'

# Prefer Roslyn from Visual Studio Build Tools; fall back to the .NET Framework
# compiler that ships with Windows. The sources stay C# 5 compatible for that.
if (-not $CscPath) {
    $candidates = @(
        'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe')
    )
    $CscPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $CscPath) { throw 'No C# compiler found. Pass -CscPath.' }
}

$managed = Join-Path $GamePath 'COM3D2x64_Data\Managed'
$bepInEx = Join-Path $GamePath 'BepInEx\core'
$sybaris = Join-Path $GamePath 'Sybaris'
$sources = Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter *.cs | ForEach-Object FullName
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null

& $CscPath /nologo /noconfig /target:library /optimize+ /out:$OutputPath `
  /nostdlib+ `
  /reference:"$managed\mscorlib.dll" `
  /reference:"$managed\System.dll" `
  /reference:"$managed\System.Core.dll" `
  /reference:"$bepInEx\BepInEx.dll" `
  /reference:"$bepInEx\0Harmony.dll" `
  /reference:"$managed\Assembly-CSharp.dll" `
  /reference:"$managed\UnityEngine.dll" `
  /reference:"$sybaris\COM3D2.ExternalSaveData.Managed.dll" `
  $sources

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host $OutputPath
