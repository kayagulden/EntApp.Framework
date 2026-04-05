using EntApp.Modules.AI.Application.DTOs;
using MediatR;

namespace EntApp.Modules.AI.Application.Queries;

public sealed record SearchSimilarQuery(string Query, int TopK = 5,
    double MinScore = 0.7, string? ModuleName = null) : IRequest<SearchResponse>;

public sealed record RagQuery(string Query, int TopK = 5, double MinScore = 0.7,
    string? ModuleName = null, string? SystemPrompt = null) : IRequest<object>;
