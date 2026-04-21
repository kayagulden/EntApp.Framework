# 16f — Scheduling Engine — Detaylı Yol Haritası

> **Ana roadmap:** [-roadmap.md (Faz 16)](file:///c:/Users/kaya/projects/EntApp.Framework/docs/-roadmap.md#faz-16--delivery-platform-almitsm-mod�lleri)
> **Durum:** Henüz başlanmadı

---

## 📋 Yapılacaklar

### Otomatik Takvim
- [ ] Otomatik takvim hesaplama (bağımlılık, kapasite, öncelik)
- [ ] WorkItem bağımlılık grafiği (predecessor/successor)
- [ ] Kritik yol (Critical Path) analizi

### Metodoloji Bazlı Yerleştirme
- [ ] Scrum: Sprint bazlı yerleştirme (kapasite + velocity bazlı)
- [ ] Kanban: Sürekli akış tahmini (lead time + throughput bazlı)
- [ ] Waterfall: Gantt chart timeline

### Yeniden Hesaplama
- [ ] Periyodik yeniden hesaplama (Hangfire job)
- [ ] Manuel tetikleme (PM tarafından)
- [ ] Kayma tespit + bildirim (hedef tarih aşımı uyarısı)
