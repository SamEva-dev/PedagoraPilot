using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Infrastructure.Persistence;
using PedagoraPilot.Infrastructure.Persistence.Catalog;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class ModelSchemaIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public void Every_table_uses_an_explicit_non_public_schema()
    {
        using var db = fixture.CreateContext();
        var relationalTypes = db.Model.GetEntityTypes().Where(x => x.GetTableName()is not null).ToArray();
        relationalTypes.Should().NotBeEmpty();
        foreach (var entityType in relationalTypes)
        {
            entityType.GetSchema().Should().NotBeNullOrWhiteSpace($"{entityType.ClrType.Name} must have an explicit schema");
            entityType.GetSchema().Should().NotBe("public");
        }
    }

    [Fact]
    public void Catalog_entities_are_mapped_to_catalog_schema()
    {
        using var db = fixture.CreateContext();
        db.Model.FindEntityType(typeof(TrainingProgram))!.GetSchema().Should().Be(SchemaNames.Catalog);
        db.Model.FindEntityType(typeof(ProgramCapabilityRow))!.GetSchema().Should().Be(SchemaNames.Catalog);
        db.Model.FindEntityType(typeof(ReferentialVersionCapabilityRow))!.GetSchema().Should().Be(SchemaNames.Catalog);
    }

    [Fact]
    public void Strong_identifiers_have_value_converters_in_EF_model()
    {
        using var db = fixture.CreateContext();
        var cohort = db.Model.FindEntityType(typeof(Cohort))!;
        var idProperty = cohort.FindProperty(nameof(Cohort.Id))!;
        idProperty.GetValueConverter().Should().NotBeNull();
        idProperty.GetValueConverter()!.ProviderClrType.Should().Be(typeof(Guid));
    }
}
