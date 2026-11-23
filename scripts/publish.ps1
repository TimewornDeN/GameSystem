# Builds release packages of GameSystem into the dist folder (Windows, PowerShell).
# Usage:  .\scripts\publish.ps1            (runtime: win-x64)
#         .\scripts\publish.ps1 -Runtime win-arm64
param(
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist | Out-Null

# 1. All tests must pass before anything is packaged.
dotnet test GameSystem.slnx -c Release
if ($LASTEXITCODE -ne 0) { throw "Tests failed, nothing was published." }

# 2. Self-contained single-file builds: the user does not need to install .NET.
$targets = @(
    @{ Project = "GameSystem.CLI/GameSystem.CLI.csproj"; Name = "GameSystem-Console" },
    @{ Project = "GameSystem.Gui/GameSystem.Gui.csproj"; Name = "GameSystem-Window" }
)
foreach ($t in $targets) {
    $out = Join-Path $dist "$($t.Name)-$Version-$Runtime"
    dotnet publish $t.Project -c Release -r $Runtime --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version -o $out
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $($t.Project)" }
    Copy-Item README.md, "docs/DESIGN.md" -Destination $out
    Compress-Archive -Path "$out/*" -DestinationPath "$out.zip" -Force
}
Write-Host "Done. Packages are in: $dist"
