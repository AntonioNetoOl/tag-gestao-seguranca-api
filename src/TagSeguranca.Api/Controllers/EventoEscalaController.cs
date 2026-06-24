using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Escalas;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

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

        if (evento.Status != EventoStatus.Rascunho)
        {
            return ApiConflict("Após finalizar a escala, não é possível adicionar novos funcionários.");
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

        var agora = DateTime.UtcNow;

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
            vinculoExistente.DataAlteracao = agora;
            vinculoExistente.UsuarioAlteracaoId = ObterUsuarioAtualId();

            RegistrarHistorico(
                eventoId,
                "ReativarFuncionario",
                vinculoExistente.Id,
                funcionarioAnteriorId: null,
                funcionarioNovoId: vinculoExistente.FuncionarioId,
                motivo: "Funcionário reativado na escala.",
                dataAcao: agora);

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
            DataCriacao = agora,
            UsuarioCriacaoId = ObterUsuarioAtualId()
        };

        _context.EventoFuncionarios.Add(eventoFuncionario);

        RegistrarHistorico(
            eventoId,
            "AdicionarFuncionario",
            eventoFuncionario.Id,
            funcionarioAnteriorId: null,
            funcionarioNovoId: request.FuncionarioId,
            motivo: null,
            dataAcao: agora);

        await _context.SaveChangesAsync(cancellationToken);

        var response = await BuscarResponsePorId(eventoFuncionario.Id, cancellationToken);

        return CreatedAtAction(nameof(Listar), new { eventoId }, response);
    }

    [HttpPost("finalizar")]
    public async Task<IActionResult> FinalizarEscala(
        Guid eventoId,
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
            return ApiConflict("Evento cancelado não pode ter escala finalizada.");
        }

        if (evento.Status == EventoStatus.Finalizado)
        {
            return ApiConflict("Evento finalizado não pode ter escala finalizada novamente.");
        }

        var possuiFuncionario = await _context.EventoFuncionarios
            .AnyAsync(ef => ef.EventoId == eventoId && !ef.Removido, cancellationToken);

        if (!possuiFuncionario)
        {
            return ApiConflict("A escala precisa ter pelo menos um funcionário.");
        }

        if (evento.Status == EventoStatus.Escalado)
        {
            return NoContent();
        }

        var agora = DateTime.UtcNow;
        evento.Status = EventoStatus.Escalado;
        evento.DataAlteracao = agora;

        RegistrarHistorico(
            eventoId,
            "FinalizarEscala",
            eventoFuncionarioId: null,
            funcionarioAnteriorId: null,
            funcionarioNovoId: null,
            motivo: "Escala finalizada pelo usuário.",
            dataAcao: agora);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("cancelar-finalizacao")]
    public async Task<IActionResult> CancelarFinalizacaoEscala(
        Guid eventoId,
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
            return ApiConflict("Evento cancelado não pode ter finalização de escala cancelada.");
        }

        if (evento.Status == EventoStatus.Finalizado)
        {
            return ApiConflict("Evento finalizado não pode ter a finalização da escala cancelada.");
        }

        if (evento.Status == EventoStatus.Rascunho)
        {
            return NoContent();
        }

        var possuiVinculoPago = await _context.EventoFuncionarios
            .AnyAsync(ef => ef.EventoId == eventoId && ef.Pago, cancellationToken);

        if (possuiVinculoPago)
        {
            return ApiConflict("Escala com funcionário pago não pode ter finalização cancelada.");
        }

        var agora = DateTime.UtcNow;
        evento.Status = EventoStatus.Rascunho;
        evento.DataAlteracao = agora;

        RegistrarHistorico(
            eventoId,
            "CancelarFinalizacaoEscala",
            eventoFuncionarioId: null,
            funcionarioAnteriorId: null,
            funcionarioNovoId: null,
            motivo: "Finalização da escala cancelada pelo usuário.",
            dataAcao: agora);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
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

        var escalaFinalizada = evento.Status is EventoStatus.Escalado or EventoStatus.Finalizado;
        var motivo = request?.MotivoRemocao?.Trim();

        if (escalaFinalizada && string.IsNullOrWhiteSpace(motivo))
        {
            return ApiBadRequest("Informe o motivo da remoção para escalas finalizadas.");
        }

        var agora = DateTime.UtcNow;
        var motivoFinal = string.IsNullOrWhiteSpace(motivo)
            ? "Removido da escala"
            : motivo;

        vinculo.Removido = true;
        vinculo.MotivoRemocao = motivoFinal;
        vinculo.DataAlteracao = agora;
        vinculo.UsuarioAlteracaoId = ObterUsuarioAtualId();

        RegistrarHistorico(
            eventoId,
            "RemoverFuncionario",
            vinculo.Id,
            funcionarioAnteriorId: funcionarioId,
            funcionarioNovoId: null,
            motivo: motivoFinal,
            dataAcao: agora);

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
        var motivo = string.IsNullOrWhiteSpace(request.Motivo)
            ? "Substituído por outro funcionário"
            : request.Motivo.Trim();

        vinculoAntigo.Removido = true;
        vinculoAntigo.MotivoRemocao = motivo;
        vinculoAntigo.DataAlteracao = agora;
        vinculoAntigo.UsuarioAlteracaoId = ObterUsuarioAtualId();

        EventoFuncionario vinculoNovo;

        if (vinculoNovoExistente is not null)
        {
            vinculoNovoExistente.Removido = false;
            vinculoNovoExistente.MotivoRemocao = null;
            vinculoNovoExistente.DataAlteracao = agora;
            vinculoNovoExistente.UsuarioAlteracaoId = ObterUsuarioAtualId();

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
                DataCriacao = agora,
                UsuarioCriacaoId = ObterUsuarioAtualId()
            };

            _context.EventoFuncionarios.Add(vinculoNovo);
        }

        RegistrarHistorico(
            eventoId,
            "SubstituirFuncionario",
            vinculoAntigo.Id,
            funcionarioAnteriorId: request.FuncionarioAntigoId,
            funcionarioNovoId: request.FuncionarioNovoId,
            motivo: motivo,
            dataAcao: agora,
            observacao: $"Novo vínculo: {vinculoNovo.Id}");

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = await BuscarResponsePorId(vinculoNovo.Id, cancellationToken);

        return Ok(response);
    }

    private void RegistrarHistorico(
        Guid eventoId,
        string acao,
        Guid? eventoFuncionarioId,
        Guid? funcionarioAnteriorId,
        Guid? funcionarioNovoId,
        string? motivo,
        DateTime dataAcao,
        string? observacao = null)
    {
        _context.EventoFuncionarioHistoricos.Add(new EventoFuncionarioHistorico
        {
            EventoId = eventoId,
            EventoFuncionarioId = eventoFuncionarioId,
            FuncionarioAnteriorId = funcionarioAnteriorId,
            FuncionarioNovoId = funcionarioNovoId,
            Acao = acao,
            Motivo = motivo,
            Observacao = observacao,
            UsuarioAcaoId = ObterUsuarioAtualId(),
            DataAcao = dataAcao
        });
    }

    private Guid? ObterUsuarioAtualId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(valor, out var usuarioId) ? usuarioId : null;
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
