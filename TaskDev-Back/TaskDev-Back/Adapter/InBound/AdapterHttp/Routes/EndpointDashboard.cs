using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointDashboard
{
    public static void MapEndpointDashboard(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/dashboard", async (HttpContext context, ISessionHandler sessionHandler, IDashboardUseCase dashboardUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await dashboardUseCase.GetDashboardAsync(user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        })
        .WithTags("Dashboard");

        routes.MapGet("/api/progress", async (HttpContext context, ISessionHandler sessionHandler, IDashboardUseCase dashboardUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await dashboardUseCase.GetProgressAsync(user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        })
        .WithTags("Dashboard");
    }
}
