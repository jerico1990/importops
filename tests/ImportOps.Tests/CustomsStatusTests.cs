using ImportOps.Domain.Entities;

namespace ImportOps.Tests;

public sealed class CustomsStatusTests
{
    [Fact]
    public void Released_IsDifferentFromObserved()
    {
        Assert.NotEqual(CustomsStatuses.Released, CustomsStatuses.Observed);
    }

    [Theory]
    [InlineData(CustomsChannels.Green)]
    [InlineData(CustomsChannels.Yellow)]
    [InlineData(CustomsChannels.Red)]
    [InlineData(CustomsChannels.Other)]
    public void KnownChannels_AreNonEmpty(string channel)
    {
        Assert.False(string.IsNullOrWhiteSpace(channel));
    }

    [Fact]
    public void PaidAndExempt_AreValidTerminalPaymentStatesForRelease()
    {
        Assert.Equal("PAID", CustomsTaxPaymentStatuses.Paid);
        Assert.Equal("EXEMPT", CustomsTaxPaymentStatuses.Exempt);
    }
}
