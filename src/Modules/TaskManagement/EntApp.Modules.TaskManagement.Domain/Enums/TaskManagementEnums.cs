namespace EntApp.Modules.TaskManagement.Domain.Enums;

/// <summary>Uygulama tipi.</summary>
public enum ApplicationType
{
    /// <summary>İç geliştirme.</summary>
    InHouse = 0,
    /// <summary>Paket / lisanslı / SaaS.</summary>
    COTS = 1,
    /// <summary>Altyapı bileşeni.</summary>
    Infrastructure = 2,
    /// <summary>İç geliştirme + paket entegrasyonu.</summary>
    Hybrid = 3
}

/// <summary>Configuration Item durumu — tüm CI tipleri paylaşır.</summary>
public enum CIStatus
{
    Planned = 0,
    InDevelopment = 1,
    Active = 2,
    Deprecated = 3,
    Retired = 4
}

/// <summary>Configuration Item kritiklik seviyesi.</summary>
public enum CICriticality
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Proje durumu.</summary>
public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>İş kalemi durumu (Kanban).</summary>
public enum WorkItemStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4,
    Cancelled = 5
}

/// <summary>Görev önceliği.</summary>
public enum WorkItemPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Görev / iş kalemi tipi.</summary>
public enum WorkItemType
{
    Task = 0,
    Bug = 1,
    Feature = 2,
    Improvement = 3,
    Epic = 4,
    /// <summary>Kullanıcı hikayesi — Epic/Feature altında.</summary>
    UserStory = 5,
    /// <summary>Teknik borç.</summary>
    TechDebt = 6,
    /// <summary>Araştırma / PoC.</summary>
    Spike = 7,
}

/// <summary>Sprint durumu.</summary>
public enum SprintStatus
{
    Planning = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>Milestone durumu.</summary>
public enum MilestoneStatus
{
    /// <summary>Henüz başlanmadı.</summary>
    Pending = 0,
    /// <summary>Üzerinde çalışılıyor.</summary>
    InProgress = 1,
    /// <summary>Hedefe ulaşıldı.</summary>
    Reached = 2,
    /// <summary>Hedef tarih geçti, ulaşılamadı.</summary>
    Missed = 3,
    /// <summary>İptal edildi.</summary>
    Cancelled = 4
}

/// <summary>Portfolyo durumu.</summary>
public enum PortfolioStatus
{
    Active = 0,
    OnHold = 1,
    Archived = 2
}

/// <summary>Proje metodolojisi.</summary>
public enum ProjectMethodology
{
    Kanban = 0,
    Scrum = 1,
    ScrumBan = 2,
    Waterfall = 3
}

/// <summary>Proje kategorisi — hangi özelliklerin aktif olduğunu belirler.</summary>
public enum ProjectCategory
{
    /// <summary>Genel proje — sadece Task + Milestone.</summary>
    General = 0,
    /// <summary>Yazılım geliştirme — Sprint, BacklogHierarchy, Board, Release.</summary>
    SoftwareDevelopment = 1,
    /// <summary>Sistem / Altyapı / Network — Milestone, WBS, Timeline.</summary>
    Infrastructure = 2,
    /// <summary>Tedarik / Edinme — Milestone, Vendor, Budget.</summary>
    Procurement = 3,
    /// <summary>İş / Organizasyonel — Milestone, Timeline, Budget.</summary>
    Business = 4
}

/// <summary>Proje tahmin gösterim modu.</summary>
public enum EstimationDisplayMode
{
    /// <summary>Story Points (Fibonacci: 0, 1, 2, 3, 5, 8, 13, 21).</summary>
    StoryPoints = 0,
    /// <summary>T-Shirt bedenleri (XS, S, M, L, XL) — SP'ye eşlenir.</summary>
    TShirt = 1,
    /// <summary>Saat bazlı tahmin.</summary>
    Hours = 2,
    /// <summary>Tahmin kullanılmaz.</summary>
    None = 3
}

/// <summary>Teslim edilebilir rolü — CI'ın projede hangi kapasitede olduğu.</summary>
public enum DeliverableRole
{
    /// <summary>Birincil çıktı — projenin ana ürünü.</summary>
    Primary = 0,
    /// <summary>İkincil çıktı — proje kapsamında güncelleme/entegrasyon.</summary>
    Secondary = 1,
    /// <summary>Destek — proje için altyapı/araç.</summary>
    Supporting = 2
}

// ── CMDB CI Enum'ları ────────────────────────────────────────

/// <summary>Sunucu tipi.</summary>
public enum ServerType
{
    Physical = 0,
    Virtual = 1,
    Container = 2,
    Cloud = 3
}

/// <summary>Dağıtım ortamı.</summary>
public enum DeploymentEnvironment
{
    Development = 0,
    Staging = 1,
    Production = 2,
    DR = 3
}

/// <summary>Veritabanı motoru.</summary>
public enum DatabaseEngine
{
    PostgreSQL = 0,
    MSSQL = 1,
    Oracle = 2,
    MySQL = 3,
    MongoDB = 4,
    Redis = 5
}

/// <summary>Lisans tipi.</summary>
public enum LicenceType
{
    Perpetual = 0,
    Subscription = 1,
    OpenSource = 2,
    Trial = 3,
    OEM = 4
}

/// <summary>CI-CI ilişki tipi — CMDB graf yapısı.</summary>
public enum CIRelationType
{
    /// <summary>Uygulama → Server ("bu sunucuda çalışır").</summary>
    RunsOn = 0,
    /// <summary>Uygulama → Database ("bu veritabanına bağımlı").</summary>
    DependsOn = 1,
    /// <summary>Lisans → Uygulama/Server ("bu CI'a atanmış").</summary>
    LicensedTo = 2,
    /// <summary>Database → Server ("bu sunucuda barınır").</summary>
    HostedOn = 3,
    /// <summary>Uygulama → Uygulama ("entegre olur").</summary>
    IntegratesWith = 4,
    /// <summary>Server → Server ("yedek/DR").</summary>
    BackupOf = 5
}

// ── Requirement Enum'ları ────────────────────────────────────

/// <summary>Gereksinim tipi.</summary>
public enum RequirementType
{
    /// <summary>Üst-seviye fonksiyonel spec — Description alanı spec dokümanı yerine geçer.</summary>
    FeatureSpec = 0,
    /// <summary>Fonksiyonel gereksinim — sistem davranışı.</summary>
    Functional = 1,
    /// <summary>Fonksiyonel olmayan gereksinim — performans, güvenlik, ölçeklenebilirlik.</summary>
    NonFunctional = 2,
    /// <summary>Arayüz gereksinimi — entegrasyon, API, UI.</summary>
    Interface = 3,
    /// <summary>Kısıt — regülasyon, teknoloji, bütçe.</summary>
    Constraint = 4
}

/// <summary>Gereksinim önceliği (MoSCoW).</summary>
public enum RequirementPriority
{
    /// <summary>Olmazsa olmaz.</summary>
    Must = 0,
    /// <summary>Olması gerekir ama ertelenebilir.</summary>
    Should = 1,
    /// <summary>Olsa iyi olur.</summary>
    Could = 2,
    /// <summary>Bu sürümde olmayacak.</summary>
    WontHave = 3
}

/// <summary>Gereksinim durumu.</summary>
public enum RequirementStatus
{
    /// <summary>Taslak — henüz gözden geçirilmedi.</summary>
    Draft = 0,
    /// <summary>İnceleme/gözden geçirme aşamasında.</summary>
    InReview = 1,
    /// <summary>Onaylandı — implementasyona hazır.</summary>
    Approved = 2,
    /// <summary>İmplemente edildi — tüm iş kalemleri tamamlandı.</summary>
    Implemented = 3,
    /// <summary>Doğrulandı — test edildi ve kabul edildi.</summary>
    Verified = 4
}

// ── Test Management Enum'ları ────────────────────────────────

/// <summary>Test senaryosu tipi.</summary>
public enum TestScenarioType
{
    /// <summary>Fonksiyonel test.</summary>
    Functional = 0,
    /// <summary>Regresyon testi.</summary>
    Regression = 1,
    /// <summary>Smoke test — temel işlevsellik kontrolü.</summary>
    Smoke = 2,
    /// <summary>Entegrasyon testi.</summary>
    Integration = 3,
    /// <summary>Kullanıcı kabul testi.</summary>
    UAT = 4,
    /// <summary>Performans testi.</summary>
    Performance = 5
}

/// <summary>Test senaryosu önceliği.</summary>
public enum TestScenarioPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Test senaryosu durumu.</summary>
public enum TestScenarioStatus
{
    /// <summary>Taslak — henüz hazır değil.</summary>
    Draft = 0,
    /// <summary>Aktif — çalıştırılmaya hazır.</summary>
    Active = 1,
    /// <summary>Kullanımdan kaldırıldı.</summary>
    Deprecated = 2,
    /// <summary>Arşivlendi.</summary>
    Archived = 3
}

/// <summary>Test planı durumu.</summary>
public enum TestPlanStatus
{
    /// <summary>Taslak — plan hazırlanıyor.</summary>
    Draft = 0,
    /// <summary>Aktif — çalıştırma bekliyor.</summary>
    Active = 1,
    /// <summary>Çalıştırma devam ediyor.</summary>
    InExecution = 2,
    /// <summary>Tamamlandı — tüm senaryolar çalıştırıldı.</summary>
    Completed = 3,
    /// <summary>İptal edildi.</summary>
    Cancelled = 4
}

/// <summary>Test çalıştırma sonucu.</summary>
public enum TestResult
{
    /// <summary>Henüz çalıştırılmadı.</summary>
    NotRun = 0,
    /// <summary>Başarılı.</summary>
    Pass = 1,
    /// <summary>Başarısız.</summary>
    Fail = 2,
    /// <summary>Engellendi — ön koşul sağlanamadı.</summary>
    Blocked = 3,
    /// <summary>Atlandı.</summary>
    Skipped = 4
}

// ── Release Management Enum'ları ─────────────────────────────

/// <summary>Release durumu — MVP'de sabit akış.</summary>
public enum ReleaseStatus
{
    /// <summary>Planlama aşaması.</summary>
    Planning = 0,
    /// <summary>Kod dondurma — yeni özellik eklenmez.</summary>
    CodeFreeze = 1,
    /// <summary>Test aşaması.</summary>
    Testing = 2,
    /// <summary>Go/No-Go karar aşaması.</summary>
    GoNoGo = 3,
    /// <summary>Staging ortamında.</summary>
    Staging = 4,
    /// <summary>Production'a deploy edildi.</summary>
    Deployed = 5,
    /// <summary>Release kapatıldı.</summary>
    Closed = 6,
    /// <summary>İptal edildi.</summary>
    Cancelled = 7,
    /// <summary>Geri alındı.</summary>
    Rollback = 8
}

/// <summary>Release tipi.</summary>
public enum ReleaseType
{
    /// <summary>Büyük sürüm — breaking change içerebilir.</summary>
    Major = 0,
    /// <summary>Küçük sürüm — yeni özellikler.</summary>
    Minor = 1,
    /// <summary>Yama — hata düzeltmeleri.</summary>
    Patch = 2,
    /// <summary>Acil düzeltme.</summary>
    Hotfix = 3,
    /// <summary>Geri alma sürümü.</summary>
    Rollback = 4
}

/// <summary>Go/No-Go checklist genel durumu.</summary>
public enum GoNoGoStatus
{
    /// <summary>Beklemede — henüz başlanmadı.</summary>
    Pending = 0,
    /// <summary>İnceleme devam ediyor.</summary>
    InProgress = 1,
    /// <summary>Onaylandı — release deploy edilebilir.</summary>
    Approved = 2,
    /// <summary>Reddedildi — release deploy edilemez.</summary>
    Rejected = 3
}

/// <summary>Go/No-Go kontrol maddesi kategorisi.</summary>
public enum GoNoGoCategory
{
    /// <summary>Geliştirme kontrolü.</summary>
    Development = 0,
    /// <summary>Kalite güvence kontrolü.</summary>
    QA = 1,
    /// <summary>Operasyon kontrolü.</summary>
    Operations = 2,
    /// <summary>Güvenlik kontrolü.</summary>
    Security = 3,
    /// <summary>İş birimi kontrolü.</summary>
    Business = 4,
    /// <summary>Yasal/uyumluluk kontrolü.</summary>
    Legal = 5
}

/// <summary>Go/No-Go kontrol maddesi durumu.</summary>
public enum GoNoGoItemStatus
{
    /// <summary>Beklemede — henüz değerlendirilmedi.</summary>
    Pending = 0,
    /// <summary>Onaylandı.</summary>
    Approved = 1,
    /// <summary>Reddedildi.</summary>
    Rejected = 2,
    /// <summary>Uygulanamaz — bu release için geçerli değil.</summary>
    NotApplicable = 3
}

// ── Risk Management Enum'ları ────────────────────────────────

/// <summary>Risk durumu.</summary>
public enum RiskStatus
{
    /// <summary>Açık — aktif risk.</summary>
    Open = 0,
    /// <summary>Azaltıldı — mitigation uygulandı.</summary>
    Mitigated = 1,
    /// <summary>Kapatıldı — risk ortadan kalktı.</summary>
    Closed = 2
}

/// <summary>Risk kategorisi.</summary>
public enum RiskCategory
{
    /// <summary>Teknik risk — teknoloji, mimari, performans.</summary>
    Technical = 0,
    /// <summary>Takvim riski — gecikme, deadline.</summary>
    Schedule = 1,
    /// <summary>Bütçe riski — maliyet aşımı.</summary>
    Budget = 2,
    /// <summary>Kaynak riski — personel eksikliği, yetkinlik.</summary>
    Resource = 3,
    /// <summary>Kapsam riski — scope creep.</summary>
    Scope = 4,
    /// <summary>Kalite riski — bug, test coverage.</summary>
    Quality = 5,
    /// <summary>Dış etken riski — vendor, regülasyon.</summary>
    External = 6,
    /// <summary>Organizasyonel risk — yönetim, iletişim.</summary>
    Organizational = 7
}

/// <summary>Risk azaltma aksiyonu durumu.</summary>
public enum MitigationActionStatus
{
    /// <summary>Planlandı.</summary>
    Planned = 0,
    /// <summary>Devam ediyor.</summary>
    InProgress = 1,
    /// <summary>Tamamlandı.</summary>
    Completed = 2,
    /// <summary>İptal edildi.</summary>
    Cancelled = 3
}
