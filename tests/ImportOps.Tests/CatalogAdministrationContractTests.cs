using ImportOps.Application.Security;
using Xunit;

namespace ImportOps.Tests;

public sealed class CatalogAdministrationContractTests
{
    [Fact]
    public void Configuration_permissions_are_defined()
    {
        Assert.Equal("CONFIGURATION.VIEW", Permissions.ConfigurationView);
        Assert.Equal("CONFIGURATION.MANAGE", Permissions.ConfigurationManage);
    }
}
