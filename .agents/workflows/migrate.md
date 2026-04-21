---
description: DB migration SQL dosyalarını PostgreSQL'e uygular
---

# DB Migration Uygula

// turbo-all

1. Docker servislerinin çalıştığından emin ol:
```bash
docker compose up -d
```

2. Connection string'den DB adını oku ve migration uygula:
```powershell
$cs = (Get-Content src/Host/EntApp.WebAPI/appsettings.Development.json | ConvertFrom-Json).ConnectionStrings.DefaultConnection; $db = [regex]::Match($cs, 'Database=([^;]+)').Groups[1].Value; Write-Host "DB: $db"; Get-Content migrations/20260421_unified_work_item.sql | docker exec -i entapp-postgres psql -U postgres -d $db
```

## Migration Dosyaları

| Dosya | Açıklama |
|-------|----------|
| `migrations/20260421_unified_work_item.sql` | Unified Work Item Model (tablo rename, yeni kolonlar, Sprint tablosu) |

## Manuel Çalıştırma

```bash
docker exec -it entapp-postgres psql -U postgres -d entapp_dev
```

## Notlar

- DB adı `appsettings.Development.json` → `ConnectionStrings.DefaultConnection` → `Database=` değerinden okunur
- Migration dosyaları **idempotent** — birden fazla çalıştırılabilir
- Yeni migration dosyaları `migrations/` klasörüne `YYYYMMDD_aciklama.sql` formatında eklenir
