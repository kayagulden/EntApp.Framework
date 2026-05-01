namespace EntApp.Modules.StateFlow.Domain.Enums;

/// <summary>StateFlow aksiyon tipleri — state'e giriş veya transition sırasında tetiklenir.</summary>
public enum ActionType
{
    /// <summary>Bildirim gönder (InApp, Email).</summary>
    SendNotification = 0,
    /// <summary>İş kalemini birine ata.</summary>
    AssignWorkItem = 1,
    /// <summary>İş kaleminin durumunu değiştir.</summary>
    ChangeStatus = 2,
    /// <summary>Otomatik yorum ekle.</summary>
    AddComment = 3,
    /// <summary>Webhook çağrısı (gelecek).</summary>
    CallWebhook = 4
}
