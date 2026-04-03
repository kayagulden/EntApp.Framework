# EntApp.Framework — Dinamik UI Oluşturma

> **Tarih:** 2026-03-28  
> **Durum:** Tasarım tartışması

---

## 1. Konsept

Framework'te iki tür ekran vardır:

| Tür | Oluşturma | Örnek |
|-----|-----------|-------|
| **Auto-generated** | Entity metadata'dan otomatik | Lookup tabloları, konfigürasyon, basit CRUD |
| **Custom** | Geliştirici tarafından elle | Dashboard, karmaşık iş akışı, multi-step form |

Hedef: Basit CRUD'lar için **sıfır frontend kodu yazılmasın**. Backend'de entity tanımla → UI otomatik oluşsun.

---

## 2. Mimari

```
Backend (metadata sağlayıcı)              Frontend (render engine)
┌───────────────────────┐                 ┌──────────────────────┐
│ EntityMeta attribute  │  GET /meta/{e}  │ DynamicPage          │
│ veya Fluent API ile   │ ──────────────► │  ├── DynamicTable     │
│ entity tanımı         │  JSON schema    │  ├── DynamicForm      │
│                       │                 │  └── DynamicDetail    │
│ Field, Relation,      │                 │                      │
│ Validation, Layout    │                 │ shadcn/ui + TanStack  │
└───────────────────────┘                 └──────────────────────┘
```

> [!NOTE]
> **Orval vs Dynamic UI:** Orval (OpenAPI → TypeScript) **Custom (Level 4) sayfalar** ve non-CRUD API'ler için tip güvenliği sağlar. Dynamic UI ise **Auto-generated (Level 1-3)** CRUD ekranları için metadata API'den runtime tip alır. `useDynamicCrud.ts` hook'u Orval ile üretilen Axios instance'ı kullanır, ancak metadata'yı kendi endpoint'inden alır.

---

## 3. Backend — Entity Metadata Tanımı

### Attribute Yaklaşımı (Veri & Validasyon)

> [!IMPORTANT]
> **Mimari Kural:** C# entity'si üzerindeki attributelar sadece **veritabanı, tip ve validasyon** kurallarını belirtir. Kolon sırası (order), listede görünürlük (showInList), grid genişliği (width) gibi saf UI konfigürasyonları C# kodunda tutulmaz. Bu sayede UI değişikliği için Backend derlemesi gerekmez. UI konfigürasyonları `DynamicUIConfigs` tablosundan veya JSON'dan çekilerek metadata'ya (API yanıtında) birleştirilir.

```csharp
[DynamicEntity("Country", menuGroup: "Tanımlar")]
public class Country : BaseEntity
{
    [DynamicField(required: true, maxLength: 3)]
    public string Code { get; set; }

    [DynamicField(required: true, searchable: true)]
    public string Name { get; set; }

    [DynamicField(defaultValue: true)]
    public bool IsActive { get; set; }
}
```

### Master-Detail

```csharp
[DynamicEntity("SalesOrder", menuGroup: "Satış")]
public class SalesOrder : AuditableEntity
{
    [DynamicField(readOnly: true)]
    public string OrderNumber { get; set; }

    [DynamicField(fieldType: FieldType.Date)]
    public DateTime OrderDate { get; set; }

    [DynamicLookup(typeof(Customer))]
    public Guid CustomerId { get; set; }

    public OrderStatus Status { get; set; }

    [DynamicDetail(typeof(OrderItem))]
    public List<OrderItem> Items { get; set; }
}

[DynamicEntity("OrderItem", isDetail: true)]
public class OrderItem : BaseEntity
{
    [DynamicLookup(typeof(Product))]
    public Guid ProductId { get; set; }

    [DynamicField(fieldType: FieldType.Decimal)]
    public decimal Quantity { get; set; }

    [DynamicField(fieldType: FieldType.Money)]
    public decimal UnitPrice { get; set; }

    [DynamicField(fieldType: FieldType.Money, computed: "Quantity * UnitPrice", readOnly: true)]
    public decimal LineTotal { get; set; }
}
```

### UI Konfigürasyon Yönetimi (Admin Panel / Veritabanı)

Yukarıdaki entity'lerin ekranda nasıl görüneceği (Etiketler, sıra, gizlilik) `DynamicUIConfigs` veritabanı tablosunda veya bir JSON dosyasında tutulur. Bu sayede admin panelinden "Müşteri kodunu listede 1. sıraya al" dendiğinde kod değişmez:

```json
// Örnek DB veya JSON Konfigürasyonu (Country)
{
  "entity": "Country",
  "title": "Ülkeler",
  "icon": "globe",
  "fields": [
    { "name": "Code", "label": "Ülke Kodu", "order": 1, "width": "sm", "showInList": true },
    { "name": "Name", "label": "Ülke Adı", "order": 2, "showInList": true },
    { "name": "IsActive", "label": "Aktif", "order": 3, "showInList": false }
  ]
}
```

### UI Konfigürasyon Fallback Mekanizması

Bir entity için `DynamicUIConfigs` kaydı yoksa sistem otomatik çalışmaya devam eder:

```
Fallback Sırası:
1. DB/JSON konfigürasyonu (admin düzenlemiş) → varsa bunu kullan
2. Convention-based otomatik → property adından label, property sırasından order, 
   string/number ise showInList: true
3. Attribute'tan gelen → [DynamicField] parametreleri override olarak
```

Bu sayede yeni bir entity eklendiğinde hiçbir konfigürasyon yapmadan çalışan bir ekran elde edilir. Admin panelden ince ayar yapmak isteğinde DB konfigürasyonu oluşturulur.

### Metadata API Endpoint (Birleştirilmiş Sonuç)

```
GET /api/meta/SalesOrder

Response:
{
  "entity": "SalesOrder",
  "title": "Siparişler",
  "icon": "shopping-cart",
  "menuGroup": "Satış",
  "fields": [
    {
      "name": "orderNumber", "label": "Sipariş No", "type": "string",
      "readOnly": true, "showInList": true
    },
    {
      "name": "orderDate", "label": "Tarih", "type": "date",
      "showInList": true
    },
    {
      "name": "customerId", "label": "Müşteri", "type": "lookup",
      "lookup": { "entity": "Customer", "displayField": "name", 
                  "endpoint": "/api/customer" },
      "showInList": true
    },
    {
      "name": "status", "label": "Durum", "type": "enum",
      "options": ["Draft", "Confirmed", "Shipped", "Delivered", "Cancelled"],
      "showInList": true
    }
  ],
  "details": [
    {
      "name": "items", "label": "Kalemler", "entity": "OrderItem",
      "fields": [ ... ]
    }
  ],
  "actions": {
    "create": true, "edit": true, "delete": true, "export": true
  }
}
```

---

## 4. Frontend — Render Engine

### Component Hiyerarşisi

```
DynamicPage (route: /dynamic/:entityName)
├── DynamicToolbar          (Yeni Ekle, Dışa Aktar, Filtre butonları)
├── DynamicTable             (liste görünümü — TanStack Table + shadcn)
│   ├── sort, filter, pagination (metadata'dan otomatik)
│   ├── showInList=true alanları kolon olarak
│   └── satır tıklama → DynamicForm (edit modu)
├── DynamicForm              (oluşturma/düzenleme dialogu — shadcn Sheet/Dialog)
│   ├── field.type → shadcn Input/Select/DatePicker/Switch/...
│   ├── field.lookup → async combobox (arama destekli)
│   ├── field.computed → hesaplanan, readOnly
│   ├── validation → Zod schema (metadata'dan üretilir)
│   └── DynamicDetailTable   (master-detail alt tablo)
│       ├── inline edit (satır içi düzenleme)
│       └── satır ekle/sil
├── DynamicFilters           (gelişmiş filtreleme paneli)
└── DynamicBulkActions       (toplu işlem: seçili satırları sil, durum değiştir, dışa aktar)
```

### Field Type → Component Mapping

| FieldType | shadcn/ui Component | Davranış |
|-----------|---------------------|----------|
| `string` | `<Input>` | maxLength validation |
| `text` | `<Textarea>` | Çok satırlı |
| `number` | `<Input type="number">` | min/max |
| `decimal` | `<Input>` + format | Ondalık, binlik ayracı |
| `money` | `<Input>` + currency | Para birimi prefix |
| `date` | `<DatePicker>` | Takvim popup |
| `datetime` | `<DateTimePicker>` | Tarih + saat |
| `boolean` | `<Switch>` | Toggle |
| `enum` | `<Select>` | Options listesi |
| `lookup` | `<Combobox>` (async) | API'den arama + seçim |
| `file` | `<FileUpload>` | Dosya yükleme |
| `richtext` | `<RichTextEditor>` | Zengin metin |

---

## 5. Generic CRUD API Entegrasyonu

Dynamic UI, Business Framework'teki generic CRUD handler'larla doğrudan çalışır:

```
DynamicTable → GET    /api/{entity}?page=1&size=20&sort=name
DynamicForm  → POST   /api/{entity}          (create)
             → PUT    /api/{entity}/{id}     (update)
             → DELETE /api/{entity}/{id}     (delete)
Lookup       → GET    /api/{entity}/lookup?search=abc
Export       → GET    /api/{entity}/export?format=xlsx
```

Backend'de her `[DynamicEntity]` için otomatik controller registration:

```csharp
// Framework otomatik keşif
// DynamicEntity attribute'ü olan her entity için CRUD endpoint oluşturur
app.MapDynamicCrudEndpoints();  // tek satır, tüm entity'ler register
```

---

## 6. UI Piramidi (4 Seviye)

```
        ▲
       /  \        Level 4: CUSTOM
      / el \       Tamamen özel React sayfası
     /  ile  \     (dashboard, wizard, karmaşık iş akışı)
    /  yazılır \
   ──────────────
   │  Level 3  │   HYBRID
   │ DynamicUI │   Meta + özel component override
   │ + custom  │   (meta tanımlı ama bazı alanlar özel render)
   ──────────────
   │  Level 2  │   CONFIGURED
   │ DynamicUI │   Meta + JSON konfigürasyon ile özelleştirilmiş
   │ + config  │   (kolon sırası, gizli alanlar, özel butonlar)
   ──────────────
   │  Level 1  │   AUTO-GENERATED
   │ DynamicUI │   Sıfır frontend kodu
   │  sıfır    │   Attribute'dan otomatik CRUD ekranı
   │   kod     │   (lookup, konfigürasyon, referans tabloları)
   ──────────────
```

**Override mekanizması:**

```tsx
// Level 1: Tamamen otomatik — sadece route tanımla
<DynamicPage entity="Country" />

// Level 2: Konfigürasyon ile özelleştir
<DynamicPage entity="Customer" 
  config={{ 
    hiddenFields: ["internalCode"],
    customColumns: { name: { width: 300 } },
    toolbar: { export: true, import: true }
  }} 
/>

// Level 3: Belirli alanları özel component ile render et
<DynamicPage entity="SalesOrder"
  fieldOverrides={{
    status: (value, onChange) => <StatusBadge value={value} onChange={onChange} />,
  }}
  detailOverrides={{
    items: (items, onChange) => <CustomOrderItemGrid items={items} onChange={onChange} />,
  }}
/>

// Level 4: Tamamen özel sayfa — DynamicUI kullanılmaz
<CustomDashboardPage />
```

---

## 7. Menu ve Routing (Otomatik)

```csharp
// Backend metadata'dan otomatik menu yapısı
GET /api/meta/menu

Response:
{
  "groups": [
    {
      "name": "Tanımlar",
      "icon": "settings",
      "items": [
        { "entity": "Country", "title": "Ülkeler", "icon": "globe" },
        { "entity": "City", "title": "Şehirler", "icon": "map-pin" },
        { "entity": "Currency", "title": "Para Birimleri", "icon": "banknote" }
      ]
    },
    {
      "name": "Satış",
      "icon": "shopping-cart",
      "items": [
        { "entity": "SalesOrder", "title": "Siparişler", "icon": "file-text" },
        { "entity": "PriceList", "title": "Fiyat Listeleri", "icon": "tag" }
      ]
    }
  ]
}
```

Frontend sidebar bu endpoint'ten menu'yü otomatik oluşturur. Yeni bir `[DynamicEntity]` eklendiğinde sidebar'a otomatik gelir.

---

## 8. Proje Yapısı Etkisi

```
src/Shared/Shared.Infrastructure/
├── DynamicCrud/
│   ├── Attributes/
│   │   ├── DynamicEntityAttribute.cs
│   │   ├── DynamicFieldAttribute.cs
│   │   ├── DynamicLookupAttribute.cs
│   │   └── DynamicDetailAttribute.cs
│   ├── MetadataService.cs           (reflection → JSON schema)
│   ├── DynamicCrudEndpointBuilder.cs (otomatik endpoint registration)
│   └── DynamicMenuBuilder.cs        (otomatik menu üretimi)

frontend/src/
├── components/dynamic/
│   ├── DynamicPage.tsx              (orchestrator)
│   ├── DynamicTable.tsx             (liste)
│   ├── DynamicForm.tsx              (form)
│   ├── DynamicDetailTable.tsx       (master-detail alt tablo)
│   ├── DynamicFilters.tsx           (filtre paneli)
│   ├── DynamicToolbar.tsx           (butonlar)
│   ├── DynamicField.tsx             (field type → component router)
│   ├── DynamicLookup.tsx            (async combobox)
│   ├── DynamicImport.tsx            (Excel/CSV import wizard)
│   └── DynamicExport.tsx            (dışa aktarma dialog)
├── hooks/
│   ├── useDynamicMeta.ts            (metadata fetch + cache)
│   └── useDynamicCrud.ts            (CRUD operations)
└── lib/
    └── schema-to-zod.ts             (metadata → Zod validation)
```

---

## 9. Data Import/Export Engine

Enterprise uygulamalarda en sık gelen talep: **"Excel'den toplu veri yüklemek istiyorum."** Dynamic UI + metadata altyapısı bunu otomatik çözebilir.

### Export (Dışa Aktarma)

Her `DynamicEntity` otomatik export destekler:

```
GET /api/{entity}/export?format=xlsx&columns=name,code,status&filter=isActive:true
```

| Format | Kütüphane |
|--------|-----------|
| Excel (.xlsx) | ClosedXML |
| CSV | CsvHelper |
| PDF | QuestPDF |

### Import (İçe Aktarma)

3 adımlı wizard:

```
Adım 1: Dosya Yükleme
  → Kullanıcı Excel/CSV dosyası seçer
  → Backend: dosyayı parse et, kolon listesini döndür

Adım 2: Kolon Eşleştirme
  → Sol: Excel kolonları  |  Sağ: Entity alanları (metadata'dan)
  → Otomatik eşleştirme önerisi (isim benzerliği)
  → Lookup alanları: "müşteri adı" → Customer tablosunda ara → CustomerId eşle

Adım 3: Önizleme & Doğrulama
  → İlk 10 satır preview
  → Validasyon hataları kırmızı ile gösterilir
  → "Tümünü İçe Aktar" → toplu insert/update
```

**Backend altyapısı:**

```
Shared.Infrastructure/DynamicCrud/
├── Import/
│   ├── DynamicImportService.cs     (parse + validate + bulk insert)
│   ├── ColumnMapper.cs             (Excel kolon → entity field eşleme)
│   └── ImportResult.cs             (başarılı/hatalı satır raporu)
├── Export/
│   ├── DynamicExportService.cs     (entity → Excel/CSV/PDF)
│   └── ExportTemplateBuilder.cs    (boş import şablonu üretimi)
```

**Boş şablon indirme:** Her entity için kolon başlıklarını içeren boş Excel dosyası indirilebilir:
```
GET /api/{entity}/import-template → boş Excel, kolon başlıkları metadata'dan
```

