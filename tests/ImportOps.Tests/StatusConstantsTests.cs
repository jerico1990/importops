using ImportOps.Domain.Entities;

namespace ImportOps.Tests;

public sealed class StatusConstantsTests
{
    [Fact]
    public void ApprovedQuotation_IsClosed()
    {
        Assert.Contains(
            QuotationStatuses.Approved,
            QuotationStatuses.Closed);
    }

    [Fact]
    public void EditableImportRequest_DoesNotIncludeApproved()
    {
        Assert.DoesNotContain(
            ImportRequestStatuses.Approved,
            ImportRequestStatuses.Editable);
    }

    [Fact]
    public void ClosedOperation_IsDistinctFromDelivered()
    {
        Assert.NotEqual(
            OperationStatuses.Delivered,
            OperationStatuses.Closed);
    }

    [Fact]
    public void FinancialClosed_IsExplicitState()
    {
        Assert.Equal("CLOSED", FinancialStatuses.Closed);
    }
}
