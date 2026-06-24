using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Eventos.Services;

public class EventoConflitoService
{
    private readonly TagDbContext _context;

    public EventoConflitoService(TagDbContext context)
    {
        _context = context;
    }

    public async Task<string?> ValidarConflitoCasaAsync(
        Guid? eventoIdIgnorado,
        Guid casaId,
        DateTime dataEvento,
        TimeSpan horaInicio,
        TimeSpan horaFim,
        CancellationToken cancellationToken)
    {
        var intervalo = CriarIntervalo(dataEvento, horaInicio, horaFim);
        var dataMinima = intervalo.Inicio.Date.AddDays(-1);
        var dataMaxima = intervalo.Fim.Date;

        var eventos = await _context.Eventos
            .AsNoTracking()
            .Where(e =>
                e.CasaId == casaId &&
                e.Status != EventoStatus.Cancelado &&
                (!eventoIdIgnorado.HasValue || e.Id != eventoIdIgnorado.Value) &&
                e.DataEvento >= dataMinima &&
                e.DataEvento <= dataMaxima)
            .OrderBy(e => e.DataEvento)
            .ThenBy(e => e.HoraInicio)
            .ToListAsync(cancellationToken);

        var conflito = eventos.FirstOrDefault(e => Sobrepoe(intervalo, CriarIntervalo(e.DataEvento, e.HoraInicio, e.HoraFim)));

        if (conflito is null)
        {
            return null;
        }

        return $"Já existe um evento para esta casa no mesmo período: \"{conflito.Nome}\" em {FormatarPeriodoEvento(conflito)} das {FormatarHora(conflito.HoraInicio)} às {FormatarHora(conflito.HoraFim)}.";
    }

    public async Task<string?> ValidarConflitoFuncionarioAsync(
        Guid eventoIdIgnorado,
        Guid funcionarioId,
        string? funcionarioNome,
        DateTime dataEvento,
        TimeSpan horaInicio,
        TimeSpan horaFim,
        CancellationToken cancellationToken)
    {
        var intervalo = CriarIntervalo(dataEvento, horaInicio, horaFim);
        var dataMinima = intervalo.Inicio.Date.AddDays(-1);
        var dataMaxima = intervalo.Fim.Date;

        var vinculos = await _context.EventoFuncionarios
            .AsNoTracking()
            .Include(ef => ef.Evento)
                .ThenInclude(e => e.Casa)
            .Where(ef =>
                ef.FuncionarioId == funcionarioId &&
                !ef.Removido &&
                ef.EventoId != eventoIdIgnorado &&
                ef.Evento.Status != EventoStatus.Cancelado &&
                ef.Evento.DataEvento >= dataMinima &&
                ef.Evento.DataEvento <= dataMaxima)
            .OrderBy(ef => ef.Evento.DataEvento)
            .ThenBy(ef => ef.Evento.HoraInicio)
            .ToListAsync(cancellationToken);

        var conflito = vinculos.FirstOrDefault(ef => Sobrepoe(intervalo, CriarIntervalo(ef.Evento.DataEvento, ef.Evento.HoraInicio, ef.Evento.HoraFim)));

        if (conflito is null)
        {
            return null;
        }

        var nome = string.IsNullOrWhiteSpace(funcionarioNome)
            ? "O funcionário"
            : $"O funcionário {funcionarioNome.Trim()}";

        return $"{nome} já está vinculado ao evento \"{conflito.Evento.Nome}\" na casa {conflito.Evento.Casa.Nome}, em {FormatarPeriodoEvento(conflito.Evento)} das {FormatarHora(conflito.Evento.HoraInicio)} às {FormatarHora(conflito.Evento.HoraFim)}.";
    }

    public async Task<string?> ValidarConflitosFuncionariosDaEscalaAsync(
        Guid eventoId,
        DateTime dataEvento,
        TimeSpan horaInicio,
        TimeSpan horaFim,
        CancellationToken cancellationToken)
    {
        var vinculos = await _context.EventoFuncionarios
            .AsNoTracking()
            .Include(ef => ef.Funcionario)
            .Where(ef => ef.EventoId == eventoId && !ef.Removido)
            .OrderBy(ef => ef.Funcionario.NomeCompleto)
            .ToListAsync(cancellationToken);

        foreach (var vinculo in vinculos)
        {
            var conflito = await ValidarConflitoFuncionarioAsync(
                eventoId,
                vinculo.FuncionarioId,
                vinculo.Funcionario.NomeCompleto,
                dataEvento,
                horaInicio,
                horaFim,
                cancellationToken);

            if (conflito is not null)
            {
                return conflito;
            }
        }

        return null;
    }

    private static EventoIntervalo CriarIntervalo(DateTime dataEvento, TimeSpan horaInicio, TimeSpan horaFim)
    {
        var inicio = dataEvento.Date.Add(horaInicio);
        var fim = dataEvento.Date.Add(horaFim);

        if (fim < inicio)
        {
            fim = fim.AddDays(1);
        }

        return new EventoIntervalo(inicio, fim);
    }

    private static bool Sobrepoe(EventoIntervalo atual, EventoIntervalo existente)
    {
        return atual.Inicio < existente.Fim && existente.Inicio < atual.Fim;
    }

    private static string FormatarPeriodoEvento(Evento evento)
    {
        var dataInicio = evento.DataEvento.ToString("dd/MM/yyyy");

        if (evento.HoraFim < evento.HoraInicio)
        {
            return $"{dataInicio} - {evento.DataEvento.AddDays(1):dd/MM/yyyy}";
        }

        return dataInicio;
    }

    private static string FormatarHora(TimeSpan hora)
    {
        return hora.ToString("hh\\:mm");
    }

    private readonly record struct EventoIntervalo(DateTime Inicio, DateTime Fim);
}
