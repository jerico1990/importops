using ImportOps.Domain.Entities;

namespace ImportOps.Tests;

public sealed class DocumentFinanceInvariantTests
{
    [Fact]
    public void ValidDocument_IsDifferentFromObserved()
        => Assert.NotEqual(DocumentStatuses.Valid,DocumentStatuses.Observed);

    [Fact]
    public void FinancialClosed_IsRequiredDistinctState()
        => Assert.Equal("CLOSED",FinancialStatuses.Closed);

    [Fact]
    public void SettlementApproved_IsExplicit()
        => Assert.Equal("APPROVED",FinanceStatuses.Approved);
}
