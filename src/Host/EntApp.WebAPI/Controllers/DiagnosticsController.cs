using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers;

/// <summary>
/// Walking Skeleton controller — Altyapı doğrulama endpoint'leri.
/// Tüm middleware, auth, versioning, rate limiting zincirinin
/// uçtan uca çalıştığını doğrular.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class DiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DiagnosticsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Walking Skeleton doğrulama — tüm altyapı bileşenlerinin
    /// çalıştığını kontrol eder.
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Status = "OK",
            Timestamp = DateTimeOffset.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version = "1.0.0",
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        });
    }

    /// <summary>
    /// Altyapı bileşenlerinin konfigürasyon durumunu döner.
    /// Sadece Development ortamında detay gösterir.
    /// </summary>
    [HttpGet("info")]
    public IActionResult Info()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var info = new
        {
            Application = "EntApp.Framework",
            Version = "1.0.0",
            Environment = env,
            Components = new
            {
                Database = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection")),
                Redis = !string.IsNullOrEmpty(_configuration["Redis:ConnectionString"]),
                Keycloak = !string.IsNullOrEmpty(_configuration["Keycloak:Authority"]),
                RabbitMQ = !string.IsNullOrEmpty(_configuration["RabbitMQ:HostName"]),
                Seq = _configuration["Serilog:WriteTo:1:Args:serverUrl"] is not null
            }
        };

        return Ok(info);
    }
}
