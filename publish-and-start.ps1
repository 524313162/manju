param(
    [string]$OutputDir = ".\publish",
    [string]$ManjuPort = "8010",
    [string]$ProxyPort = "8288",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Publishing ManjuCraft.Web (net10.0) ===" -ForegroundColor Cyan
$manjuPublish = Join-Path $OutputDir "ManjuCraft.Web"
dotnet publish "$root\src\Web\ManjuCraft.Web\ManjuCraft.Web.csproj" `
    -c $Configuration `
    -o $manjuPublish `
    --self-contained false
if (-not $?) { throw "ManjuCraft.Web publish failed" }

Write-Host "=== Publishing ComfyuiProxy.Web (net8.0) ===" -ForegroundColor Cyan
$proxyPublish = Join-Path $OutputDir "ComfyuiProxy.Web"
dotnet publish "$root\src\Web\ComfyuiProxy.Web\ComfyuiProxy.Web.csproj" `
    -c $Configuration `
    -o $proxyPublish `
    --self-contained false
if (-not $?) { throw "ComfyuiProxy.Web publish failed" }

Write-Host "=== Publishing completed successfully ===" -ForegroundColor Green
Write-Host "ManjuCraft.Web -> $manjuPublish"
Write-Host "ComfyuiProxy.Web -> $proxyPublish"
Write-Host ""
Write-Host "To start services, run:"
Write-Host "  .\start-production.ps1"
