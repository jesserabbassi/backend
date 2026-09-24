using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NinetyBackend.Infrastructure.SignalR;
using Xunit;

namespace NinetyBackend.Tests.Stations;

public class StationHubTests
{
    private readonly Mock<ILogger<StationHub>> _loggerMock = new();
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly StationHub _hub;

    public StationHubTests()
    {
        _hub = new StationHub(_loggerMock.Object)
        {
            Context = _contextMock.Object
        };
        _contextMock.Setup(c => c.ConnectionId).Returns("test-conn-123");
    }

    [Fact]
    public async Task OnConnectedAsync_LogsClientConnected()
    {
        await _hub.OnConnectedAsync();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-conn-123") && v.ToString()!.Contains("connected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithoutException_LogsClientDisconnected()
    {
        await _hub.OnDisconnectedAsync(null);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-conn-123") && v.ToString()!.Contains("disconnected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_LogsWarning()
    {
        var ex = new Exception("Connection lost");
        await _hub.OnDisconnectedAsync(ex);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-conn-123")),
                ex,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
