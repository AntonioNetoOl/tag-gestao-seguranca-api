using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.TiposEvento;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Common.Pagination;
using TagSeguranca.Api.Application.Common.Options;

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
        [FromQuery] bool? ativo,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.TiposEvento
            .AsNoTracking()
            .AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(t => t.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(t => t.Nome.ToLower().Contains(termo));
        }

        var tipos = await query
            .OrderByDescending(t => t.Ativo)
            .ThenBy(t => t.Nome)
            .Select(t => new TipoEventoResponse
            {
                Id = t.Id,
                Nome = t.Nome,
                Ativo = t.Ativo,
                DataCriacao = t.DataCriacao,
                DataAlteracao = t.DataAlteracao
            })
            .ToPagedResponseAsync(pagination.Page, pagination.PageSize, cancellationToken);

        return Ok(tipos);
    }

    [HttpGet("opcoes")]
    public async Task<ActionResult<IEnumerable<OptionResponse>>> ListarOpcoes(
        [FromQuery] bool apenasAtivos = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TiposEvento
            .AsNoTracking()
            .AsQueryable();

        if (apenasAtivos)
        {
            query = query.Where(t => t.Ativo);
        }

        var opcoes = await query
            .OrderBy(t => t.Nome)
            .Select(t => new OptionResponse
            {
                Id = t.Id,
                Nome = t.Nome
            })
            .ToListAsync(cancellationToken);

        return Ok(opcoes);
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
                Ativo = t.Ativo,
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
        if (erro is not null) return ApiBadRequest(erro);

        var nomeNormalizado = request.Nome.Trim();

        if (await ExisteNomeAtivoAsync(nomeNormalizado, null, cancellationToken))
        {
            return ApiConflict("Já existe um tipo de evento ativo com este nome.");
        }

        var tipo = new TipoEvento
        {
            Nome = nomeNormalizado,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        _context.TiposEvento.Add(tipo);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = tipo.Id }, MapearResponse(tipo));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TipoEventoResponse>> Atualizar(
        Guid id,
        [FromBody] TipoEventoRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);
        if (erro is not null) return ApiBadRequest(erro);

        var tipo = await _context.TiposEvento
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tipo is null)
        {
            return ApiNotFound("Tipo de evento não encontrado.");
        }

        var nomeNormalizado = request.Nome.Trim();

        if (await ExisteNomeAtivoAsync(nomeNormalizado, id, cancellationToken))
        {
            return ApiConflict("Já existe outro tipo de evento ativo com este nome.");
        }

        tipo.Nome = nomeNormalizado;
        tipo.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapearResponse(tipo));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var tipo = await _context.TiposEvento.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tipo is null) return ApiNotFound("Tipo de evento não encontrado.");
        if (!tipo.Ativo) return NoContent();

        tipo.Ativo = false;
        tipo.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var tipo = await _context.TiposEvento.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tipo is null) return ApiNotFound("Tipo de evento não encontrado.");
        if (tipo.Ativo) return NoContent();

        if (await ExisteNomeAtivoAsync(tipo.Nome, id, cancellationToken))
        {
            return ApiConflict("Já existe outro tipo de evento ativo com este nome.");
        }

        tipo.Ativo = true;
        tipo.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> ExisteNomeAtivoAsync(string nome, Guid? idAtual, CancellationToken cancellationToken)
    {
        var nomeNormalizado = nome.Trim().ToLower();
        var query = _context.TiposEvento.Where(t => t.Ativo && t.Nome.ToLower() == nomeNormalizado);
        if (idAtual.HasValue) query = query.Where(t => t.Id != idAtual.Value);
        return await query.AnyAsync(cancellationToken);
    }

    private static string? ValidarRequest(TipoEventoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return "O nome do tipo de evento é obrigatório.";
        if (request.Nome.Trim().Length > 100) return "O nome do tipo de evento deve ter no máximo 100 caracteres.";
        return null;
    }

    private static TipoEventoResponse MapearResponse(TipoEvento tipo)
    {
        return new TipoEventoResponse
        {
            Id = tipo.Id,
            Nome = tipo.Nome,
            Ativo = tipo.Ativo,
            DataCriacao = tipo.DataCriacao,
            DataAlteracao = tipo.DataAlteracao
        };
    }
}
