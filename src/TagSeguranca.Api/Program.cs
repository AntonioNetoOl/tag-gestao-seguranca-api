using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Eventos.Services;
using TagSeguranca.Api.Infrastructure.BackgroundServices;
using TagSeguranca.Api.Application.Relatorios.Services;
using Microsoft.OpenApi;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TagSeguranca.Api.Application.Auth;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Seed;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

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
builder.Services.AddScoped<PagamentosExcelService>();
builder.Services.AddScoped<RelatoriosPdfService>();

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

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("Configuração Jwt:Secret não encontrada.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await UsuarioMasterSeeder.SeedAsync(scope.ServiceProvider);
}

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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers().RequireAuthorization();

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