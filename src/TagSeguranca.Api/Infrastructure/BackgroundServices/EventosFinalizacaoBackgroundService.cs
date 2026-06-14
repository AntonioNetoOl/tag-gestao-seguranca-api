using TagSeguranca.Api.Application.Eventos.Services;

namespace TagSeguranca.Api.Infrastructure.BackgroundServices;

public class EventosFinalizacaoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventosFinalizacaoBackgroundService> _logger;

    public EventosFinalizacaoBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventosFinalizacaoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de finalização automática de eventos iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<EventoFinalizacaoService>();

                var resultado = await service.FinalizarEventosVencidosAsync(stoppingToken);

                if (resultado.QuantidadeEventosFinalizados > 0)
                {
                    _logger.LogInformation(
                        "{Quantidade} evento(s) finalizado(s) automaticamente em {DataHora}.",
                        resultado.QuantidadeEventosFinalizados,
                        resultado.DataHoraProcessamento);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar finalização automática de eventos.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}