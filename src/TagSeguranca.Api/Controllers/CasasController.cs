using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Casas;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;
using TagSeguranca.Api.Application.Common.Pagination;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/casas")]
public class CasasController : BaseApiController
{
    private readonly TagDbContext _context;

    public CasasController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CasaResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.Casas
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            query = query.Where(c =>
                c.Nome.ToLower().Contains(termo) ||
                c.Endereco.ToLower().Contains(termo));
        }

        var casas = await query
    .OrderBy(c => c.Nome)
    .Select(c => new CasaResponse
    {
        Id = c.Id,
        Nome = c.Nome,
        Endereco = c.Endereco,
        Cep = c.Cep,
        DataCriacao = c.DataCriacao,
        DataAlteracao = c.DataAlteracao
    })
    .ToPagedResponseAsync(
        pagination.Page,
        pagination.PageSize,
        cancellationToken);

        return Ok(casas);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CasaResponse>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var casa = await _context.Casas
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CasaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Endereco = c.Endereco,
                Cep = c.Cep,
                DataCriacao = c.DataCriacao,
                DataAlteracao = c.DataAlteracao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (casa is null)
        {
            return ApiNotFound("Casa não encontrada.");
        }

        return Ok(casa);
    }

    [HttpPost]
    public async Task<ActionResult<CasaResponse>> Criar(
        [FromBody] CasaRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);   
        }

        var casa = new Casa
        {
            Nome = request.Nome.Trim(),
            Endereco = request.Endereco.Trim(),
            Cep = string.IsNullOrWhiteSpace(request.Cep) ? null : request.Cep.Trim(),
            DataCriacao = DateTime.UtcNow
        };

        _context.Casas.Add(casa);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new CasaResponse
        {
            Id = casa.Id,
            Nome = casa.Nome,
            Endereco = casa.Endereco,
            Cep = casa.Cep,
            DataCriacao = casa.DataCriacao,
            DataAlteracao = casa.DataAlteracao
        };

        return CreatedAtAction(nameof(ObterPorId), new { id = casa.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CasaResponse>> Atualizar(
        Guid id,
        [FromBody] CasaRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var casa = await _context.Casas
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (casa is null)
        {
            return ApiNotFound("Casa não encontrada.");
        }

        casa.Nome = request.Nome.Trim();
        casa.Endereco = request.Endereco.Trim();
        casa.Cep = string.IsNullOrWhiteSpace(request.Cep) ? null : request.Cep.Trim();
        casa.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CasaResponse
        {
            Id = casa.Id,
            Nome = casa.Nome,
            Endereco = casa.Endereco,
            Cep = casa.Cep,
            DataCriacao = casa.DataCriacao,
            DataAlteracao = casa.DataAlteracao
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(
        Guid id,
        CancellationToken cancellationToken)
    {
        var casa = await _context.Casas
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (casa is null)
        {
            return ApiNotFound("Casa não encontrada.");
 
        }

        var possuiEventos = await _context.Eventos
            .AnyAsync(e => e.CasaId == id, cancellationToken);

        if (possuiEventos)
        {
            return ApiConflict("Não é possível excluir uma casa que possui eventos vinculados.");
        }

        _context.Casas.Remove(casa);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidarRequest(CasaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "O nome da casa é obrigatório.";
        }

        if (request.Nome.Trim().Length > 150)
        {
            return "O nome da casa deve ter no máximo 150 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.Endereco))
        {
            return "O endereço da casa é obrigatório.";
        }

        if (request.Endereco.Trim().Length > 300)
        {
            return "O endereço da casa deve ter no máximo 300 caracteres.";
        }

        if (!string.IsNullOrWhiteSpace(request.Cep) && request.Cep.Trim().Length > 20)
        {
            return "O CEP deve ter no máximo 20 caracteres.";
        }

        return null;
    }
}