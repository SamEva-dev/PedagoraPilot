using System.Reflection;
using FluentAssertions;
using PedagoraPilot.Application.Common.Errors;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;
public sealed class ErrorKeysTests
{
    [Fact]
    public void Error_keys_are_unique_uppercase_and_stable_strings()
    {
        var values = typeof(ErrorKeys).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string)).Select(f => (Name: f.Name, Value: (string)f.GetRawConstantValue()!)).ToArray();
        values.Should().NotBeEmpty();
        values.Select(x => x.Value).Should().OnlyHaveUniqueItems();
        foreach (var(_, value)in values)
        {
            value.Should().NotBeNullOrWhiteSpace();
            value.Should().MatchRegex("^[A-Z0-9_]+$");
        }
    }
}
