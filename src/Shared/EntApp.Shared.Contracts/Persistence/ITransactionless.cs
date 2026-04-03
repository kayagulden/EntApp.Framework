namespace EntApp.Shared.Contracts.Persistence;

/// <summary>
/// Transaction gerektirmeyen Command'lar için marker interface.
/// TransactionBehavior bu interface'i gördüğünde transaction açmaz.
/// </summary>
/// <example>
/// <code>
/// public sealed record ImportDataCommand : IRequest&lt;Result&gt;, ITransactionless;
/// </code>
/// </example>
public interface ITransactionless;
