using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Eventos.Services;

public class EventoFinalizacaoService
{
    private readonly TagDbContext _context;

    public EventoFinalizacaoService(TagDbContext context)
    {
        _context = context;
    }

    public async Task<EventoFinalizacaoResultado> FinalizarEventosVencidosAsync(
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.Now;

        var eventosEscalados = await _context.Eventos
            .Where(e => e.Status == EventoStatus.Escalado)
            .ToListAsync(cancellationToken);

        var eventosParaFinalizar = eventosEscalados
            .Where(e => ObterDataHoraFim(e.DataEvento, e.HoraInicio, e.HoraFim) <= agora)
            .ToList();

        foreach (var evento in eventosParaFinalizar)
        {
            evento.Status = EventoStatus.Finalizado;
            evento.DataAlteracao = DateTime.UtcNow;
        }

        if (eventosParaFinalizar.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new EventoFinalizacaoResultado
        {
            QuantidadeEventosFinalizados = eventosParaFinalizar.Count,
            DataHoraProcessamento = agora
        };
    }

    private static DateTime ObterDataHoraFim(
        DateTime dataEvento,
        TimeSpan horaInicio,
        TimeSpan horaFim)
    {
        var dataBase = dataEvento.Date;

        if (horaFim < horaInicio)
        {
            return dataBase.AddDays(1).Add(horaFim);
        }

        return dataBase.Add(horaFim);
    }
}