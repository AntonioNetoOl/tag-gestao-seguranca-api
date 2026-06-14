using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Pagamentos;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/pagamentos")]
public class PagamentosController : BaseApiController
{
    private readonly TagDbContext _context;

    public PagamentosController(TagDbContext context)
    {
        _context = context;
    }

    [HttpPost("confirmar")]
    public async Task<ActionResult<PagamentoConfirmadoResponse>> Confirmar(
        [FromBody] ConfirmarPagamentoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var funcionario = await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == request.FuncionarioId, cancellationToken);

        if (funcionario is null)
        {
            return ApiNotFound("Funcionário não encontrado.");
        }

        var pendencias = await _context.EventoFuncionarios
            .Include(ef => ef.Evento)
                .ThenInclude(e => e.Casa)
            .Where(ef =>
                ef.FuncionarioId == request.FuncionarioId &&
                ef.Evento.Status == EventoStatus.Finalizado &&
                !ef.Pago &&
                !ef.Removido)
            .OrderBy(ef => ef.Evento.DataEvento)
            .ThenBy(ef => ef.Evento.HoraInicio)
            .ToListAsync(cancellationToken);

        if (pendencias.Count == 0)
        {
            return ApiConflict("Funcionário não possui pagamentos pendentes.");
        }

        var idsPendentes = pendencias
            .Select(p => p.Id)
            .OrderBy(id => id)
            .ToList();

        var idsRequest = request.Itens
            .Select(i => i.EventoFuncionarioId)
            .OrderBy(id => id)
            .ToList();

        if (!idsPendentes.SequenceEqual(idsRequest))
        {
            return ApiBadRequest("Pagamento não pode ser parcial. Todos os eventos pendentes do funcionário devem ser enviados.");
        }

        var itensDuplicados = request.Itens
            .GroupBy(i => i.EventoFuncionarioId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (itensDuplicados.Count > 0)
        {
            return ApiBadRequest("Existem eventos duplicados na solicitação de pagamento.");
        }

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var itensPorEventoFuncionario = request.Itens
            .ToDictionary(i => i.EventoFuncionarioId, i => i);

        var pagamento = new Pagamento
        {
            FuncionarioId = funcionario.Id,
            DataPagamento = DateTime.UtcNow,
            Status = PagamentoStatus.Confirmado,
            DataCriacao = DateTime.UtcNow,
            QuantidadeEventos = pendencias.Count
        };

        foreach (var pendencia in pendencias)
        {
            var itemRequest = itensPorEventoFuncionario[pendencia.Id];

            var valorDiariaPago = pendencia.Evento.ValorDiaria;
            var valorHoraExtraPago = pendencia.Evento.ValorHoraExtra;
            var quantidadeHorasExtras = itemRequest.QuantidadeHorasExtras;
            var valorTotalItem = valorDiariaPago + (quantidadeHorasExtras * valorHoraExtraPago);

            var pagamentoItem = new PagamentoItem
            {
                Pagamento = pagamento,
                EventoFuncionarioId = pendencia.Id,
                ValorDiariaPago = valorDiariaPago,
                ValorHoraExtraPago = valorHoraExtraPago,
                QuantidadeHorasExtras = quantidadeHorasExtras,
                ValorTotalItem = valorTotalItem
            };

            pagamento.Itens.Add(pagamentoItem);

            pendencia.Pago = true;
            pendencia.DataAlteracao = DateTime.UtcNow;
        }

        pagamento.TotalHorasExtras = pagamento.Itens.Sum(i => i.QuantidadeHorasExtras);
        pagamento.ValorTotal = pagamento.Itens.Sum(i => i.ValorTotalItem);

        _context.Pagamentos.Add(pagamento);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = await BuscarPagamentoResponsePorId(pagamento.Id, cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = pagamento.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PagamentoConfirmadoResponse>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await BuscarPagamentoResponsePorId(id, cancellationToken);

        if (response is null)
        {
            return ApiNotFound("Pagamento não encontrado.");
        }

        return Ok(response);
    }

    private async Task<PagamentoConfirmadoResponse?> BuscarPagamentoResponsePorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.Pagamentos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PagamentoConfirmadoResponse
            {
                Id = p.Id,
                FuncionarioId = p.FuncionarioId,
                NomeCompleto = p.Funcionario.NomeCompleto,
                Rg = p.Funcionario.Rg,
                Cpf = p.Funcionario.Cpf,
                MeioPagamento = ObterMeioPagamento(p.Funcionario.ChavePix, p.Funcionario.Cpf),
                DataPagamento = p.DataPagamento,
                ValorTotal = p.ValorTotal,
                TotalHorasExtras = p.TotalHorasExtras,
                QuantidadeEventos = p.QuantidadeEventos,
                Status = p.Status.ToString(),
                Itens = p.Itens
                    .OrderBy(i => i.EventoFuncionario.Evento.DataEvento)
                    .Select(i => new PagamentoConfirmadoItemResponse
                    {
                        Id = i.Id,
                        EventoFuncionarioId = i.EventoFuncionarioId,
                        EventoId = i.EventoFuncionario.EventoId,
                        NomeEvento = i.EventoFuncionario.Evento.Nome,
                        DataEvento = i.EventoFuncionario.Evento.DataEvento,
                        CasaNome = i.EventoFuncionario.Evento.Casa.Nome,
                        ValorDiariaPago = i.ValorDiariaPago,
                        ValorHoraExtraPago = i.ValorHoraExtraPago,
                        QuantidadeHorasExtras = i.QuantidadeHorasExtras,
                        ValorTotalItem = i.ValorTotalItem
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ValidarRequest(ConfirmarPagamentoRequest request)
    {
        if (request.FuncionarioId == Guid.Empty)
        {
            return "O funcionário é obrigatório.";
        }

        if (request.Itens.Count == 0)
        {
            return "É necessário informar os eventos do pagamento.";
        }

        if (request.Itens.Any(i => i.EventoFuncionarioId == Guid.Empty))
        {
            return "Todos os itens devem possuir um evento de funcionário válido.";
        }

        if (request.Itens.Any(i => i.QuantidadeHorasExtras < 0))
        {
            return "A quantidade de horas extras não pode ser negativa.";
        }

        return null;
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