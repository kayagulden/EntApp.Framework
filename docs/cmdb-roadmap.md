# CMDB (Configuration Management Database) — Yol Haritası

> **Tarih:** 2026-04-20  
> **Modül:** TaskManagement (`pm` schema)  
> **UI Konumu:** Tenant Yönetimi → `/manage/cmdb/`  
> **İlgili Roadmap:** [delivery-platform-roadmap.md](file:///c:/Users/kaya/projects/EntApp.Framework/docs/delivery-platform-roadmap.md)

---

## Faz A — Domain Entities & TPT Inheritance ✅

> **Tamamlanma:** 2026-04-20

- [x] `ConfigurationItemBase` — TPT base entity (Name, Code, Description, Status, Criticality, Owner)
- [x] `ApplicationBase` — ApplicationType, TechnologyStack, RepositoryUrl, CurrentVersion
- [x] `ServerCI` — ServerType, Environment, OS, IpAddress, Hostname, CPU/RAM/Disk, DataCenter
- [x] `DatabaseCI` — DatabaseEngine, Version, Port, SizeGB, ConnectionString, BackupSchedule
- [x] `LicenceCI` — LicenceType, Vendor, ProductName, MaxUsers, ExpirationDate, AnnualCost
- [x] `CIRelationship` — Directed graph (SourceCIId, TargetCIId, RelationType, Notes)
- [x] `CIRelationType` enum — RunsOn, DependsOn, Hosts, ConnectsTo, ManagedBy, LicensedBy

**Çıktı:** TPT inheritance ile CI tiplerine ait tablolar (`pm.servers`, `pm.databases`, `pm.licences`, `pm.ci_relationships`).

---

## Faz B — Backend CQRS & API ✅

> **Tamamlanma:** 2026-04-20

- [x] Commands: CreateServer, UpdateServer, CreateDatabase, UpdateDatabase, CreateLicence, UpdateLicence
- [x] Commands: AddCIRelationship, RemoveCIRelationship
- [x] Queries: ListServers, GetServer, ListDatabases, GetDatabase, ListLicences, GetLicence
- [x] Queries: ListCIRelationships (with direction: incoming/outgoing)
- [x] Handlers — EF Core LINQ, strongly-typed ID projections
- [x] 14 API endpoint (CRUD + relationship management)
- [x] Request DTOs with FluentValidation

**Çıktı:** Tam CRUD + ilişki yönetimi API'si.

---

## Faz C — Frontend UI ✅

> **Tamamlanma:** 2026-04-20

- [x] List pages — Applications, Servers, Databases, Licences (with create modals, filtering, status badges)
- [x] Detail pages — 4 CI type (inline editing, status flow, hardware resource cards)
- [x] `CIRelationshipsPanel` — Reusable component (cross-type search, add/remove, incoming/outgoing)
- [x] Manage sidebar — CMDB section with all CI types + disabled "Ağ Cihazları (Yakında)"
- [x] Dashboard sidebar — CMDB section removed (CMDB is admin-only)
- [x] Design themes — Application (cyan), Server (violet), Database (emerald), Licence (amber)

**Çıktı:** Tenant Yönetimi altında tam işlevsel CMDB yönetim arayüzü.

---

## Faz D — NetworkDeviceCI 📋

> **Durum:** Menüde "Yakında" olarak eklendi, backend implemente edilmedi

- [ ] `NetworkDeviceCI` entity — DeviceType (Router, Switch, Firewall, WAP, LoadBalancer), Manufacturer, Model, FirmwareVersion, ManagementIP, PortCount, VlanSupport, SnmpCommunity
- [ ] Domain entity + TPT table (`pm.network_devices`)
- [ ] Commands: CreateNetworkDevice, UpdateNetworkDevice
- [ ] Queries: ListNetworkDevices, GetNetworkDevice
- [ ] API endpoints (4 endpoint: list, get, create, update)
- [ ] Frontend list + detail page (tema: rose/pink)
- [ ] Manage sidebar'da "Ağ Cihazları" enable

---

## Faz E — CI Silme & Soft Delete 📋

- [ ] DeleteCI command (soft delete — `IsDeleted = true`, Status → Retired)
- [ ] Silme öncesi ilişki kontrolü (bağlı CI varsa uyarı/onay)
- [ ] Cascade relationship removal (silinen CI'ın ilişkileri de kaldırılır)
- [ ] Frontend: silme butonu + onay dialogu (detail page'de)
- [ ] Çöp kutusu / geri yükleme (opsiyonel)

---

## Faz F — CI Topoloji Görselleştirme 📋

- [ ] Topoloji API endpoint — `GET /api/pm/ci/topology` (graph data: nodes + edges)
- [ ] Frontend: interaktif topoloji haritası (React Flow veya D3.js)
  - [ ] Node'lar: CI tipi ikonları + status renkleri
  - [ ] Edge'ler: ilişki tipi etiketleri + yön okları
  - [ ] Zoom, pan, fit-to-screen
  - [ ] Node tıklama → CI detay sayfasına git
  - [ ] Filtre: CI tipine göre, ortama göre
- [ ] CMDB Dashboard'a topoloji widget'ı ekleme

---

## Faz G — CMDB Dashboard & İstatistikler 📋

- [ ] CMDB ana dashboard sayfası (`/manage/cmdb`)
- [ ] Toplam CI sayısı (tip bazlı kırılım) — stat cards
- [ ] Toplam ilişki sayısı (tip bazlı kırılım)
- [ ] Status dağılımı (pie chart: Active, Planned, Deprecated, Retired)
- [ ] Kritiklik dağılımı (bar chart)
- [ ] Son eklenen CI'lar listesi (son 10)
- [ ] Lisans süresi dolmak üzere olan CI'lar (alert list)
- [ ] Mini topoloji haritası (en bağlantılı CI'lar)

---

## Faz H — Bulk Import / Export 📋

- [ ] CI toplu yükleme — Excel/CSV → CI oluşturma
- [ ] Import template indirme (her CI tipi için ayrı şablon)
- [ ] Import preview + validation (hata gösterimi)
- [ ] CI toplu dışa aktarma — Excel/CSV
- [ ] İlişki toplu yükleme (source_code, target_code, relation_type)

---

## Faz I — CI Change History & Audit 📋

- [ ] CI değişiklik geçmişi endpoint — `GET /api/pm/ci/{id}/history`
- [ ] AuditLog modülü entegrasyonu (mevcut altyapıyı kullan)
- [ ] Frontend: detail page'de "Geçmiş" tab'ı
  - [ ] Tarih/kullanıcı bazlı filtreleme
  - [ ] Değişiklik diff gösterimi (eski → yeni değer)

---

## Faz J — CI Discovery & Auto-Populate (İleri Aşama) 📋

> **Not:** Bu faz ileri seviye otomasyon gerektirir.

- [ ] Active Directory / LDAP entegrasyonu — sunucu keşfi
- [ ] SNMP/WMI agent — network device keşfi
- [ ] Database connection string scan — DB keşfi
- [ ] Keşfedilen CI'ları onay kuyruğuna ekleme
- [ ] Periyodik tarama (Hangfire job)

---

## Faz K — CMDB & Ticket Entegrasyonu (Derinleştirme) 📋

- [ ] Ticket oluştururken CI seçimi iyileştirmesi (mevcut dropdown → arama + tip filtre)
- [ ] Ticket detayında bağlı CI bilgisi gösterimi
- [ ] CI detayında ilgili ticket listesi (mevcut kısmen var, Application için)
- [ ] CI bazlı SLA tanımlama (kritik CI → daha kısa SLA)
- [ ] CI etki analizi — bir CI çöktüğünde etkilenen tüm CI'ları bul (ilişki grafiği traverse)

---

## Özet Tablo

| Faz | Başlık | Durum | Tahmini Süre |
|-----|--------|-------|-------------|
| A | Domain Entities & TPT | ✅ Tamamlandı | — |
| B | Backend CQRS & API | ✅ Tamamlandı | — |
| C | Frontend UI | ✅ Tamamlandı | — |
| D | NetworkDeviceCI | 📋 Planlandı | 1 gün |
| E | CI Silme & Soft Delete | 📋 Planlandı | 0.5 gün |
| F | CI Topoloji Görselleştirme | 📋 Planlandı | 2-3 gün |
| G | CMDB Dashboard & İstatistikler | 📋 Planlandı | 1-2 gün |
| H | Bulk Import / Export | 📋 Planlandı | 1-2 gün |
| I | CI Change History & Audit | 📋 Planlandı | 1 gün |
| J | CI Discovery & Auto-Populate | 📋 İleri Aşama | 1-2 hafta |
| K | CMDB & Ticket Entegrasyonu | 📋 İleri Aşama | 2-3 gün |

> [!NOTE]
> Faz A–C tamamlanmıştır. Faz D–I kısa vadede, Faz J–K uzun vadede yapılabilir.
> Süre tahminleri AI-assisted geliştirme ile hesaplanmıştır.
