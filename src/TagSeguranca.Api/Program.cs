var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "TAG Gestão de Segurança API",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/", () => Results.Ok(new
{
    name = "TAG Gestão de Segurança API",
    version = "0.1.0",
    status = "initial-setup"
}));

app.Run();
