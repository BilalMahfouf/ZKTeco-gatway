using Carter;
using Microsoft.AspNetCore.Mvc;
using ZKTecoGatway.ZKTeco;

namespace ZKTecoGatway.Endpoints;

public sealed class Endpoint : ICarterModule
{
    public sealed record Request(
        DateOnly From,
        DateOnly To,
        string IpAddress,
        int Port,
        int MachineNumber);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/zkteco")
            .WithTags("ZKTeco");

        group.MapPost("/", async (
            [FromBody] Request request,
            ZKTecoAttendanceMachineReader reader,
            CancellationToken ct) =>
        {
            var response = await reader.GetLogsAsync(
     new AttendenceMachine(
         request.MachineNumber,
         request.IpAddress,
         request.Port),
     request.From,
     request.To,
     null,
     ct);

            if (response is null)
                return Results.NotFound();

            return Results.Ok(response);
        })
        .WithName("GetZKTecoData")
        .WithSummary("Retrieve attendance logs from a ZKTeco device")
        .WithDescription("""
        Retrieves attendance logs from the specified ZKTeco attendance device
        for the requested date range.

        The endpoint connects directly to the ZKTeco device using its IP address,
        port, and machine number, then reads the attendance logs recorded by the
        device.

        All employees' attendance logs are returned when no enrollment number
        is specified.
        """)
        .Produces<IReadOnlyList<RawAttendanceLog>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);


        group.MapPost("/{enrollmentNumber}", async (
            string enrollmentNumber,
            [FromBody] Request request,
            ZKTecoAttendanceMachineReader reader,
            CancellationToken ct) =>
        {
            var response = await reader.GetLogsAsync(
                new AttendenceMachine(
                    request.MachineNumber,
                    request.IpAddress,
                    request.Port),
                request.From,
                request.To,
                enrollmentNumber,
                ct);

            if (response is null)
                return Results.NotFound();

            return Results.Ok(response);
        })
        .WithName("GetZKTecoDataByEnrollmentNumber")
        .WithSummary("Retrieve attendance logs for a specific employee")
        .WithDescription("""
        Retrieves attendance logs from the specified ZKTeco attendance device
        for a specific employee.

        The employee is identified using the enrollment number stored on the
        ZKTeco device. Only attendance logs belonging to that enrollment number
        and falling within the requested date range are returned.
        """)
        .Produces<IReadOnlyList<RawAttendanceLog>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}
