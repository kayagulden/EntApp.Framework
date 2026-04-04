---
description: Backend ve frontend projelerini tek komutla başlatır (docker, backend, frontend)
---

# Projeyi Başlat

// turbo-all

1. Docker servislerini başlat:
```bash
docker compose up -d
```

2. Backend'i başlat (ayrı terminalde):
```bash
dotnet run --project src\Host\EntApp.WebAPI
```

3. Frontend'i başlat (ayrı terminalde):
```bash
cd src\Frontend\entapp-web && npm run dev
```

## Adresler
- **Backend API**: http://localhost:5212
- **Swagger**: http://localhost:5212/swagger
- **Frontend**: http://localhost:3000
- **Seq Logs**: http://localhost:5341
- **RabbitMQ**: http://localhost:15672 (guest/guest)
