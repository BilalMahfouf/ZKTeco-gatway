using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ZKTecoGatway.ZKTeco;
using Xunit;

namespace ZKTecoGatway.Tests;

public class ConnectEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IZKemSessionFactory> _sessionFactoryMock;
    private readonly Mock<IZKemSession> _sessionMock;

    public ConnectEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _sessionFactoryMock = new Mock<IZKemSessionFactory>();
        _sessionMock = new Mock<IZKemSession>();

        _sessionFactoryMock.Setup(f => f.Create()).Returns(_sessionMock.Object);
    }

    [Fact]
    public async Task Connect_ReturnsOkWithSerialNumber_WhenConnectionSucceeds()
    {
        _sessionMock.Setup(s => s.ConnectNet("192.168.1.100", 4370))
            .Returns(true);

        _sessionMock.Setup(s => s.GetSerialNumber(1, out It.Ref<string>.IsAny))
            .Callback(new GetSerialNumberCallback((int mn, out string sn) =>
            {
                sn = "TEST123456";
            }))
            .Returns(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_sessionFactoryMock.Object);
            });
        }).CreateClient();

        var request = new
        {
            IpAddress = "192.168.1.100",
            Port = 4370,
            MachineNumber = 1
        };

        var response = await client.PostAsJsonAsync("/api/zkteco/connect", request);
        var body = await response.Content.ReadFromJsonAsync<ConnectResponseDto>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(body);
        Assert.True(body!.Connected);
        Assert.Equal("TEST123456", body.SerialNumber);
        Assert.Null(body.Error);
    }

    [Fact]
    public async Task Connect_Returns500_WhenConnectionFails()
    {
        _sessionMock.Setup(s => s.ConnectNet("192.168.1.100", 4370))
            .Returns(false);

        _sessionMock.Setup(s => s.GetLastError())
            .Returns(1);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_sessionFactoryMock.Object);
            });
        }).CreateClient();

        var request = new
        {
            IpAddress = "192.168.1.100",
            Port = 4370,
            MachineNumber = 1
        };

        var response = await client.PostAsJsonAsync("/api/zkteco/connect", request);
        var body = await response.Content.ReadFromJsonAsync<ConnectResponseDto>();

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body!.Connected);
        Assert.Null(body.SerialNumber);
        Assert.Contains("SDK error: 1", body.Error);
    }

    [Fact]
    public async Task Connect_Returns500_WhenExceptionThrown()
    {
        _sessionMock.Setup(s => s.ConnectNet("192.168.1.100", 4370))
            .Throws(new InvalidOperationException("COM error"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_sessionFactoryMock.Object);
            });
        }).CreateClient();

        var request = new
        {
            IpAddress = "192.168.1.100",
            Port = 4370,
            MachineNumber = 1
        };

        var response = await client.PostAsJsonAsync("/api/zkteco/connect", request);
        var body = await response.Content.ReadFromJsonAsync<ConnectResponseDto>();

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body!.Connected);
        Assert.Null(body.SerialNumber);
        Assert.Equal("An unexpected error occurred while connecting to the machine.", body.Error);
    }

    private sealed class ConnectResponseDto
    {
        public bool Connected { get; set; }
        public string? SerialNumber { get; set; }
        public string? Error { get; set; }
    }

    private delegate void GetSerialNumberCallback(int machineNumber, out string serialNumber);
}
