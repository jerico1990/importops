using ImportOps.Domain.Entities;

namespace ImportOps.Tests;

public sealed class ShipmentStatusTests
{
    [Fact]
    public void Arrived_IsFinal()
    {
        Assert.Contains(ShipmentStatuses.Arrived, ShipmentStatuses.Final);
    }

    [Fact]
    public void Cancelled_IsFinal()
    {
        Assert.Contains(ShipmentStatuses.Cancelled, ShipmentStatuses.Final);
    }

    [Fact]
    public void Departed_IsNotFinal()
    {
        Assert.DoesNotContain(ShipmentStatuses.Departed, ShipmentStatuses.Final);
    }
}
