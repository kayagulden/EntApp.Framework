Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Backend Starting..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  URL: http://localhost:5212" -ForegroundColor Yellow
Write-Host "  Swagger: http://localhost:5212/swagger" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot
dotnet run --project src\Host\EntApp.WebAPI
