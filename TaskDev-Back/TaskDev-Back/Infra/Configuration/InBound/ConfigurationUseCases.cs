using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.UseCases;

namespace TaskDev.Api.Infra.Configuration.InBound;

public static class ConfigurationUseCases
{
    public static IServiceCollection AddUseCasesConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<ISpaceUseCase, SpaceUseCase>();
        services.AddScoped<ITaskUseCase, TaskUseCase>();
        services.AddScoped<ISubtaskUseCase, SubtaskUseCase>();
        services.AddScoped<IDashboardUseCase, DashboardUseCase>();
        services.AddScoped<IAgentUseCase, AgentUseCase>();
        services.AddScoped<IAdminUseCase, AdminUseCase>();

        return services;
    }
}
