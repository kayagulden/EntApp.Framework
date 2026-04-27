using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.KnowledgeBase.Application.IntegrationEvents;

/// <summary>Wiki sayfası yayınlandığında → bildirim, search index güncelleme.</summary>
public sealed record WikiPagePublishedEvent(
    Guid PageId, Guid SpaceId, string Title,
    Guid? ProjectId) : IntegrationEvent;

/// <summary>Wiki sayfası güncellendiğinde → takipçilere bildirim.</summary>
public sealed record WikiPageUpdatedEvent(
    Guid PageId, string Title,
    Guid AuthorUserId) : IntegrationEvent;
