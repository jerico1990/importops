using ImportOps.Domain.Entities;
using Xunit;

namespace ImportOps.Tests;

public sealed class AlertConstantsTests
{
    [Theory]
    [InlineData(AlertSeverities.Info)]
    [InlineData(AlertSeverities.Warning)]
    [InlineData(AlertSeverities.Critical)]
    public void Alert_severity_catalog_accepts_supported_values(string value)
        => Assert.Contains(value, AlertSeverities.All);

    [Fact]
    public void Client_visible_alert_types_are_explicitly_controlled()
    {
        Assert.Contains(AlertTypes.ShipmentEtaSoon, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.ShipmentEtaOverdue, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.ShipmentArrived, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.DocumentObserved, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.CustomsReleased, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.QuotationPendingApproval, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.RequestObserved, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.OperationOnHold, AlertTypes.ClientVisible);
        Assert.Contains(AlertTypes.OperationCancelled, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.RequestPendingReview, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.QuotationRejected, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.QuotationApprovedOperationPending, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.TaskOverdue, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.SettlementPending, AlertTypes.ClientVisible);
        Assert.DoesNotContain(AlertTypes.OperationReadyToClose, AlertTypes.ClientVisible);
    }

    [Fact]
    public void Alert_lifecycle_has_active_and_resolved_states()
    {
        Assert.Equal("ACTIVE", AlertStatuses.Active);
        Assert.Equal("RESOLVED", AlertStatuses.Resolved);
    }
}
