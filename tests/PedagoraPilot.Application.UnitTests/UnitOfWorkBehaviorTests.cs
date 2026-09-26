using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Behaviors;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Transactional_command_saves_changes()
    {
        var unitOfWork = new FakeUnitOfWork();
        var compensation = new TransactionCompensation();
        var behavior = new UnitOfWorkBehavior<TestCommand, int>(unitOfWork, compensation);

        var result = await behavior.Handle(new TestCommand(), () => Task.FromResult(42), CancellationToken.None);

        result.Should().Be(42);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Failed_transaction_executes_registered_compensation()
    {
        var unitOfWork = new FakeUnitOfWork { SaveException = new InvalidOperationException("db failure") };
        var compensation = new TransactionCompensation();
        var behavior = new UnitOfWorkBehavior<TestCommand, int>(unitOfWork, compensation);
        var compensated = false;

        Func<Task> action = async () => await behavior.Handle(new TestCommand(), () =>
        {
            compensation.Register(_ =>
            {
                compensated = true;
                return Task.CompletedTask;
            });
            return Task.FromResult(42);
        }, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db failure");
        compensated.Should().BeTrue();
    }

    [Fact]
    public async Task Successful_transaction_discards_compensation()
    {
        var unitOfWork = new FakeUnitOfWork();
        var compensation = new TransactionCompensation();
        var behavior = new UnitOfWorkBehavior<TestCommand, int>(unitOfWork, compensation);
        var compensated = false;

        await behavior.Handle(new TestCommand(), () =>
        {
            compensation.Register(_ =>
            {
                compensated = true;
                return Task.CompletedTask;
            });
            return Task.FromResult(42);
        }, CancellationToken.None);

        await compensation.RollbackAsync();
        compensated.Should().BeFalse();
    }

    private sealed record TestCommand : ICommand<int>;

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public Exception? SaveException { get; init; }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            if (SaveException is not null)
                throw SaveException;
            return Task.FromResult(1);
        }
    }
}
