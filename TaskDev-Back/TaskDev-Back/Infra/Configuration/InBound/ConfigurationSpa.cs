namespace TaskDev.Api.Infra.Configuration.InBound;

internal static class ConfigurationSpa
{
    public static void UseSpaFrontend(this WebApplication app)
    {
        app.UseWhen(context => context.GetEndpoint() is null && !context.Request.Path.StartsWithSegments("/api"), frontend =>
        {
            frontend.UseStaticFiles();
            frontend.Use(async (context, next) =>
            {
                var request = context.Request;
                if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) &&
                    !Path.HasExtension(request.Path.Value?.TrimEnd('/')))
                {
                    var index = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
                    if (index.PhysicalPath is { } indexPath)
                    {
                        await TypedResults.PhysicalFile(indexPath, "text/html", lastModified: index.LastModified)
                            .ExecuteAsync(context);
                        return;
                    }
                }

                await next(context);
            });
        });
    }
}
