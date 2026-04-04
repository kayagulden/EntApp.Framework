using System.Text;
using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Doküman işleme — PDF, TXT, MD dosyalarını metin parçalarına ayırır.
/// </summary>
public sealed class DocumentProcessor : IDocumentProcessor
{
    private readonly TextChunker _chunker;
    private readonly ILogger<DocumentProcessor> _logger;

    private static readonly HashSet<string> SupportedExtensions =
        [".pdf", ".txt", ".md", ".csv", ".json", ".xml", ".html"];

    public DocumentProcessor(TextChunker chunker, ILogger<DocumentProcessor> logger)
    {
        _chunker = chunker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TextChunk>> ProcessAsync(
        Stream file, string fileName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";

        _logger.LogInformation("[DocProcessor] Processing {FileName} ({Extension})", fileName, extension);

        var rawText = extension switch
        {
            ".pdf" => ExtractPdfText(file),
            ".txt" or ".md" or ".csv" or ".json" or ".xml" or ".html" => await ExtractPlainTextAsync(file),
            _ => throw new NotSupportedException(
                $"File format '{extension}' is not supported. Supported: {string.Join(", ", SupportedExtensions)}")
        };

        if (string.IsNullOrWhiteSpace(rawText))
        {
            _logger.LogWarning("[DocProcessor] No text extracted from {FileName}", fileName);
            return [];
        }

        var chunks = _chunker.Chunk(rawText);
        _logger.LogInformation("[DocProcessor] {FileName}: {ChunkCount} chunks, ~{TotalTokens} tokens",
            fileName, chunks.Count, chunks.Sum(c => c.TokenCount));

        return chunks;
    }

    private static string ExtractPdfText(Stream stream)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.AppendLine(pageText);
                sb.AppendLine(); // Sayfa arası boşluk
            }
        }

        return sb.ToString();
    }

    private static async Task<string> ExtractPlainTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
