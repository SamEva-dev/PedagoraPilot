using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Infrastructure.Persistence;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class DatabaseObjectIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Expected_schemas_exist_after_model_creation()
    {
        await using var db = fixture.CreateContext();
        var schemas = await db.Database.SqlQueryRaw<string>("""
            SELECT schema_name AS "Value" FROM information_schema.schemata
               WHERE schema_name IN('platform', 'organization', 'catalog', 'training', 'learning', 'workplace',
               'document', 'certification', 'distance_learning', 'workforce', 'audit', 'integration')
            """).ToListAsync();
        schemas.Should().Contain([SchemaNames.Platform, SchemaNames.Organization, SchemaNames.Catalog, SchemaNames.Training, SchemaNames.Learning, SchemaNames.Workplace, SchemaNames.Document, SchemaNames.Certification, SchemaNames.DistanceLearning, SchemaNames.Workforce, SchemaNames.Audit, SchemaNames.Integration]);
    }
}
