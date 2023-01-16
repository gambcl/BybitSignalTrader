using SignalTrader.Common.Resources;

namespace SignalTraderTests.Common.Resources;

public class ErrorResourceTests
{
    [Fact]
    public void ErrorResource_Properties()
    {
        // Arrange
        const string expectedMessage = "Message";

        // Act
        var sut = new ErrorResource(expectedMessage);

        // Assert
        Assert.NotNull(sut);
        Assert.NotNull(sut.Message);
        Assert.Equal(expectedMessage, sut.Message);
    }
}
