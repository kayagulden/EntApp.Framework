using EntApp.Modules.AI.Application.DTOs;
using MediatR;

namespace EntApp.Modules.AI.Application.Commands;

public sealed record EmbedTextCommand(string Text) : IRequest<EmbedResponse>;

public sealed record StoreEmbeddingCommand(string Content, string? ModuleName = null,
    string? SourceType = null, string? SourceId = null, string? Metadata = null,
    int ChunkIndex = 0) : IRequest<StoreEmbeddingResult>;

public sealed record IngestDocumentCommand(Stream FileStream, string FileName,
    string? ModuleName = null, string? SourceType = null, string? SourceId = null) : IRequest<IngestResponse>;
