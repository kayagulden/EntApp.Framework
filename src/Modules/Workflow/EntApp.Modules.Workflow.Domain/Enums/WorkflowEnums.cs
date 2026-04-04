namespace EntApp.Modules.Workflow.Domain.Enums;

/// <summary>Workflow akış durumu.</summary>
public enum WorkflowStatus
{
    /// <summary>Taslak — henüz başlatılmadı</summary>
    Draft = 0,

    /// <summary>Aktif — onay sürecinde</summary>
    Active = 1,

    /// <summary>Tamamlandı — tüm adımlar onaylandı</summary>
    Completed = 2,

    /// <summary>Reddedildi — bir adım reddetti</summary>
    Rejected = 3,

    /// <summary>İptal edildi — manuel iptal</summary>
    Cancelled = 4,

    /// <summary>Zaman aşımı — süre doldu</summary>
    TimedOut = 5
}

/// <summary>Onay adımı durumu.</summary>
public enum StepStatus
{
    /// <summary>Beklemede</summary>
    Pending = 0,

    /// <summary>Onaylandı</summary>
    Approved = 1,

    /// <summary>Reddedildi</summary>
    Rejected = 2,

    /// <summary>Atlandı</summary>
    Skipped = 3,

    /// <summary>Üst kademeye yönlendirildi</summary>
    Escalated = 4
}

/// <summary>Onay tipi — adımların nasıl ilerleyeceğini belirler.</summary>
public enum ApprovalType
{
    /// <summary>Sıralı — her adım sırasıyla</summary>
    Sequential = 0,

    /// <summary>Paralel — tüm adımlar aynı anda, hepsi onaylamalı</summary>
    Parallel = 1,

    /// <summary>Herhangi biri — bir kişi onaylarsa yeterli</summary>
    AnyOne = 2
}
