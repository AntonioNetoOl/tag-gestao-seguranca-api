using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Escalas;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Eventos.Services;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/eventos/{eventoId:guid}/funcionarios")]
public class EscalasController : BaseApiController
{
    private readonly TagDbContext _context;

    public EscalasController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventoFuncionarioResponse>>> Listar(
        Guid eventoId,
        [FromQuery] bool incluirRemovidos = false,
        CancellationToken cancellationToken = default)
    {
        var eventoExiste = await _context.Eventos
            .AnyAsync(e => e.Id == eventoId, cancellationToken);

        if (!eventoExiste)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        var query = _context.EventoFuncionarios
            .AsNoTracking()
            .Where(ef => ef.EventoId == eventoId);

        if (!incluirRemovidos)
        {
            query = query.Where(ef => !ef.Removido);
        }

        var funcionarios = await query
            .OrderBy(ef => ef.Funcionario.NomeCompleto)
            .Select(ef => new EventoFuncionarioResponse
            {
                Id = ef.Id,
                EventoId = ef.EventoId,
                FuncionarioId = ef.FuncionarioId,
                NomeCompleto = ef.Funcionario.NomeCompleto,
                Rg = ef.Funcionario.Rg,
                Cpf = ef.Funcionario.Cpf,
                Funcao = ef.Funcionario.Funcao,
                Pago = ef.Pago,
                Removido = ef.Removido,
                MotivoRemocao = ef.MotivoRemocao,
                DataCriacao = ef.DataCriacao,
                DataAlteracao = ef.DataAlteracao
            })
            .ToListAsync(cancellationToken);

        return Ok(funcionarios);
    }

    [HttpPost]
    public async Task<ActionResult<EventoFuncionarioResponse>> Adicionar(
        Guid eventoId,
        [FromBody] EventoFuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FuncionarioId == Guid.Empty)
        {
            return ApiBadRequest("O funcionário é obrigatório.");
        }

        var evento = await _context.Eventos
            .FirstOrDefaultAsync(e => e.Id == eventoId, cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        if (evento.Status == EventoStatus.Cancelado)
        {
            return ApiConflict("Evento cancelado não pode receber funcionários.");
        }

        var funcionario = await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == request.FuncionarioId, cancellationToken);

        if (funcionario is null)
        {
            return ApiBadRequest("Funcionário informado não existe.");
        }

        if (!funcionario.Ativo)
        {
            return ApiConflict("Funcionário inativo não pode ser vinculado a novos eventos.");
        }

        var vinculoExistente = await _context.EventoFuncionarios
            .FirstOrDefaultAsync(
                ef => ef.EventoId == eventoId && ef.FuncionarioId == request.FuncionarioId,
                cancellationToken);

        if (vinculoExistente is not null)
        {
            if (!vinculoExistente.Removido)
            {
                return ApiConflict("Funcionário já está vinculado a este evento.");
            }

            if (vinculoExistente.Pago)
            {
                return ApiConflict("Funcionário já pago não pode ser reativado na escala.");
            }

            vinculoExistente.Removido = false;
            vinculoExistente.MotivoRemocao = null;
            vinculoExistente.DataAlteracao = DateTime.UtcNow;

            if (evento.Status == EventoStatus.Rascunho)
            {
                evento.Status = EventoStatus.Escalado;
                evento.DataAlteracao = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var responseReativado = await BuscarResponsePorId(vinculoExistente.Id, cancellationToken);

            return Ok(responseReativado);
        }

        var eventoFuncionario = new EventoFuncionario
        {
            EventoId = eventoId,
            FuncionarioId = request.FuncionarioId,
            Pago = false,
            Removido = false,
            DataCriacao = DateTime.UtcNow
        };

        _context.EventoFuncionarios.Add(eventoFuncionario);

        if (evento.Status == EventoStatus.Rascunho)
        {
            evento.Status = EventoStatus.Escalado;
            evento.DataAlteracao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = await BuscarResponsePorId(eventoFuncionario.Id, cancellationToken);

        return CreatedAtAction(nameof(Listar), new { eventoId }, response);
    }

    [HttpDelete("{funcionarioId:guid}")]
    public async Task<IActionResult> Remover(
        Guid eventoId,
        Guid funcionarioId,
        [FromBody] RemoverFuncionarioEventoRequest? request,
        CancellationToken cancellationToken)
    {
        var evento = await _context.Eventos
            .FirstOrDefaultAsync(e => e.Id == eventoId, cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        if (evento.Status == EventoStatus.Cancelado)
        {
            return ApiConflict("Evento cancelado não pode ter escala alterada.");
        }

        var vinculo = await _context.EventoFuncionarios
            .FirstOrDefaultAsync(
                ef => ef.EventoId == eventoId && ef.FuncionarioId == funcionarioId,
                cancellationToken);

        if (vinculo is null || vinculo.Removido)
        {
            return ApiNotFound("Funcionário não encontrado na escala do evento.");
        }

        if (vinculo.Pago)
        {
            return ApiConflict("Funcionário já pago não pode ser removido da escala.");
        }

        vinculo.Removido = true;
        vinculo.MotivoRemocao = string.IsNullOrWhiteSpace(request?.MotivoRemocao)
            ? "Removido da escala"
            : request.MotivoRemocao.Trim();

        vinculo.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("substituir")]
    public async Task<ActionResult<EventoFuncionarioResponse>> Substituir(
    Guid eventoId,
    [FromBody] SubstituirFuncionarioEventoRequest request,
    CancellationToken cancellationToken)
    {
        if (request.FuncionarioAntigoId == Guid.Empty)
        {
            return ApiBadRequest("O funcionário antigo é obrigatório.");
        }

        if (request.FuncionarioNovoId == Guid.Empty)
        {
            return ApiBadRequest("O funcionário novo é obrigatório.");
        }

        if (request.FuncionarioAntigoId == request.FuncionarioNovoId)
        {
            return ApiBadRequest("O funcionário novo deve ser diferente do funcionário antigo.");
        }

        var evento = await _context.Eventos
            .FirstOrDefaultAsync(e => e.Id == eventoId, cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        if (evento.Status == EventoStatus.Cancelado)
        {
            return ApiConflict("Evento cancelado não pode ter escala alterada.");
        }

        var vinculoAntigo = await _context.EventoFuncionarios
            .FirstOrDefaultAsync(
                ef => ef.EventoId == eventoId &&
                      ef.FuncionarioId == request.FuncionarioAntigoId,
                cancellationToken);

        if (vinculoAntigo is null || vinculoAntigo.Removido)
        {
            return ApiNotFound("Funcionário antigo não encontrado na escala do evento.");
        }

        if (vinculoAntigo.Pago)
        {
            return ApiConflict("Funcionário já pago não pode ser substituído.");
        }

        var funcionarioNovo = await _context.Funcionarios
            .FirstOrDefaultAsync(
                f => f.Id == request.FuncionarioNovoId,
                cancellationToken);

        if (funcionarioNovo is null)
        {
            return ApiBadRequest("Funcionário novo informado não existe.");
        }

        if (!funcionarioNovo.Ativo)
        {
            return ApiConflict("Funcionário inativo não pode ser vinculado a novos eventos.");
        }

        var vinculoNovoExistente = await _context.EventoFuncionarios
            .FirstOrDefaultAsync(
                ef => ef.EventoId == eventoId &&
                      ef.FuncionarioId == request.FuncionarioNovoId,
                cancellationToken);

        if (vinculoNovoExistente is not null && !vinculoNovoExistente.Removido)
        {
            return ApiConflict("Funcionário novo já está vinculado a este evento.");
        }

        if (vinculoNovoExistente is not null && vinculoNovoExistente.Pago)
        {
            return ApiConflict("Funcionário novo já possui vínculo pago neste evento e não pode ser reativado.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var agora = DateTime.UtcNow;

        vinculoAntigo.Removido = true;
        vinculoAntigo.MotivoRemocao = string.IsNullOrWhiteSpace(request.Motivo)
            ? "Substituído por outro funcionário"
            : request.Motivo.Trim();
        vinculoAntigo.DataAlteracao = agora;

        EventoFuncionario vinculoNovo;

        if (vinculoNovoExistente is not null)
        {
            vinculoNovoExistente.Removido = false;
            vinculoNovoExistente.MotivoRemocao = null;
            vinculoNovoExistente.DataAlteracao = agora;

            vinculoNovo = vinculoNovoExistente;
        }
        else
        {
            vinculoNovo = new EventoFuncionario
            {
                EventoId = eventoId,
                FuncionarioId = request.FuncionarioNovoId,
                Pago = false,
                Removido = false,
                DataCriacao = agora
            };

            _context.EventoFuncionarios.Add(vinculoNovo);
        }

        if (evento.Status == EventoStatus.Rascunho)
        {
            evento.Status = EventoStatus.Escalado;
            evento.DataAlteracao = agora;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = await BuscarResponsePorId(vinculoNovo.Id, cancellationToken);

        return Ok(response);
    }

    private async Task<EventoFuncionarioResponse> BuscarResponsePorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.EventoFuncionarios
            .AsNoTracking()
            .Where(ef => ef.Id == id)
            .Select(ef => new EventoFuncionarioResponse
            {
                Id = ef.Id,
                EventoId = ef.EventoId,
                FuncionarioId = ef.FuncionarioId,
                NomeCompleto = ef.Funcionario.NomeCompleto,
                Rg = ef.Funcionario.Rg,
                Cpf = ef.Funcionario.Cpf,
                Funcao = ef.Funcionario.Funcao,
                Pago = ef.Pago,
                Removido = ef.Removido,
                MotivoRemocao = ef.MotivoRemocao,
                DataCriacao = ef.DataCriacao,
                DataAlteracao = ef.DataAlteracao
            })
            .FirstAsync(cancellationToken);
    }
}