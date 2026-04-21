# 16e — Release Management — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Durum:** Henüz başlanmadı

---

## 📋 Yapılacaklar

### Entity'ler
- [ ] `Release` — release tanımı (versiyon, tarih, durum)
- [ ] `ReleaseItem` — release'e dahil WorkItem'lar (M:N)
- [ ] `GoNoGoChecklist` — Go/No-Go kontrol listesi
- [ ] `GoNoGoItem` — kontrol listesi maddesi (Dev/QA/Ops/Security)
- [ ] `ReleaseNote` — otomatik üretilen release notu

### Release Akışı
- [ ] Release lifecycle: Planning → Code Freeze → Testing → Go/No-Go → Deployed
- [ ] Release'e WorkItem atama (sprint veya manuel seçim)
- [ ] Release branch / tag entegrasyonu (opsiyonel)

### Go/No-Go
- [ ] Go/No-Go kontrol listesi (Dev/QA/Ops/Security kategorileri)
- [ ] Her kategoriden onay gereksinimi
- [ ] Checklist tamamlanma durumu dashboard'u

### Release Note
- [ ] Release note otomatik üretimi (WorkItem'lardan)
- [ ] Markdown formatında export
- [ ] Kategori bazlı gruplama (Feature, Bug Fix, Improvement)
