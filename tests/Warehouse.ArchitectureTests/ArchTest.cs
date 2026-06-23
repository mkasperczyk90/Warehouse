using NetArchTest.Rules;

namespace Warehouse.ArchitectureTests;

/// <summary>Shared helper: fail with the offending type names so a broken rule is actionable.</summary>
internal static class ArchTest
{
    public static void Assert(TestResult result)
    {
        Xunit.Assert.True(
            result.IsSuccessful,
            result.IsSuccessful
                ? string.Empty
                : "Architecture rule violated by:" + Environment.NewLine +
                  string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
