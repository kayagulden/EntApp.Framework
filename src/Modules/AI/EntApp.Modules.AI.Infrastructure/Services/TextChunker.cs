using System.Text;
using EntApp.Modules.AI.Application.DTOs;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Metin parçalama — paragraf ve cümle bazlı chunking.
/// Her chunk belirli bir token limitine uyar, overlap ile bağlam korunur.
/// </summary>
public sealed class TextChunker
{
    /// <summary>
    /// Metni belirli token limitine göre chunk'lara böler.
    /// </summary>
    /// <param name="text">Kaynak metin</param>
    /// <param name="maxTokens">Chunk başına maks token (default: 500)</param>
    /// <param name="overlapTokens">Overlap token sayısı (default: 50)</param>
    public IReadOnlyList<TextChunk> Chunk(string text, int maxTokens = 500, int overlapTokens = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var paragraphs = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<TextChunk>();
        var currentChunk = new StringBuilder();
        var currentTokens = 0;
        var chunkIndex = 0;

        foreach (var para in paragraphs)
        {
            var paraTokens = EstimateTokens(para);

            // Paragraf tek başına max'ı aşıyorsa cümle bazlı böl
            if (paraTokens > maxTokens)
            {
                // Mevcut chunk'ı flush et
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString(), chunkIndex++));
                    currentChunk.Clear();
                    currentTokens = 0;
                }

                var sentences = SplitSentences(para);
                foreach (var sentence in sentences)
                {
                    var sentenceTokens = EstimateTokens(sentence);

                    if (currentTokens + sentenceTokens > maxTokens && currentChunk.Length > 0)
                    {
                        chunks.Add(CreateChunk(currentChunk.ToString(), chunkIndex++));

                        // Overlap: son overlap token kadar geri al
                        var overlapText = GetOverlapText(currentChunk.ToString(), overlapTokens);
                        currentChunk.Clear();
                        currentChunk.Append(overlapText);
                        currentTokens = EstimateTokens(overlapText);
                    }

                    currentChunk.Append(sentence).Append(' ');
                    currentTokens += sentenceTokens;
                }
            }
            else
            {
                if (currentTokens + paraTokens > maxTokens && currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString(), chunkIndex++));

                    var overlapText = GetOverlapText(currentChunk.ToString(), overlapTokens);
                    currentChunk.Clear();
                    currentChunk.Append(overlapText);
                    currentTokens = EstimateTokens(overlapText);
                }

                currentChunk.AppendLine(para);
                currentTokens += paraTokens;
            }
        }

        // Son chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(currentChunk.ToString(), chunkIndex));
        }

        return chunks;
    }

    private static TextChunk CreateChunk(string content, int index)
    {
        var trimmed = content.Trim();
        return new TextChunk
        {
            Content = trimmed,
            Index = index,
            TokenCount = EstimateTokens(trimmed)
        };
    }

    private static int EstimateTokens(string text) => text.Length / 4;

    private static string[] SplitSentences(string text)
    {
        return text.Split([". ", "! ", "? ", ".\n", "!\n", "?\n"],
            StringSplitOptions.RemoveEmptyEntries);
    }

    private static string GetOverlapText(string text, int overlapTokens)
    {
        var overlapChars = overlapTokens * 4;
        if (text.Length <= overlapChars)
            return text;

        return text[^overlapChars..];
    }
}
