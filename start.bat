@echo off
echo ====================================
echo  EntApp.Framework - Startup Script
echo ====================================
echo.

echo [1/3] Starting Docker services...
docker compose up -d
echo.

echo [2/3] Starting Backend (http://localhost:5212)...
start "EntApp Backend" cmd /k "cd /d %~dp0 && dotnet run --project src\Host\EntApp.WebAPI"
echo.

echo [3/3] Starting Frontend (http://localhost:3000)...
start "EntApp Frontend" cmd /k "cd /d %~dp0\src\Frontend\entapp-web && npm run dev"
echo.

echo ====================================
echo  All services started!
echo ====================================
echo.
echo  Backend API:  http://localhost:5212
echo  Swagger:      http://localhost:5212/swagger
echo  Frontend:     http://localhost:3000
echo  Seq Logs:     http://localhost:5341
echo  RabbitMQ:     http://localhost:15672
echo ====================================
timeout /t 5
