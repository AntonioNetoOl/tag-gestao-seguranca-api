using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Dashboard;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly TagDbContext _context;

    public DashboardController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResumoResponse>> ObterResumo(
        CancellationToken cancellationToken)
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);

        var queryEventosAtivos = _context.Eventos
            .AsNoTracking()
            .Where(e => e.Status != EventoStatus.Cancelado);

        var quantidadeEventosHoje = await queryEventosAtivos
            .CountAsync(e =>
                e.DataEvento >= hoje &&
                e.DataEvento < amanha,
                cancellationToken);

        var proximosEventos = await queryEventosAtivos
            .Where(e =>
                e.DataEvento >= hoje &&
                e.Status != EventoStatus.Finalizado)
            .OrderBy(e => e.DataEvento)
            .ThenBy(e => e.HoraInicio)
            .Take(10)
            .Select(e => new DashboardProximoEventoResponse
            {
                Id = e.Id,
                Nome = e.Nome,
                CasaNome = e.Casa.Nome,
                TipoEventoNome = e.TipoEvento.Nome,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                Status = e.Status.ToString(),
                QuantidadeFuncionarios = e.Funcionarios.Count(ef => !ef.Removido)
            })
            .ToListAsync(cancellationToken);

        var quantidadeFuncionariosPendentesPagamento = await _context.EventoFuncionarios
            .AsNoTracking()
            .Where(ef =>
                ef.Evento.Status == EventoStatus.Finalizado &&
                !ef.Pago &&
                !ef.Removido)
            .Select(ef => ef.FuncionarioId)
            .Distinct()
            .CountAsync(cancellationToken);

        var response = new DashboardResumoResponse
        {
            QuantidadeProximosEventos = proximosEventos.Count,
            QuantidadeEventosHoje = quantidadeEventosHoje,
            QuantidadeFuncionariosPendentesPagamento = quantidadeFuncionariosPendentesPagamento,
            ProximosEventos = proximosEventos
        };

        return Ok(response);
    }
}