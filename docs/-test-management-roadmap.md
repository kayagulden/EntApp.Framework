# 16d — Test Management — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Durum:** Henüz başlanmadı

---

## 📋 Yapılacaklar

### Entity'ler
- [ ] `TestScenario` — test senaryosu (adımlar, ön koşullar, beklenen sonuç)
- [ ] `TestStep` — senaryo adımı (sıralı)
- [ ] `TestPlan` — test planı (sprint/release bazlı)
- [ ] `TestPlanScenario` — plan ↔ senaryo M:N ilişki
- [ ] `TestExecution` — test çalıştırma kaydı (Pass/Fail/Blocked + notlar)

### Test Planı & Çalıştırma
- [ ] Test planı oluşturma + senaryo atama
- [ ] Test execution: Pass/Fail/Blocked sonuç kaydetme
- [ ] Fail durumunda otomatik Bug oluşturma (→ WorkItem[Bug])
- [ ] Test çalıştırma geçmişi ve trend analizi

### Test Coverage
- [ ] Gereksinim bazlı coverage raporu
- [ ] Sprint bazlı coverage raporu
- [ ] Release bazlı coverage raporu
- [ ] Coverage dashboard widget
