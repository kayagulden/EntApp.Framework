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
