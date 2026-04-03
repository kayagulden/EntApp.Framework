using System.Diagnostics;
using EntApp.Shared.Kernel.Exceptions;
using EntApp.Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Tüm unhandled exception'ları yakalar ve RFC 7807 ProblemDetails formatında döner.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errorType) = exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Kaynak Bulunamadı",
                notFound.Message,
                ErrorType.NotFound),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Çakışma",
                conflict.Message,
                ErrorType.Conflict),

            DomainException domain => (
                StatusCodes.Status422UnprocessableEntity,
                "İş Kuralı Hatası",
                domain.Message,
                ErrorType.Validation),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Yetkisiz Erişim",
                "Bu işlem için yetkiniz bulunmamaktadır.",
                ErrorType.Unauthorized),

            ArgumentException argument => (
                StatusCodes.Status400BadRequest,
                "Geçersiz İstek",
                argument.Message,
                ErrorType.Validation),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucu Hatası",
                "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                ErrorType.Failure)
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problemDetails.Extensions["errorType"] = errorType.ToString();

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
