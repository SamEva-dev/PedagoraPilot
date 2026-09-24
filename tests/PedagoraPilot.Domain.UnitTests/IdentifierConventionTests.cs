using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class IdentifierConventionTests
{
    [Fact]
    public void Every_identifier_exposes_a_public_Guid_constructor_and_New_factory()
    {
        var idTypes = typeof(CohortId).Assembly.GetTypes().Where(t => t.Namespace == "PedagoraPilot.Domain.Identifiers").Where(t => typeof(IIdentifier).IsAssignableFrom(t)).Where(t => t.IsValueType).OrderBy(t => t.Name).ToArray();
        idTypes.Should().NotBeEmpty();
        foreach (var type in idTypes)
        {
            type.GetConstructor([typeof(Guid)]).Should().NotBeNull($"{type.Name} must wrap a Guid");
            var factory = type.GetMethod("New", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            factory.Should().NotBeNull($"{type.Name} must expose New()");
            var boxed = factory!.Invoke(null, null);
            boxed.Should().BeAssignableTo<IIdentifier>();
            ((IIdentifier)boxed!).IsEmpty.Should().BeFalse();
        }
    }

    [Fact]
    public void Only_pre_identifier_legacy_aggregates_may_use_the_non_generic_AggregateRoot()
    {
        var allowedLegacy = new HashSet<string>(StringComparer.Ordinal)
        {
            "Organization",
            "TrainingSite",
            "ProgramFamily",
            "TrainingProgram",
            "ProgramOffering",
            "Referential",
            "ReferentialVersion"
        };
        var offenders = typeof(AggregateRoot).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).Where(t => typeof(AggregateRoot).IsAssignableFrom(t)).Select(t => t.Name).Where(name => !allowedLegacy.Contains(name)).ToArray();
        offenders.Should().BeEmpty("all aggregates introduced after the Identifier convention must derive from AggregateRoot<TIdentifier>");
    }
}
