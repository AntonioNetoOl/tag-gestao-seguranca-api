using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Common.Options;
using TagSeguranca.Api.Application.Common.Pagination;
using TagSeguranca.Api.Application.Funcoes;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/funcoes-funcionario")]
public class FuncoesFuncionarioController : BaseApiController
{
    private readonly TagDbContext _context;

    public FuncoesFuncionarioController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<FuncaoFuncionarioResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.FuncoesFuncionario
            .AsNoTracking()
            .AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(f => f.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(f => f.Nome.ToLower().Contains(termo));
        }

        var funcoes = await query
            .OrderByDescending(f => f.Ativo)
            .ThenBy(f => f.Nome)
            .Select(f => new FuncaoFuncionarioResponse
            {
                Id = f.Id,
                Nome = f.Nome,
                Ativo = f.Ativo,
                DataCriacao = f.DataCriacao,
                DataAlteracao = f.DataAlteracao
            })
            .ToPagedResponseAsync(pagination.Page, pagination.PageSize, cancellationToken);

        return Ok(funcoes);
    }

    [HttpGet("opcoes")]
    public async Task<ActionResult<IEnumerable<OptionResponse>>> ListarOpcoes(
        [FromQuery] bool apenasAtivos = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FuncoesFuncionario
            .AsNoTracking()
            .AsQueryable();

        if (apenasAtivos)
        {
            query = query.Where(f => f.Ativo);
        }

        var opcoes = await query
            .OrderBy(f => f.Nome)
            .Select(f => new OptionResponse
            {
                Id = f.Id,
                Nome = f.Nome
            })
            .ToListAsync(cancellationToken);

        return Ok(opcoes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FuncaoFuncionarioResponse>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var funcao = await _context.FuncoesFuncionario
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FuncaoFuncionarioResponse
            {
                Id = f.Id,
                Nome = f.Nome,
                Ativo = f.Ativo,
                DataCriacao = f.DataCriacao,
                DataAlteracao = f.DataAlteracao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (funcao is null)
        {
            return ApiNotFound("Função não encontrada.");
        }

        return Ok(funcao);
    }

    [HttpPost]
    public async Task<ActionResult<FuncaoFuncionarioResponse>> Criar(
        [FromBody] FuncaoFuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var nomeNormalizado = NormalizarNome(request.Nome);

        var nomeJaExiste = await _context.FuncoesFuncionario
            .AnyAsync(f => f.Nome.ToLower() == nomeNormalizado.ToLower(), cancellationToken);

        if (nomeJaExiste)
        {
            return ApiConflict("Já existe uma função cadastrada com este nome.");
        }

        var funcao = new FuncaoFuncionario
        {
            Nome = nomeNormalizado,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        _context.FuncoesFuncionario.Add(funcao);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = funcao.Id }, MapearResponse(funcao));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FuncaoFuncionarioResponse>> Atualizar(
        Guid id,
        [FromBody] FuncaoFuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return ApiBadRequest(erro);
        }

        var funcao = await _context.FuncoesFuncionario
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcao is null)
        {
            return ApiNotFound("Função não encontrada.");
        }

        var nomeNormalizado = NormalizarNome(request.Nome);

        var nomeJaExiste = await _context.FuncoesFuncionario
            .AnyAsync(f => f.Id != id && f.Nome.ToLower() == nomeNormalizado.ToLower(), cancellationToken);

        if (nomeJaExiste)
        {
            return ApiConflict("Já existe outra função cadastrada com este nome.");
        }

        funcao.Nome = nomeNormalizado;
        funcao.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapearResponse(funcao));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var funcao = await _context.FuncoesFuncionario
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcao is null)
        {
            return ApiNotFound("Função não encontrada.");
        }

        if (!funcao.Ativo)
        {
            return NoContent();
        }

        funcao.Ativo = false;
        funcao.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var funcao = await _context.FuncoesFuncionario
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcao is null)
        {
            return ApiNotFound("Função não encontrada.");
        }

        if (funcao.Ativo)
        {
            return NoContent();
        }

        funcao.Ativo = true;
        funcao.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidarRequest(FuncaoFuncionarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "O nome da função é obrigatório.";
        }

        if (request.Nome.Trim().Length > 100)
        {
            return "O nome da função deve ter no máximo 100 caracteres.";
        }

        return null;
    }

    private static string NormalizarNome(string nome)
    {
        return nome.Trim();
    }

    private static FuncaoFuncionarioResponse MapearResponse(FuncaoFuncionario funcao)
    {
        return new FuncaoFuncionarioResponse
        {
            Id = funcao.Id,
            Nome = funcao.Nome,
            Ativo = funcao.Ativo,
            DataCriacao = funcao.DataCriacao,
            DataAlteracao = funcao.DataAlteracao
        };
    }
}
