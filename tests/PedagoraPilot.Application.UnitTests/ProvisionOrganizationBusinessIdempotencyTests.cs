using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Services;
using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Organizations.Provision;
using PedagoraPilot.Contracts.Provisioning;
using PedagoraPilot.Domain.Organizations;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class ProvisionOrganizationBusinessIdempotencyTests
{
    [Fact]
    public async Task Same_external_owner_must_reuse_existing_organization()
    {
        var ownerUserId = Guid.NewGuid();
        var existing = Organization.Provision(
            ownerUserId,
            "PP-EXISTING",
            "Existing Training Center",
            "FR",
            "owner@example.com",
            null);

        var repository = new FakeOrganizationRepository(existing);
        var mapper = new FakeMapper(existing);
        var handler = new ProvisionOrganizationCommandHandler(repository, mapper);

        var result = await handler.Handle(
            new ProvisionOrganizationCommand(
                ownerUserId,
                "Changed display name",
                "FR",
                "Owner",
                "Example",
                "owner@example.com",
                "+33600000000"),
            CancellationToken.None);

        result.OrganizationId.Should().Be(existing.Id);
        repository.AddCalls.Should().Be(0);
    }

    private sealed class FakeOrganizationRepository(Organization existing)
        : IOrganizationRepository
    {
        public int AddCalls { get; private set; }

        public Task<Organization?> GetByOwnerUserIdAsync(
            Guid ownerUserId,
            bool isTracking = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Organization?>(
                existing.OwnerUserId == ownerUserId ? existing : null);

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AddAsync(
            Organization organization,
            CancellationToken cancellationToken = default)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public IQueryable<Organization> Query(bool isTracking = false)
            => new[] { existing }.AsQueryable();

        public Task<Organization?> GetByIdAsync(
            Guid id,
            bool isTracking = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Organization?>(existing.Id == id ? existing : null);

        public void Update(Organization entity)
        {
            throw new NotImplementedException();
        }

        public void Remove(Organization entity)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeMapper(Organization existing) : IObjectMapper
    {
        public TDestination Map<TDestination>(object source)
            => (TDestination)(object)new ProvisionOrganizationResponse
            {
                OrganizationId = existing.Id,
                Code = existing.Code.Value,
                Name = existing.LegalName,
                Status = existing.Status.ToString()
            };

        public TDestination Map<TSource, TDestination>(TSource source)
            => Map<TDestination>(source!);

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
            => throw new NotSupportedException();

        public TDestination Map<TDestination>(object source, Action<IMappingOperationOptions> options)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions> options)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination, Action<IMappingOperationOptions> options)
        {
            throw new NotImplementedException();
        }

        public object? Map(object? source, Type sourceType, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public object? Map(object? source, Type sourceType, Type destinationType, Action<IMappingOperationOptions> options)
        {
            throw new NotImplementedException();
        }

        public object? Map(object? source, object destination, Type sourceType, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public object? Map(object? source, object destination, Type sourceType, Type destinationType, Action<IMappingOperationOptions> options)
        {
            throw new NotImplementedException();
        }

        TDestination IObjectMapper.Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            throw new NotImplementedException();
        }
    }
}
