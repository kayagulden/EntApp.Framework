# WaitForAssignment Activity — Tasarım Notu

## Problem

Mevcut workflow akışında `RouteToQueue → WaitForStatusDecision` şeklinde ilerliyor.
Bu durumda ticket henüz **kimseye atanmamışken** (kuyrukta beklerken) kullanıcı doğrudan
"Çözüldü", "İptal", "Eskale Et" gibi status kararları verebiliyor.

Bu, gerçek bir ITSM sürecinde **yanlış davranış** — birine atanmadan status değiştirilememelidir.

## Önerilen Akış

```
RouteToQueue → WaitForAssignment → WaitForStatusDecision → Done
```

| Adım | Activity | Davranış |
|------|----------|----------|
| 1 | `RouteToQueue` | Ticket kuyruğa yönlendirilir |
| 2 | **`WaitForAssignment`** (YENİ) | Blocking — biri ticket'ı üstlenene kadar bekler |
| 3 | `WaitForStatusDecision` | Atanan kişi karar verir (Çözüldü, İptal, Eskale Et) |

## WaitForAssignment Activity Tasarımı

### Tip
- **Blocking activity** (bookmark oluşturur)
- `WaitForApprovalActivity` ve `WaitForStatusDecisionActivity` pattern'ini takip eder

### Input'lar
- `TicketId` (Guid) — Beklenecek ticket
- `Label` (string) — UI'da gösterilecek etiket (ör: "Atama Bekliyor")
- `AutoSetInProgress` (bool, default: true) — Atandığında status otomatik `InProgress` yapılsın mı

### Output'lar
- `AssignedUserId` (Guid) — Atanan kullanıcı ID'si
- `AssignedUserName` (string) — Atanan kullanıcı adı

### Bookmark Payload
```csharp
public sealed record AssignmentBookmarkPayload(
    Guid TicketId,
    string Label,
    bool AutoSetInProgress);
```

### Resume Mekanizması
İki olası yaklaşım:

#### A) Event-driven (önerilen)
- `AssignTicketCommand` çalıştığında bir `TicketAssignedEvent` publish edilir
- Yeni bir `WorkflowAssignmentHandler` bu event'i dinler
- Handler, ilgili workflow instance'ının bookmark'ını `IWorkflowDispatcher` ile resume eder
- Bu yaklaşım mevcut `WorkflowTaskCompletionHandler` pattern'ini takip eder

#### B) Aksiyon-driven
- Frontend "Aksiyonlar" panelinde kuyruk üyelerini listeleyen bir dropdown gösterir
- Kullanıcı bir kişi seçip "Ata" butonuna tıklar
- Bu hem AssignTicketCommand'ı çalıştırır hem de bookmark'ı resume eder

### Frontend UI Değişiklikleri
- `WaitForAssignment` activity tipi için Aksiyonlar panelinde:
  - Kuyruk üyelerini dropdown olarak göster
  - "Ata ve Devam Et" butonu
  - Veya mevcut "Üstlen" butonu ile otomatik self-assign

### Mevcut Referans Dosyalar
- Activity pattern: `WaitForStatusDecisionActivity.cs`
- Handler pattern: `WorkflowTaskCompletionHandler.cs`
- Bookmark persist: `context.SetProperty("ResolvedTicketId", ticketId)`
- Resume: `IWorkflowDispatcher.DispatchAsync(DispatchWorkflowInstanceRequest, null, ct)`

### Elsa Designer Workflow JSON Örneği
```json
{
  "activities": [
    { "type": "EntApp.RouteToQueueActivity", "id": "route" },
    { "type": "EntApp.WaitForAssignmentActivity", "id": "wait-assign" },
    { "type": "EntApp.WaitForStatusDecisionActivity", "id": "wait-decision" }
  ],
  "connections": [
    { "source": { "activity": "route", "port": "Done" }, "target": { "activity": "wait-assign" } },
    { "source": { "activity": "wait-assign", "port": "Done" }, "target": { "activity": "wait-decision" } }
  ]
}
```

## Tarih
- Tasarım: 2026-04-15
- Durum: Tasarım aşamasında, implementasyon bekliyor
