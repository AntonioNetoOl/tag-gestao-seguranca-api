using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Eventos;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Eventos.Services;
using TagSeguranca.Api.Application.Relatorios.Services;
using TagSeguranca.Api.Application.Common.Pagination;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/eventos")]
public class EventosController : BaseApiController
{
    private readonly TagDbContext _context;
    private readonly EventoFinalizacaoService _finalizacaoService;
    private readonly EscalaExcelService _escalaExcelService;
    private readonly RelatoriosPdfService _relatoriosPdfService;

    public EventosController(
        TagDbContext context,
        EscalaExcelService escalaExcelService,
        EventoFinalizacaoService eventoFinalizacaoService,
        RelatoriosPdfService relatoriosPdfService)
    {
        _context = context;
        _escalaExcelService = escalaExcelService;
        _finalizacaoService = eventoFinalizacaoService;
        _relatoriosPdfService = relatoriosPdfService;
    }

    [HttpPost("finalizar-vencidos")]
    public async Task<ActionResult<EventoFinalizacaoResultado>> FinalizarVencidos(
        CancellationToken cancellationToken)
    {
        var resultado = await _finalizacaoService
            .FinalizarEventosVencidosAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventoResponse>>> Listar(
        [FromQuery] Guid? casaId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? nome,
        [FromQuery] EventoStatus? status,
        [FromQuery] bool apenasOperacao,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.Eventos
            .AsNoTracking()
            .AsQueryable();

        if (apenasOperacao)
        {
            var limiteFinalizados = DateTime.UtcNow.AddHours(-24);
            var limiteData = limiteFinalizados.Date;
            var limiteHora = limiteFinalizados.TimeOfDay;

            query = query.Where(e =>
                e.Status != EventoStatus.Cancelado &&
                (e.Status != EventoStatus.Finalizado ||
                 e.DataEvento > limiteData ||
                 (e.DataEvento == limiteData && e.HoraFim >= limiteHora)));
        }

        if (casaId.HasValue)
        {
            query = query.Where(e => e.CasaId == casaId.Value);
        }

        if (dataInicio.HasValue)
        {
            query = query.Where(e => e.DataEvento.Date >= dataInicio.Value.Date);
        }

        if (dataFim.HasValue)
        {
            query = query.Where(e => e.DataEvento.Date <= dataFim.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();

            query = query.Where(e =>
                e.Nome.ToLower().Contains(termo));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var eventos = await query
            .OrderBy(e => e.DataEvento)
            .ThenBy(e => e.HoraInicio)
            .Select(e => new EventoResponse
            {
                Id = e.Id,
                CasaId = e.CasaId,
                CasaNome = e.Casa.Nome,
                TipoEventoId = e.TipoEventoId,
                TipoEventoNome = e.TipoEvento.Nome,
                Nome = e.Nome,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                ValorDiaria = e.ValorDiaria,
                ValorHoraExtra = e.ValorHoraExtra,
                Status = e.Status.ToString(),
                QuantidadeFuncionarios = e.Funcionarios.Count(f => !f.Removido),
                DataCriacao = e.DataCriacao,
                DataAlteracao = e.DataAlteracao
            })
            .ToPagedResponseAsync(
                pagination.Page,
                pagination.PageSize,
                cancellationToken);

        return Ok(eventos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventoResponse>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var evento = await _context.Eventos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventoResponse
            {
                Id = e.Id,
                CasaId = e.CasaId,
                CasaNome = e.Casa.Nome,
                TipoEventoId = e.TipoEventoId,
                TipoEventoNome = e.TipoEvento.Nome,
                Nome = e.Nome,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                ValorDiaria = e.ValorDiaria,
                ValorHoraExtra = e.ValorHoraExtra,
                Status = e.Status.ToString(),
                QuantidadeFuncionarios = e.Funcionarios.Count(f => !f.Removido),
                DataCriacao = e.DataCriacao,
                DataAlteracao = e.DataAlteracao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        return Ok(evento);
    }

    [HttpGet("{id:guid}/escala/excel")]
    public async Task<IActionResult> ExportarEscalaExcel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var arquivo = await _escalaExcelService
            .GerarEscalaEventoAsync(id, cancellationToken);

        if (arquivo is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        var nomeArquivo = $"escala-evento-{id}.xlsx";

        return File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomeArquivo);
    }

    [HttpGet("{id:guid}/escala/pdf")]
    public async Task<IActionResult> ExportarEscalaPdf(
        Guid id,
        CancellationToken cancellationToken)
    {
        var arquivo = await _relatoriosPdfService.GerarEscalaEventoAsync(id, cancellationToken);

        if (arquivo is null)
        {
            return NotFound(new
            {
                mensagem = "Evento não encontrado."
            });
        }

        var nomeArquivo = $"escala-evento-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";

        return File(
            arquivo,
            "application/pdf",
            nomeArquivo
        );
    }

    [HttpPost]
    public async Task<ActionResult<EventoResponse>> Criar(
        [FromBody] EventoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var casaExiste = await _context.Casas
            .AnyAsync(c => c.Id == request.CasaId, cancellationToken);

        if (!casaExiste)
        {
            return ApiBadRequest("A casa informada não existe.");
        }

        var tipoEventoExiste = await _context.TiposEvento
            .AnyAsync(t => t.Id == request.TipoEventoId, cancellationToken);

        if (!tipoEventoExiste)
        {
            return ApiBadRequest("O tipo de evento informado não existe.");
        }

        var evento = new Evento
        {
            CasaId = request.CasaId,
            TipoEventoId = request.TipoEventoId,
            Nome = request.Nome.Trim(),
            DataEvento = request.DataEvento.Date,
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim,
            ValorDiaria = request.ValorDiaria,
            ValorHoraExtra = request.ValorHoraExtra,
            Status = EventoStatus.Rascunho,
            DataCriacao = DateTime.UtcNow
        };

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync(cancellationToken);

        var response = await BuscarResponsePorId(evento.Id, cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = evento.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventoResponse>> Atualizar(
        Guid id,
        [FromBody] EventoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var evento = await _context.Eventos
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        if (evento.Status == EventoStatus.Cancelado)
        {
            return ApiConflict("Evento cancelado não pode ser alterado.");
        }

        var casaExiste = await _context.Casas
            .AnyAsync(c => c.Id == request.CasaId, cancellationToken);

        if (!casaExiste)
        {
            return ApiBadRequest("A casa informada não existe.");
        }

        var tipoEventoExiste = await _context.TiposEvento
            .AnyAsync(t => t.Id == request.TipoEventoId, cancellationToken);

        if (!tipoEventoExiste)
        {
            return ApiBadRequest("O tipo de evento informado não existe.");
        }

        if (evento.Status == EventoStatus.Finalizado)
        {
            var possuiVinculoPago = await _context.EventoFuncionarios
                .AnyAsync(ef => ef.EventoId == id && ef.Pago, cancellationToken);

            if (possuiVinculoPago)
            {
                return ApiConflict("Evento finalizado com pagamento confirmado não pode ser alterado.");
            }

            evento.ValorDiaria = request.ValorDiaria;
            evento.ValorHoraExtra = request.ValorHoraExtra;
            evento.DataAlteracao = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var responseFinalizado = await BuscarResponsePorId(evento.Id, cancellationToken);
            return Ok(responseFinalizado);
        }

        evento.CasaId = request.CasaId;
        evento.TipoEventoId = request.TipoEventoId;
        evento.Nome = request.Nome.Trim();
        evento.DataEvento = request.DataEvento.Date;
        evento.HoraInicio = request.HoraInicio;
        evento.HoraFim = request.HoraFim;
        evento.ValorDiaria = request.ValorDiaria;
        evento.ValorHoraExtra = request.ValorHoraExtra;
        evento.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = await BuscarResponsePorId(evento.Id, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancelar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var evento = await _context.Eventos
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evento is null)
        {
            return ApiNotFound("Evento não encontrado.");
        }

        if (evento.Status == EventoStatus.Cancelado)
        {
            return NoContent();
        }

        if (evento.Status == EventoStatus.Finalizado)
        {
            return ApiConflict("Evento finalizado não pode ser cancelado.");
        }

        evento.Status = EventoStatus.Cancelado;
        evento.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<EventoResponse> BuscarResponsePorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.Eventos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventoResponse
            {
                Id = e.Id,
                CasaId = e.CasaId,
                CasaNome = e.Casa.Nome,
                TipoEventoId = e.TipoEventoId,
                TipoEventoNome = e.TipoEvento.Nome,
                Nome = e.Nome,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                ValorDiaria = e.ValorDiaria,
                ValorHoraExtra = e.ValorHoraExtra,
                Status = e.Status.ToString(),
                QuantidadeFuncionarios = e.Funcionarios.Count(f => !f.Removido),
                DataCriacao = e.DataCriacao,
                DataAlteracao = e.DataAlteracao
            })
            .FirstAsync(cancellationToken);
    }

    private static string? ValidarRequest(EventoRequest request)
    {
        if (request.CasaId == Guid.Empty)
        {
            return "A casa do evento é obrigatória.";
        }

        if (request.TipoEventoId == Guid.Empty)
        {
            return "O tipo de evento é obrigatório.";
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "O nome do evento é obrigatório.";
        }

        if (request.Nome.Trim().Length > 200)
        {
            return "O nome do evento deve ter no máximo 200 caracteres.";
        }

        if (request.DataEvento == default)
        {
            return "A data do evento é obrigatória.";
        }

        var hojeOperacional = ObterHojeOperacional();

        if (request.DataEvento.Date < hojeOperacional)
        {
            return $"A data do evento não pode ser anterior a hoje ({hojeOperacional:dd/MM/yyyy}).";
        }

        if (request.ValorDiaria <= 0)
        {
            return "O valor da diária deve ser maior que zero.";
        }

        if (request.ValorHoraExtra < 0)
        {
            return "O valor da hora extra não pode ser negativo.";
        }

        return null;
    }

    private static DateTime ObterHojeOperacional()
    {
        var agoraUtc = DateTime.UtcNow;

        foreach (var timeZoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, timeZone).Date;
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return agoraUtc.AddHours(-3).Date;
    }
}
