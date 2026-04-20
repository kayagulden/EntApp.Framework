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

/// <summary>Görev durumu (Kanban).</summary>
public enum TaskStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4,
    Cancelled = 5
}

/// <summary>Görev önceliği.</summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Görev tipi.</summary>
public enum TaskType
{
    Task = 0,
    Bug = 1,
    Feature = 2,
    Improvement = 3,
    Epic = 4
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
