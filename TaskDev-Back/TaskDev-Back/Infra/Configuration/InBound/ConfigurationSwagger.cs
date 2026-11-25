using Microsoft.OpenApi.Models;

namespace TaskDev.Api.Infra.Configuration.InBound;

public static class ConfigurationSwagger
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var title = configuration["Swagger:Title"] ?? "Task.dev API";
        var version = configuration["Swagger:Version"] ?? "v1";
        var description = configuration["Swagger:Description"] ?? "API de gerenciamento de tarefas minimalista e gamificada com Dapper, PostgreSQL e Agentes IA";

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description,
                Contact = new OpenApiContact
                {
                    Name = "Task.dev Team"
                }
            });

            // Configuração para suporte a testes no Swagger UI via Cookie de Sessão
            c.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = "taskdev-session",
                Description = "Cookie de autenticação de sessão gerado após login bem-sucedido."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "CookieAuth"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Swagger:Enabled", true);
        if (!enabled)
        {
            return app;
        }

        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task.dev API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Task.dev API - Documentação Interativa";
            c.DefaultModelsExpandDepth(1);
            c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
            c.DisplayRequestDuration();
            c.EnablePersistAuthorization();
        });

        return app;
    }
}
