using SignalTrader.Signals.Resources;

namespace SignalTraderTests.Signals.Resources;

public class PingResponseResourceTests
{
    [Fact]
    public void PingResponseResource_Properties()
    {
        // Arrange
        const string expectedServerTime = "2023-01-15T22:56:08.8355850Z";
        const string expectedServerVersion = "0.0.0.0";

        // Act
        var sut = new PingResource(expectedServerTime, expectedServerVersion);

        // Assert
        Assert.NotNull(sut);
        Assert.NotNull(sut.ServerTime);
        Assert.NotNull(sut.ServerVersion);
        Assert.Equal(expectedServerTime, sut.ServerTime);
        Assert.Equal(expectedServerVersion, sut.ServerVersion);
    }
}
