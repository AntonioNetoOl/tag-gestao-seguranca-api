using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Eventos.Services;
using TagSeguranca.Api.Infrastructure.BackgroundServices;
using TagSeguranca.Api.Application.Relatorios.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

builder.Services.AddDbContext<TagDbContext>(options =>
{
    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddControllers();

builder.Services.AddScoped<EventoFinalizacaoService>();
builder.Services.AddHostedService<EventosFinalizacaoBackgroundService>();

builder.Services.AddScoped<EscalaExcelService>();

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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TAG Gestão de Segurança API",
        Version = "v1",
        Description = "API para gestão de funcionários, casas, tipos de evento, eventos, escalas, pagamentos e dashboard da TAG."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TAG Gestão de Segurança API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "TAG Gestão de Segurança API",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/", () => Results.Ok(new
{
    name = "TAG Gestão de Segurança API",
    version = "0.1.0",
    status = "initial-setup"
}));

app.Run();