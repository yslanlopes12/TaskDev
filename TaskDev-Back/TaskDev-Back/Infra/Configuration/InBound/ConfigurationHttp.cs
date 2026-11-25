using TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

namespace TaskDev.Api.Infra.Configuration.InBound;

public static class ConfigurationHttp
{
    public static IEndpointRouteBuilder AddEndpointsHttp(this IEndpointRouteBuilder routes)
    {
        // Rota raiz: redireciona para o Swagger UI se aberta em navegador (text/html) ou retorna JSON de status
        routes.MapGet("/", (HttpContext context) =>
        {
            if (context.Request.Headers.Accept.Any(a => a != null && a.Contains("text/html")))
            {
                return Results.Redirect("/swagger");
            }

            return Results.Ok(new
            {
                name = "Task.dev API",
                version = "1.0.0",
                status = "healthy",
                documentation = "/swagger",
                timestamp = DateTime.UtcNow
            });
        }).WithTags("Health").ExcludeFromDescription();

        // Health check endpoint padrão
        routes.MapGet("/api/health", () => Results.Ok(new
        {
            status = "UP",
            timestamp = DateTime.UtcNow
        })).WithTags("Health");

        // Mapeamento dos endpoints InBound da API
        routes.MapEndpointAuth();
        routes.MapEndpointSpaces();
        routes.MapEndpointTasks();
        routes.MapEndpointSubtasks();
        routes.MapEndpointDashboard();
        routes.MapEndpointAgent();
        routes.MapEndpointAdmin();

        return routes;
    }
}
