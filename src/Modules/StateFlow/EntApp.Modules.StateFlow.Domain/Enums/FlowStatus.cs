namespace EntApp.Modules.StateFlow.Domain.Enums;

/// <summary>Akış tanımının yaşam döngüsü durumu.</summary>
public enum FlowStatus
{
    /// <summary>Taslak — düzenlenebilir, henüz yayınlanmadı.</summary>
    Draft = 0,
    /// <summary>Yayınlandı — yeni entity'ler bu versiyondan başlar.</summary>
    Published = 1,
    /// <summary>Arşivlendi — yeni versiyon yayınlandığında eski versiyon bu duruma geçer.</summary>
    Archived = 2
}
