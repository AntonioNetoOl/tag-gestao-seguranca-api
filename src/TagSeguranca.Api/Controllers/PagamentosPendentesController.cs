using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Pagamentos;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/pagamentos/pendentes")]
public class PagamentosPendentesController : BaseApiController
{
    private readonly TagDbContext _context;

    public PagamentosPendentesController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PagamentoPendenteResumoResponse>>> Listar(
        [FromQuery] string? busca,
        CancellationToken cancellationToken)
    {
        var query = _context.EventoFuncionarios
            .AsNoTracking()
            .Where(ef =>
                ef.Evento.Status == EventoStatus.Finalizado &&
                !ef.Pago &&
                !ef.Removido);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            query = query.Where(ef =>
                ef.Funcionario.NomeCompleto.ToLower().Contains(termo) ||
                ef.Funcionario.Rg.ToLower().Contains(termo) ||
                ef.Funcionario.Cpf.ToLower().Contains(termo));
        }

        var pendencias = await query
            .Select(ef => new
            {
                ef.FuncionarioId,
                ef.Funcionario.NomeCompleto,
                ef.Funcionario.Rg,
                ef.Funcionario.Cpf,
                ef.Funcionario.Funcao,
                ef.Funcionario.ChavePix,
                ef.Evento.ValorDiaria
            })
            .ToListAsync(cancellationToken);

        var resultado = pendencias
            .GroupBy(p => new
            {
                p.FuncionarioId,
                p.NomeCompleto,
                p.Rg,
                p.Cpf,
                p.Funcao,
                p.ChavePix
            })
            .Select(g => new PagamentoPendenteResumoResponse
            {
                FuncionarioId = g.Key.FuncionarioId,
                NomeCompleto = g.Key.NomeCompleto,
                Rg = g.Key.Rg,
                Cpf = g.Key.Cpf,
                Funcao = g.Key.Funcao,
                MeioPagamento = ObterMeioPagamento(g.Key.ChavePix, g.Key.Cpf),
                QuantidadeEventos = g.Count(),
                TotalHorasExtras = 0,
                ValorTotalPendente = g.Sum(x => x.ValorDiaria)
            })
            .OrderByDescending(x => x.ValorTotalPendente)
            .ThenBy(x => x.NomeCompleto)
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("{funcionarioId:guid}")]
    public async Task<ActionResult<PagamentoPendenteDetalheResponse>> ObterDetalhe(
        Guid funcionarioId,
        CancellationToken cancellationToken)
    {
        var funcionario = await _context.Funcionarios
            .AsNoTracking()
            .Where(f => f.Id == funcionarioId)
            .Select(f => new
            {
                f.Id,
                f.NomeCompleto,
                f.Rg,
                f.Cpf,
                f.Funcao,
                f.ChavePix
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (funcionario is null)
        {
            return ApiNotFound("Funcionário não encontrado.");
        }

        var eventos = await _context.EventoFuncionarios
            .AsNoTracking()
            .Where(ef =>
                ef.FuncionarioId == funcionarioId &&
                ef.Evento.Status == EventoStatus.Finalizado &&
                !ef.Pago &&
                !ef.Removido)
            .OrderBy(ef => ef.Evento.DataEvento)
            .ThenBy(ef => ef.Evento.HoraInicio)
            .Select(ef => new PagamentoPendenteEventoResponse
            {
                EventoFuncionarioId = ef.Id,
                EventoId = ef.EventoId,
                NomeEvento = ef.Evento.Nome,
                DataEvento = ef.Evento.DataEvento,
                CasaNome = ef.Evento.Casa.Nome,
                ValorDiaria = ef.Evento.ValorDiaria,
                ValorHoraExtra = ef.Evento.ValorHoraExtra,
                QuantidadeHorasExtras = 0,
                ValorTotal = ef.Evento.ValorDiaria
            })
            .ToListAsync(cancellationToken);

        var response = new PagamentoPendenteDetalheResponse
        {
            FuncionarioId = funcionario.Id,
            NomeCompleto = funcionario.NomeCompleto,
            Rg = funcionario.Rg,
            Cpf = funcionario.Cpf,
            Funcao = funcionario.Funcao,
            MeioPagamento = ObterMeioPagamento(funcionario.ChavePix, funcionario.Cpf),
            QuantidadeEventos = eventos.Count,
            TotalHorasExtras = eventos.Sum(e => e.QuantidadeHorasExtras),
            ValorTotalPendente = eventos.Sum(e => e.ValorTotal),
            Eventos = eventos
        };

        return Ok(response);
    }

    private static string ObterMeioPagamento(string? chavePix, string cpf)
    {
        if (!string.IsNullOrWhiteSpace(chavePix))
        {
            return chavePix.Trim();
        }

        return $"CPF: {cpf}";
    }
}
