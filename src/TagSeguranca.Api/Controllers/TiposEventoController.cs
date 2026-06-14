using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.TiposEvento;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Common.Pagination;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/tipos-evento")]
public class TiposEventoController : BaseApiController
{
    private readonly TagDbContext _context;

    public TiposEventoController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoEventoResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.TiposEvento
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            query = query.Where(t =>
                t.Nome.ToLower().Contains(termo));
        }

        var tipos = await query
    .OrderBy(t => t.Nome)
    .Select(t => new TipoEventoResponse
    {
        Id = t.Id,
        Nome = t.Nome,
        DataCriacao = t.DataCriacao,
        DataAlteracao = t.DataAlteracao
    })
    .ToPagedResponseAsync(
        pagination.Page,
        pagination.PageSize,
        cancellationToken);

        return Ok(tipos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TipoEventoResponse>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tipo = await _context.TiposEvento
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TipoEventoResponse
            {
                Id = t.Id,
                Nome = t.Nome,
                DataCriacao = t.DataCriacao,
                DataAlteracao = t.DataAlteracao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tipo is null)
        {
            return ApiNotFound("Tipo de evento não encontrado.");
        }

        return Ok(tipo);
    }

    [HttpPost]
    public async Task<ActionResult<TipoEventoResponse>> Criar(
        [FromBody] TipoEventoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var nomeNormalizado = request.Nome.Trim();

        var jaExiste = await _context.TiposEvento
            .AnyAsync(t => t.Nome.ToLower() == nomeNormalizado.ToLower(), cancellationToken);

        if (jaExiste)
        {
            return ApiConflict("Já existe um tipo de evento com este nome.");
        }

        var tipo = new TipoEvento
        {
            Nome = nomeNormalizado,
            DataCriacao = DateTime.UtcNow
        };

        _context.TiposEvento.Add(tipo);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new TipoEventoResponse
        {
            Id = tipo.Id,
            Nome = tipo.Nome,
            DataCriacao = tipo.DataCriacao,
            DataAlteracao = tipo.DataAlteracao
        };

        return CreatedAtAction(nameof(ObterPorId), new { id = tipo.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TipoEventoResponse>> Atualizar(
        Guid id,
        [FromBody] TipoEventoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var tipo = await _context.TiposEvento
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tipo is null)
        {
            return ApiNotFound("Tipo de evento não encontrado.");
        }

        var nomeNormalizado = request.Nome.Trim();

        var jaExiste = await _context.TiposEvento
            .AnyAsync(t =>
                t.Id != id &&
                t.Nome.ToLower() == nomeNormalizado.ToLower(),
                cancellationToken);

        if (jaExiste)
        {
            return ApiConflict("Já existe outro tipo de evento com este nome.");
        }

        tipo.Nome = nomeNormalizado;
        tipo.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = new TipoEventoResponse
        {
            Id = tipo.Id,
            Nome = tipo.Nome,
            DataCriacao = tipo.DataCriacao,
            DataAlteracao = tipo.DataAlteracao
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tipo = await _context.TiposEvento
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tipo is null)
        {
            return ApiNotFound("Tipo de evento não encontrado.");
        }

        var possuiEventos = await _context.Eventos
            .AnyAsync(e => e.TipoEventoId == id, cancellationToken);

        if (possuiEventos)
        {
            return ApiConflict("Não é possível excluir um tipo de evento que possui eventos vinculados.");
        }

        _context.TiposEvento.Remove(tipo);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidarRequest(TipoEventoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "O nome do tipo de evento é obrigatório.";
        }

        if (request.Nome.Trim().Length > 100)
        {
            return "O nome do tipo de evento deve ter no máximo 100 caracteres.";
        }

        return null;
    }
}