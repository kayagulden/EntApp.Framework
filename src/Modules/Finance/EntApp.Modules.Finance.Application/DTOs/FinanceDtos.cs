namespace EntApp.Modules.Finance.Application.DTOs;

public sealed record AccountListDto(Guid Id, string Code, string Name, string AccountType,
    string Currency, decimal Balance, string? TaxNumber, DateTime CreatedAt);
public sealed record BalanceSummaryDto(string AccountType, int Count, decimal TotalBalance);
public sealed record InvoiceListDto(Guid Id, string InvoiceNumber, string? AccountName,
    string InvoiceType, string Status, DateTime InvoiceDate, DateTime DueDate,
    decimal GrandTotal, decimal PaidAmount, string Currency);
public sealed record OverdueInvoiceDto(Guid Id, string InvoiceNumber, string? AccountName,
    DateTime DueDate, decimal GrandTotal, decimal PaidAmount, int DaysOverdue);
public sealed record PaymentListDto(Guid Id, string? AccountName, decimal Amount, string Currency,
    string Direction, string Method, DateTime PaymentDate, string? ReferenceNumber);
