# 🔄 BPM (Business Process Management) — Araştırma Notu

> **Not:** Bu doküman AgentPlatform projesiyle doğrudan ilgili değildir. İleride bir BPM / onay akışı projesi için referans amaçlı hazırlanmıştır.

---

## BPM Nedir?

İş süreçlerinin modellenmesi, otomatikleştirilmesi ve izlenmesi. Özellikle **insan onayı gerektiren uzun süreli akışlar** (satın alma onayı, izin talebi, değişiklik yönetimi vb.) için kullanılır.

## BPM vs Workflow Otomasyon (n8n gibi)

| Özellik | BPM | Workflow Otomasyon (n8n) |
|---|---|---|
| İnsan onayı bekleme | ✅ Günlerce bekler | ❌ Tasarım gereği yok |
| Görev atama (kullanıcıya) | ✅ Kullanıcılara task atar | ❌ Yok |
| Onay formu / UI | ✅ Hazır | ❌ Yok |
| BPMN 2.0 standardı | ✅ | ❌ |
| SLA / eskalasyon | ✅ | ❌ |
| Paralel onay | ✅ (3'ten 2'si onaylarsa) | ❌ |
| Audit trail | ✅ Detaylı | ⚠️ Sınırlı |
| Sistem entegrasyonları | Sınırlı | ✅ 400+ |
| API otomasyonu | Sınırlı | ✅ Güçlü |

**Sonuç:** Farklı amaçlara hizmet ederler. Birlikte de kullanılabilir.

---

## BPM Araç Karşılaştırması

### 🥇 Camunda (Önerilen — Enterprise)

| Özellik | Detay |
|---|---|
| BPMN 2.0 | ✅ Tam destek |
| Human Tasks | ✅ Görev atama + onay formu |
| DMN (karar tabloları) | ✅ |
| REST API | ✅ Güçlü |
| Self-host | ✅ Docker |
| Lisans | Community → Apache 2.0 (ücretsiz) |
| Dili | Java |
| Topluluk | Çok büyük |
| Uygun için | Karmaşık onay akışları, kurumsal süreçler |

### 🥈 Flowable

| Özellik | Detay |
|---|---|
| BPMN 2.0 | ✅ Tam destek |
| Human Tasks | ✅ |
| CMMN + DMN | ✅ |
| Self-host | ✅ Docker |
| Lisans | Apache 2.0 |
| Dili | Java (Spring Boot entegrasyonu kolay) |
| Avantajı | Camunda'dan daha hafif |

### 🥉 Elsa Workflows (.NET)

| Özellik | Detay |
|---|---|
| BPMN | ❌ Kendi formatı (görsel editör var) |
| Human Tasks | ✅ Temel destek |
| Self-host | ✅ NuGet paketi — .NET projesine gömülür |
| Lisans | MIT |
| Dili | .NET / C# |
| Avantajı | .NET projelerine doğrudan embed, ayrı servis gerektirmez |
| Dezavantajı | Camunda kadar olgun değil |

### Diğer Alternatifler

| Araç | Açıklama |
|---|---|
| **Activiti** | Flowable'ın önceki versiyonu, hâlâ kullanılıyor |
| **jBPM** | Red Hat destekli, Java tabanlı BPM |
| **ProcessMaker** | Low-code BPM platformu |
| **Bonita** | Açık kaynak BPM, form builder dahil |

---

## Seçim Rehberi

| Senaryo | Önerilen Araç |
|---|---|
| Kurumsal, karmaşık BPMN süreçleri | **Camunda** |
| Hafif, Spring Boot projesi | **Flowable** |
| .NET projesi, basit onay akışları | **Elsa Workflows** |
| Sadece form + onay, kod yazmadan | **Bonita** veya **ProcessMaker** |

---

## Örnek Onay Akışı (BPMN)

```
[Başlat] → [Talep Formu] → [Yönetici Onayı] → ◇ Karar?
                                                  ├── Onay → [Finans Onayı] → ◇ Karar?
                                                  │                            ├── Onay → [Uygula] → [Bitir]
                                                  │                            └── Red → [Bildirim] → [Bitir]
                                                  └── Red → [Bildirim] → [Bitir]
```

Her adımda:
- Görev ilgili kullanıcıya/role atanır
- Kullanıcı onay/red formunu doldurur
- SLA süresi aşılırsa otomatik eskalasyon
- Tüm adımlar audit trail'de kaydedilir
