Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Frontend Starting..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  URL: http://localhost:3000" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Set-Location "$PSScriptRoot\src\Frontend\entapp-web"
npm run dev
