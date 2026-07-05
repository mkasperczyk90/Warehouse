using Warehouse.IdentityBridge;
using Xunit;

namespace Warehouse.IdentityBridge.Tests;

public sealed class BronzeUserMapperTests
{
    private static readonly DateTimeOffset PolledAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    private static KeycloakUser User(
        string username = "7700",
        string? firstName = "Jan",
        string? lastName = "Kowalski",
        bool enabled = true,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? attributes = null) =>
        new("kc-id-1", username, firstName, lastName, enabled,
            attributes ?? new Dictionary<string, IReadOnlyList<string>> { ["default_warehouse"] = ["WH01"] });

    [Fact]
    public void Maps_every_BronzeUser_field()
    {
        var bronzeEvent = BronzeUserMapper.Map(User(), ["warehouse-operator", "picker"], PolledAt);

        Assert.Equal("identity-service", bronzeEvent.SourceSystem);
        Assert.Equal("UserSnapshot", bronzeEvent.EventType);
        Assert.Equal("7700", bronzeEvent.EventId);
        Assert.Equal(PolledAt, bronzeEvent.OccurredAt);
        Assert.Equal("7700", bronzeEvent.Payload["user_id"]);
        Assert.Equal("Jan Kowalski", bronzeEvent.Payload["display_name"]);
        Assert.Equal("warehouse-operator,picker", bronzeEvent.Payload["role"]);
        Assert.Equal("WH01", bronzeEvent.Payload["home_warehouse_code"]);
        Assert.Equal("True", bronzeEvent.Payload["active"]);
    }

    [Fact]
    public void A_disabled_user_carries_active_False()
    {
        var bronzeEvent = BronzeUserMapper.Map(User(enabled: false), [], PolledAt);

        Assert.Equal("False", bronzeEvent.Payload["active"]);
    }

    [Fact]
    public void A_user_with_no_roles_carries_a_null_role_column()
    {
        var bronzeEvent = BronzeUserMapper.Map(User(), [], PolledAt);

        Assert.Null(bronzeEvent.Payload["role"]);
    }

    [Fact]
    public void A_user_with_no_default_warehouse_attribute_carries_a_null_column()
    {
        var bronzeEvent = BronzeUserMapper.Map(
            User(attributes: new Dictionary<string, IReadOnlyList<string>>()), [], PolledAt);

        Assert.Null(bronzeEvent.Payload["home_warehouse_code"]);
    }

    [Fact]
    public void A_missing_last_name_does_not_leave_a_trailing_space_in_display_name()
    {
        var bronzeEvent = BronzeUserMapper.Map(User(lastName: null), [], PolledAt);

        Assert.Equal("Jan", bronzeEvent.Payload["display_name"]);
    }

    [Fact]
    public void No_first_or_last_name_carries_a_null_display_name()
    {
        var bronzeEvent = BronzeUserMapper.Map(User(firstName: null, lastName: null), [], PolledAt);

        Assert.Null(bronzeEvent.Payload["display_name"]);
    }
}
