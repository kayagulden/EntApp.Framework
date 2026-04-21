# 16h — Ek Modüller — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Durum:** Henüz başlanmadı

---

## 📋 Yapılacaklar

### Change Request Management
- [ ] Değişiklik talebi entity (CR — Change Request)
- [ ] Onay akışı (Workflow modülü ile)
- [ ] Etki analizi (impact assessment — hangi CI'lar, projeler, gereksinimler etkileniyor)
- [ ] CR ↔ WorkItem bağlantısı

### Risk Management
- [ ] Risk entity (tanım, kategori, sahip)
- [ ] Risk matrisi (olasılık × etki)
- [ ] Risk azaltma aksiyonları (mitigation plan)
- [ ] Risk dashboard widget

### Automation Rules
- [ ] Rule engine: Trigger → Condition → Action
- [ ] Trigger örnekleri: WorkItem status değişimi, SLA breach, Sprint tamamlanma
- [ ] Action örnekleri: Bildirim gönder, WorkItem ata, Status değiştir
- [ ] UI: kural oluşturma/düzenleme arayüzü

### Developer Tools (Opsiyonel)
- [ ] Git webhook — commit/PR event'lerini WorkItem'a bağlama
- [ ] Commit link — WorkItem numarası üzerinden commit arama
- [ ] VS Code extension (opsiyonel)
