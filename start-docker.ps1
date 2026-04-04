Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Docker Services Starting..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot
docker compose up -d

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host " Docker services ready!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  PostgreSQL : localhost:5432"
Write-Host "  Redis      : localhost:6379"
Write-Host "  Seq Logs   : http://localhost:5341"
Write-Host "  RabbitMQ   : http://localhost:15672 (guest/guest)"
Write-Host "=====================================" -ForegroundColor Green
