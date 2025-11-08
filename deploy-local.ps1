# Deploy Jellio Plugin to Local Jellyfin Server
# Run this script after building to deploy to your local server

$SERVER = "192.168.0.125"
$JELLYFIN_PLUGINS_PATH = "\\$SERVER\c$\ProgramData\Jellyfin\Server\plugins\Jellio"
$BUILD_PATH = ".\Jellyfin.Plugin.Jellio\bin\Release\net9.0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying Jellio Plugin to $SERVER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if build exists
if (-not (Test-Path "$BUILD_PATH\Jellyfin.Plugin.Jellio.dll")) {
    Write-Host "ERROR: Build not found! Run 'dotnet build' first." -ForegroundColor Red
    exit 1
}

# Check if server is accessible
Write-Host "Checking server accessibility..." -ForegroundColor Yellow
if (-not (Test-Connection -ComputerName $SERVER -Count 1 -Quiet)) {
    Write-Host "ERROR: Cannot reach server at $SERVER" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Server is reachable" -ForegroundColor Green
Write-Host ""

# Create plugin directory if it doesn't exist
Write-Host "Creating plugin directory if needed..." -ForegroundColor Yellow
if (-not (Test-Path $JELLYFIN_PLUGINS_PATH)) {
    New-Item -ItemType Directory -Path $JELLYFIN_PLUGINS_PATH -Force | Out-Null
    Write-Host "✓ Created directory: $JELLYFIN_PLUGINS_PATH" -ForegroundColor Green
}
else {
    Write-Host "✓ Directory exists: $JELLYFIN_PLUGINS_PATH" -ForegroundColor Green
}
Write-Host ""

# Copy files
Write-Host "Copying plugin files..." -ForegroundColor Yellow
try {
    Copy-Item "$BUILD_PATH\Jellyfin.Plugin.Jellio.dll" -Destination $JELLYFIN_PLUGINS_PATH -Force
    Copy-Item "$BUILD_PATH\Jellyfin.Plugin.Jellio.deps.json" -Destination $JELLYFIN_PLUGINS_PATH -Force
    Copy-Item "$BUILD_PATH\Jellyfin.Plugin.Jellio.xml" -Destination $JELLYFIN_PLUGINS_PATH -Force
    Write-Host "✓ Files copied successfully" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to copy files: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Restart Jellyfin service on $SERVER" -ForegroundColor White
Write-Host "2. Navigate to https://jellyfin.westhomeserver.com/jellio" -ForegroundColor White
Write-Host "3. Test the localStorage configuration feature" -ForegroundColor White
Write-Host ""
Write-Host "To restart Jellyfin remotely, run:" -ForegroundColor Yellow
Write-Host "Invoke-Command -ComputerName $SERVER -ScriptBlock { Restart-Service JellyfinServer }" -ForegroundColor Cyan
Write-Host ""
