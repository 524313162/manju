param(
    [string]$OutputDir = ".\publish\ComfyuiProxy.Web",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Publishing ComfyuiProxy.Web (net8.0) ===" -ForegroundColor Cyan
dotnet publish "$root\src\Web\ComfyuiProxy.Web\ComfyuiProxy.Web.csproj" `
    -c $Configuration `
    -o $OutputDir `
    --self-contained false
if (-not $?) { throw "Publish failed" }

Write-Host "=== Publish completed ===" -ForegroundColor Green
Write-Host "Output: $OutputDir"
Write-Host ""

Write-Host "=== Starting ComfyuiProxy.Web on http://localhost:8288 ===" -ForegroundColor Cyan
$env:ASPNETCORE_URLS = "http://localhost:8288"
$env:ASPNETCORE_ENVIRONMENT = "Production"
& "$OutputDir\ComfyuiProxy.Web.exe"
