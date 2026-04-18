# Uygulama Kayıt Defteri & CMDB Yapısı — Tasarım Dokümanı

> **Modül:** TaskManagement (pm schema)  
> **Tarih:** 2026-04-19  
> **Durum:** Faz A — Tamamlandı (CMDB base yapısı ile)

---

## 1. Mimari Karar: CMDB Base Class

### Karar
`ApplicationBase` doğrudan `AuditableEntity`'den türemek yerine, `ConfigurationItemBase` abstract sınıfından türer.
Bu, gelecekte Server, Database, NetworkDevice vb. CI tipleri eklendiğinde aynı yapının kullanılmasını sağlar.

### TPT (Table-Per-Type) Stratejisi
```
configuration_items (base tablo)
├── id, name, code, description
├── status (CIStatus), criticality (CICriticality)
├── owner_user_id, tenant_id
├── created_at, updated_at
│
applications (derived tablo — FK → configuration_items.id)
├── id (FK), application_type
├── tech_lead_user_id, technology_stack
├── repository_url, documentation_url, current_version
│
[gelecek] servers (derived tablo — FK → configuration_items.id)
├── id (FK), os, ip_address, ram_gb, cpu_cores...
```

### Neden?
- Tüm CI'lar tek bir `configuration_items` tablosundan sorgulanabilir
- İlişki tablosu (CIRelationship) tek bir ID tipi (`ConfigurationItemId`) kullanır
- Yeni CI tipi eklemek = sadece entity + derived tablo, base dokunulmaz

---

## 2. Uygulama Nedir?

| Alan | Açıklama |
|---|---|
| **Application** | Şirketin sahip olduğu, kullandığı veya geliştirdiği her türlü yazılım/sistem kaydı |
| **Kalıtım** | `ConfigurationItemBase` → `ApplicationBase` (TPT) |

## 3. Uygulama Tipolojisi

```
Applications
├── InHouse (İç Geliştirme)           → Projenin teslim edilebiliri
├── COTS (Paket Yazılım)              → Tedarik projesinin teslim edilebiliri
├── Infrastructure                     → Altyapı bileşeni
└── Hybrid                             → İç geliştirme + paket entegrasyonu
```

## 4. Entity Modeli

### ConfigurationItemBase (abstract — tüm CI'lar paylaşır)
```
├── Id (ConfigurationItemId)
├── Name, Code, Description
├── Status (CIStatus: Planned, InDevelopment, Active, Deprecated, Retired)
├── Criticality (CICriticality: Low, Medium, High, Critical)
├── OwnerUserId
├── TenantId, CreatedAt, UpdatedAt
```

### ApplicationBase (ConfigurationItemBase'den türer)
```
├── ApplicationType (InHouse, COTS, Infrastructure, Hybrid)
├── TechLeadUserId
├── TechnologyStack
├── RepositoryUrl, DocumentationUrl
├── CurrentVersion
```

## 5. İlişki Haritası

```
Application ←── Ticket.ApplicationId?       → "Bu uygulama hakkında talep"
Application ←── ProjectDeliverable          → "Bu projenin çıktısı"  (many-to-many)
Application ←── Release.ApplicationId       → "Bu uygulamanın v2.4.1'i"
Application ←── TaskItemBase.ApplicationId? → "Bu uygulamadaki feature"
```

## 6. Gelecek CI Tipleri (sadece entity + migration ekle)

| CI Tipi | Ek Alanlar | Tetikleyici |
|---|---|---|
| ServerCI | OS, IP, RAM, CPU, Location | Sunucu envanteri gerektiğinde |
| DatabaseCI | Engine, Version, Size | DB yönetimi gerektiğinde |
| NetworkDeviceCI | Type, Firmware, Port sayısı | Network envanteri gerektiğinde |
| LicenseCI | Vendor, ExpiryDate, Seats | Lisans takibi gerektiğinde |

## 7. CI İlişki Tablosu (gelecek)

```
CIRelationship
├── SourceCIId (ConfigurationItemId)
├── TargetCIId (ConfigurationItemId)
├── RelationType → "runs_on", "depends_on", "connects_to"
```

## 8. Uygulama Fazları

| Faz | İçerik | Bağımlılık | Durum |
|---|---|---|---|
| **Faz A** | CMDB base + Application entity + CRUD + sayfa | Yok | ✅ Tamamlandı |
| **Faz B** | Ticket.ApplicationId + UI'da seçim | Faz A | Bekliyor |
| **Faz C** | ProjectDeliverable ara tablo | Faz A | Bekliyor |
| **Faz D** | Release.ApplicationId (16e ile) | Faz A + 16e | Bekliyor |
| **Faz E** | Backlog.ApplicationId + raporlama | Faz A + Backlog | Bekliyor |
| **Faz F** | CIRelationship tablosu | Faz A | Bekliyor |
