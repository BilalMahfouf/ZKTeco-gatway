using Carter;
using Microsoft.AspNetCore.Mvc;
using ZKTecoGatway.ZKTeco;

namespace ZKTecoGatway.Endpoints;

public sealed class ConnectEndpoint : ICarterModule
{
    public sealed record ConnectRequest(
        string IpAddress,
        int Port,
        int MachineNumber);

    public sealed record ConnectResponse(
        bool Connected,
        string? SerialNumber,
        string? Error);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/zkteco")
            .WithTags("ZKTeco");

        group.MapPost("/connect", async (
            [FromBody] ConnectRequest request,
            IZKemSessionFactory sessionFactory,
            ILogger<ConnectEndpoint> logger,
            CancellationToken ct) =>
        {
            var machine = new AttendenceMachine(
                request.MachineNumber,
                request.IpAddress,
                request.Port);

            using var session = sessionFactory.Create();

            try
            {
                if (!session.ConnectNet(machine.IpAddress, machine.Port))
                {
                    var error = session.GetLastError();

                    logger.LogWarning(
                        "Failed to connect to machine {MachineNumber} at {IpAddress}:{Port}. SDK error: {Error}",
                        machine.MachineNumber,
                        machine.IpAddress,
                        machine.Port,
                        error);

                    return Results.Json(
                        new ConnectResponse(
                            Connected: false,
                            SerialNumber: null,
                            Error: $"Failed to connect to machine. SDK error: {error}"),
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                session.GetSerialNumber(machine.MachineNumber, out var serialNumber);

                logger.LogInformation(
                    "Successfully connected to machine {MachineNumber} at {IpAddress}:{Port}. Serial: {Serial}",
                    machine.MachineNumber,
                    machine.IpAddress,
                    machine.Port,
                    serialNumber);

                return Results.Ok(new ConnectResponse(
                    Connected: true,
                    SerialNumber: serialNumber,
                    Error: null));
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error connecting to machine {MachineNumber} at {IpAddress}:{Port}",
                    machine.MachineNumber,
                    machine.IpAddress,
                    machine.Port);

                return Results.Json(
                    new ConnectResponse(
                        Connected: false,
                        SerialNumber: null,
                        Error: "An unexpected error occurred while connecting to the machine."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ConnectToMachine")
        .WithSummary("Test connection to a ZKTeco device")
        .WithDescription("""
        Attempts to establish a connection to the specified ZKTeco attendance device
        and reports whether the connection succeeds.

        If the connection is successful, the device serial number is returned as
        proof of successful communication. The connection is closed immediately
        after the test.
        """)
        .Produces<ConnectResponse>(StatusCodes.Status200OK)
        .Produces<ConnectResponse>(StatusCodes.Status500InternalServerError);
    }
}
