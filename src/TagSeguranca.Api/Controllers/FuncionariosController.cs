using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Common.Validations;
using TagSeguranca.Api.Application.Funcionarios;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/funcionarios")]
public class FuncionariosController : ControllerBase
{
    private readonly TagDbContext _context;

    public FuncionariosController(TagDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        CancellationToken cancellationToken)
    {
        var query = _context.Funcionarios
            .AsNoTracking()
            .AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(f => f.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            query = query.Where(f =>
                f.NomeCompleto.ToLower().Contains(termo) ||
                f.Rg.ToLower().Contains(termo) ||
                f.Cpf.ToLower().Contains(termo));
        }

        var funcionarios = await query
            .OrderBy(f => f.NomeCompleto)
            .Select(f => new FuncionarioResponse
            {
                Id = f.Id,
                NomeCompleto = f.NomeCompleto,
                Rg = f.Rg,
                Cpf = f.Cpf,
                ChavePix = f.ChavePix,
                Telefone = f.Telefone,
                Email = f.Email,
                Funcao = f.Funcao,
                Ativo = f.Ativo,
                DataCriacao = f.DataCriacao,
                DataAlteracao = f.DataAlteracao
            })
            .ToListAsync(cancellationToken);

        return Ok(funcionarios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FuncionarioResponse>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var funcionario = await _context.Funcionarios
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FuncionarioResponse
            {
                Id = f.Id,
                NomeCompleto = f.NomeCompleto,
                Rg = f.Rg,
                Cpf = f.Cpf,
                ChavePix = f.ChavePix,
                Telefone = f.Telefone,
                Email = f.Email,
                Funcao = f.Funcao,
                Ativo = f.Ativo,
                DataCriacao = f.DataCriacao,
                DataAlteracao = f.DataAlteracao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new
            {
                mensagem = "Funcionário não encontrado."
            });
        }

        return Ok(funcionario);
    }

    [HttpPost]
    public async Task<ActionResult<FuncionarioResponse>> Criar(
        [FromBody] FuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return BadRequest(new
            {
                mensagem = erro
            });
        }

        var cpfNormalizado = CpfValidator.ApenasNumeros(request.Cpf);
        var rgNormalizado = request.Rg.Trim();

        var cpfJaExiste = await _context.Funcionarios
            .AnyAsync(f => f.Cpf == cpfNormalizado, cancellationToken);

        if (cpfJaExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe um funcionário cadastrado com este CPF."
            });
        }

        var rgJaExiste = await _context.Funcionarios
            .AnyAsync(f => f.Rg.ToLower() == rgNormalizado.ToLower(), cancellationToken);

        if (rgJaExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe um funcionário cadastrado com este RG."
            });
        }

        var funcionario = new Funcionario
        {
            NomeCompleto = request.NomeCompleto.Trim(),
            Rg = rgNormalizado,
            Cpf = cpfNormalizado,
            ChavePix = NormalizarOpcional(request.ChavePix),
            Telefone = NormalizarOpcional(request.Telefone),
            Email = NormalizarOpcional(request.Email),
            Funcao = request.Funcao.Trim(),
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        _context.Funcionarios.Add(funcionario);
        await _context.SaveChangesAsync(cancellationToken);

        var response = MapearResponse(funcionario);

        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FuncionarioResponse>> Atualizar(
        Guid id,
        [FromBody] FuncionarioRequest request,
        CancellationToken cancellationToken)
    {
        var erro = ValidarRequest(request);

        if (erro is not null)
        {
            return BadRequest(new
            {
                mensagem = erro
            });
        }

        var funcionario = await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new
            {
                mensagem = "Funcionário não encontrado."
            });
        }

        var cpfNormalizado = CpfValidator.ApenasNumeros(request.Cpf);
        var rgNormalizado = request.Rg.Trim();

        var cpfJaExiste = await _context.Funcionarios
            .AnyAsync(f => f.Id != id && f.Cpf == cpfNormalizado, cancellationToken);

        if (cpfJaExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe outro funcionário cadastrado com este CPF."
            });
        }

        var rgJaExiste = await _context.Funcionarios
            .AnyAsync(f => f.Id != id && f.Rg.ToLower() == rgNormalizado.ToLower(), cancellationToken);

        if (rgJaExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe outro funcionário cadastrado com este RG."
            });
        }

        funcionario.NomeCompleto = request.NomeCompleto.Trim();
        funcionario.Rg = rgNormalizado;
        funcionario.Cpf = cpfNormalizado;
        funcionario.ChavePix = NormalizarOpcional(request.ChavePix);
        funcionario.Telefone = NormalizarOpcional(request.Telefone);
        funcionario.Email = NormalizarOpcional(request.Email);
        funcionario.Funcao = request.Funcao.Trim();
        funcionario.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapearResponse(funcionario));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Inativar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var funcionario = await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new
            {
                mensagem = "Funcionário não encontrado."
            });
        }

        if (!funcionario.Ativo)
        {
            return NoContent();
        }

        funcionario.Ativo = false;
        funcionario.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var funcionario = await _context.Funcionarios
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (funcionario is null)
        {
            return NotFound(new
            {
                mensagem = "Funcionário não encontrado."
            });
        }

        if (funcionario.Ativo)
        {
            return NoContent();
        }

        funcionario.Ativo = true;
        funcionario.DataAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidarRequest(FuncionarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeCompleto))
        {
            return "O nome completo do funcionário é obrigatório.";
        }

        if (request.NomeCompleto.Trim().Length > 200)
        {
            return "O nome completo deve ter no máximo 200 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.Rg))
        {
            return "O RG do funcionário é obrigatório.";
        }

        if (request.Rg.Trim().Length > 30)
        {
            return "O RG deve ter no máximo 30 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.Cpf))
        {
            return "O CPF do funcionário é obrigatório.";
        }

        if (!CpfValidator.EhValido(request.Cpf))
        {
            return "O CPF informado é inválido.";
        }

        if (string.IsNullOrWhiteSpace(request.Funcao))
        {
            return "A função do funcionário é obrigatória.";
        }

        if (request.Funcao.Trim().Length > 100)
        {
            return "A função deve ter no máximo 100 caracteres.";
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (request.Email.Trim().Length > 150)
            {
                return "O e-mail deve ter no máximo 150 caracteres.";
            }

            if (!EmailEhValido(request.Email.Trim()))
            {
                return "O e-mail informado é inválido.";
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ChavePix) && request.ChavePix.Trim().Length > 200)
        {
            return "A chave Pix deve ter no máximo 200 caracteres.";
        }

        if (!string.IsNullOrWhiteSpace(request.Telefone) && request.Telefone.Trim().Length > 30)
        {
            return "O telefone deve ter no máximo 30 caracteres.";
        }

        return null;
    }

    private static bool EmailEhValido(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? NormalizarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static FuncionarioResponse MapearResponse(Funcionario funcionario)
    {
        return new FuncionarioResponse
        {
            Id = funcionario.Id,
            NomeCompleto = funcionario.NomeCompleto,
            Rg = funcionario.Rg,
            Cpf = funcionario.Cpf,
            ChavePix = funcionario.ChavePix,
            Telefone = funcionario.Telefone,
            Email = funcionario.Email,
            Funcao = funcionario.Funcao,
            Ativo = funcionario.Ativo,
            DataCriacao = funcionario.DataCriacao,
            DataAlteracao = funcionario.DataAlteracao
        };
    }
}