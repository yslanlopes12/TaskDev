using TaskDev.Api.Adapter.OutBound.AdapterSql.Migrations;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Ports.OutBound;
using TaskDev.Api.Infra.Configuration.InBound;
using TaskDev.Api.Infra.Configuration.OutBound;

var builder = WebApplication.CreateBuilder(args);

// Configuração InBound (Swagger, CORS, Session & Security, UseCases)
builder.Services.AddSwaggerConfiguration(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddSessionAndSecurity(builder.Configuration);
builder.Services.AddUseCasesConfiguration();

// Configuração OutBound (SQL Adapter Postgres/Dapper, HTTP Client Adapter)
builder.Services.AddSqlAdapterConfiguration();
builder.Services.AddHttpClientAdapterConfiguration();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    _ = scope.ServiceProvider.GetRequiredService<IDatabaseConnectionFactory>();
    _ = scope.ServiceProvider.GetRequiredService<ISecretEncryptor>();
    if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().Migrate();
    }
}

// Pipeline HTTP InBound
app.UseGlobalErrorHandling();
app.UseSwaggerConfiguration(builder.Configuration);
app.UseCorsConfiguration();
app.UseRateLimiter();
app.UseSessionAndCsrf();

// Mapeamento dos Endpoints HTTP InBound
app.AddEndpointsHttp();

app.Run();
