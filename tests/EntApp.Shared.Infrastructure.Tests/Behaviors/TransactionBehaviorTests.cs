using EntApp.Shared.Contracts.Persistence;
using EntApp.Shared.Infrastructure.Behaviors;
using EntApp.Shared.Kernel.Results;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Behaviors;

// Test Command & Query
public sealed record TestCommand(string Name) : IRequest<Result>;
public sealed record TestTransactionlessCommand(string Name) : IRequest<Result>, ITransactionless;
public sealed record TestQuery(string Name) : IRequest<Result<string>>;

public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionBehavior<TestCommand, Result> _sut;

    public TransactionBehaviorTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<TransactionBehavior<TestCommand, Result>>>();
        _sut = new TransactionBehavior<TestCommand, Result>(_unitOfWork, logger);
    }

    [Fact]
    public async Task Command_ShouldWrapInTransaction()
    {
        var request = new TestCommand("test");
        var next = Substitute.For<RequestHandlerDelegate<Result>>();
        next().Returns(Task.FromResult(Result.Success()));

        await _sut.Handle(request, next, CancellationToken.None);

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Command_OnException_ShouldRollback()
    {
        var request = new TestCommand("test");
        var next = Substitute.For<RequestHandlerDelegate<Result>>();
        next().Returns<Task<Result>>(_ => throw new InvalidOperationException("Test error"));

        var act = async () => await _sut.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionlessCommand_ShouldSkipTransaction()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<TransactionBehavior<TestTransactionlessCommand, Result>>>();
        var sut = new TransactionBehavior<TestTransactionlessCommand, Result>(unitOfWork, logger);

        var request = new TestTransactionlessCommand("test");
        var next = Substitute.For<RequestHandlerDelegate<Result>>();
        next().Returns(Task.FromResult(Result.Success()));

        await sut.Handle(request, next, CancellationToken.None);

        await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_ShouldSkipTransaction()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<TransactionBehavior<TestQuery, Result<string>>>>();
        var sut = new TransactionBehavior<TestQuery, Result<string>>(unitOfWork, logger);

        var request = new TestQuery("test");
        var next = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        next().Returns(Task.FromResult(Result<string>.Success("value")));

        await sut.Handle(request, next, CancellationToken.None);

        await unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }
}
