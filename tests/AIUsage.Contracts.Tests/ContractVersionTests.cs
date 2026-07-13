using AIUsage.Contracts;
using Xunit;

namespace AIUsage.Contracts.Tests;

public sealed class ContractVersionTests
{
    [Fact]
    public void CurrentContractVersionIsV1()
    {
        Assert.Equal(1, ContractVersion.Major);
        Assert.Equal("v1", ContractVersion.Current);
    }
}
