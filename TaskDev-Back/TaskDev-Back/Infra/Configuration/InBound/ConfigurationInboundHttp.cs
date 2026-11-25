using System.Net;
using System.Text.Json;

namespace TaskDev.Api.Infra.Configuration.InBound;

public static class ConfigurationInboundHttp
{
    public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (BadHttpRequestException ex)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Corpo da requisição inválido ou ausente.", detail = ex.Message }));
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("TaskDev.Api.GlobalExceptionHandler");
                logger?.LogError(ex, "Exceção não tratada capturada pelo middleware global: {Message}", ex.Message);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        message = "Ocorreu um erro interno no servidor.",
                        traceId = context.TraceIdentifier
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            }
        });

        return app;
    }
}
