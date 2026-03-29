# Kurumsal Uygulama Modernizasyonu — Referans Rehber

> **Konu:** Legacy .NET Framework + MS SQL → Modern .NET + PostgreSQL dönüşümü
> **Tarih:** 2026-03-28
> **Tür:** Strateji ve planlama referansı

---

## Bağlam

- Çok sayıda monolitik legacy web uygulaması (.NET Full Framework)
- MS SQL Server veritabanları — ciddi yapısal sorunlar
- Dokümantasyon yok
- Hedef: modüler yeni mimari + PostgreSQL geçişi
- Klasik araçlarla tahmini süre: 1+ yıl

---

## Dönüşüm Fazları

### Faz 0: Keşif ve Dokümantasyon (AI ile)
- AI'a mevcut kodları okutarak otomatik modül/servis dokümantasyonu çıkar
- DB şemasını oku → tablo ilişkileri, SP'ler, view'lar otomatik dokümanla
- API endpoint kontratını çıkar
- Dead code / kullanılmayan tablo-kolon-SP tespiti

### Faz 1: Hedef Mimari Tasarımı
- Mevcut kodu analiz ederek **bounded context** önerisi (AI destekli)
- Modüller arası dependency graph oluştur
- **Modüler Monolit** yaklaşımı → ileride opsiyonel microservices

### Faz 2: Veritabanı Stratejisi

**Önerilen: Strangler Fig Pattern**
```
Eski DB olduğu gibi kalsın
  → Yeni modüller yeni şemayla (PostgreSQL) başlasın
  → Sync/view katmanı ile eski DB'den okusun
  → Kademeli veri taşıma
  → Son modül geçince eski DB emekli
```

### Faz 3: Kod Üretimi (AI ile hız artışı)

| İş | Tahmini AI Hız Artışı |
|----|----------------------|
| CRUD endpoint'ler | 5-10x |
| DTO/ViewModel mapping | 10x |
| DB migration script'leri | 3-5x |
| Unit test yazma | 5-8x |
| SP → EF Core dönüşümü | 3-5x |
| API dokümantasyonu | 10x |

### Faz 4: Kademeli Geçiş
- En bağımsız modülden başla → production'a al → doğrula → sonraki modül

---

## Mevcut DB İyileştirme (Yeni Projeye Geçmeden Önce)

### Katman 1: Sıfır Risk (Hemen Yapılabilir)
- Eksik indeksler ekle
- Kullanılmayan tablo/SP temizliği
- Kolon tipi optimizasyonları

### Katman 2: Düşük Risk (Dikkatli Yapılabilir)
- Eksik FK constraint'ler
- Gereksiz SP'leri view'a çevir
- Partition ekleme (büyük tablolar)

### Katman 3: Ön Hazırlık (Yeni Proje İçin)
- Mevcut şemanın tam dokümantasyonu (AI ile)
- Her tablo için "ideal yapı nasıl olmalı" dokümanı
- Eski → yeni tablo mapping dokümanı
- Migration script taslakları

**Gerçekçi hedef:** Mevcut uygulamada **%40-50 iyileştirme** yapılabilir. %100 ideal yapı ancak yeni projede mümkün.

---

## MS SQL → PostgreSQL Geçiş Notları

| Konu | Etki |
|------|------|
| Stored Procedure → PL/pgSQL | ⚠️ Her SP yeniden yazılmalı |
| Trigger'lar | ⚠️ Syntax farklı |
| View'lar | ✅ Çoğu uyumlu |
| Data tipleri (nvarchar→text, datetime→timestamptz) | ⚠️ Mapping gerekli |
| Identity → SERIAL/GENERATED | ⚠️ Syntax farkı |
| Full-text search | ⚠️ tsvector yaklaşımı farklı |

---

## Önerilen Sıralama

```
1. Mevcut DB'yi AI ile tam dokümanla
2. Katman 1-2 iyileştirmelerini yap
3. Yeni proje için ideal şemayı PostgreSQL olarak tasarla
4. Strangler Fig: yeni modüller PostgreSQL ile başlasın
5. Eski modüller geçtikçe veri migration
6. Son modül geçince MS SQL emekli
```
