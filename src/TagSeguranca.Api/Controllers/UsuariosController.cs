using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Common.Pagination;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : BaseApiController
{
    private static readonly string[] PerfisPermitidos = ["Master", "Administrador", "Operador"];

    private readonly TagDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(TagDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UsuarioResponse>>> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? perfil,
        [FromQuery] bool? ativo,
        [FromQuery] PagedRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.Usuarios.AsNoTracking().AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(u => u.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(perfil))
        {
            var perfilNormalizado = NormalizarPerfil(perfil);
            query = query.Where(u => u.Perfil == perfilNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(u => u.Nome.ToLower().Contains(termo) || u.Email.ToLower().Contains(termo));
        }

        var usuarios = await query
            .OrderByDescending(u => u.Ativo)
            .ThenBy(u => u.Nome)
            .Select(u => new UsuarioResponse
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Perfil = u.Perfil,
                Ativo = u.Ativo,
                DataCriacao = u.DataCriacao
            })
            .ToPagedResponseAsync(pagination.Page, pagination.PageSize, cancellationToken);

        return Ok(usuarios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioResponse
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Perfil = u.Perfil,
                Ativo = u.Ativo,
                DataCriacao = u.DataCriacao
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            return ApiNotFound("Usuário não encontrado.");
        }

        return Ok(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Criar([FromBody] UsuarioRequest request, CancellationToken cancellationToken)
    {
        var erro = await ValidarRequestAsync(request, exigirSenha: true, idAtual: null, cancellationToken);
        if (erro is not null) return ApiBadRequest(erro);

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim().ToLower(),
            Perfil = NormalizarPerfil(request.Perfil),
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.Senha!);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, MapearResponse(usuario));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioResponse>> Atualizar(Guid id, [FromBody] UsuarioRequest request, CancellationToken cancellationToken)
    {
        var erro = await ValidarRequestAsync(request, exigirSenha: false, idAtual: id, cancellationToken);
        if (erro is not null) return ApiBadRequest(erro);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null) return ApiNotFound("Usuário não encontrado.");

        var novoPerfil = NormalizarPerfil(request.Perfil);

        if (usuario.Perfil == "Master" && novoPerfil != "Master")
        {
            var mestresAtivos = await _context.Usuarios.CountAsync(u => u.Ativo && u.Perfil == "Master", cancellationToken);
            if (mestresAtivos <= 1) return ApiConflict("Não é possível remover o perfil Master do último usuário Master ativo.");
        }

        usuario.Nome = request.Nome.Trim();
        usuario.Email = request.Email.Trim().ToLower();
        usuario.Perfil = novoPerfil;

        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.Senha);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapearResponse(usuario));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null) return ApiNotFound("Usuário não encontrado.");
        if (!usuario.Ativo) return NoContent();

        if (usuario.Perfil == "Master")
        {
            var mestresAtivos = await _context.Usuarios.CountAsync(u => u.Ativo && u.Perfil == "Master", cancellationToken);
            if (mestresAtivos <= 1) return ApiConflict("Não é possível excluir o último usuário Master ativo.");
        }

        usuario.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Restaurar(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null) return ApiNotFound("Usuário não encontrado.");
        if (usuario.Ativo) return NoContent();

        if (await ExisteEmailAtivoAsync(usuario.Email, id, cancellationToken))
        {
            return ApiConflict("Já existe outro usuário ativo cadastrado com este e-mail.");
        }

        usuario.Ativo = true;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string?> ValidarRequestAsync(UsuarioRequest request, bool exigirSenha, Guid? idAtual, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return "O nome do usuário é obrigatório.";
        if (request.Nome.Trim().Length > 150) return "O nome do usuário deve ter no máximo 150 caracteres.";
        if (string.IsNullOrWhiteSpace(request.Email)) return "O e-mail do usuário é obrigatório.";
        if (request.Email.Trim().Length > 150) return "O e-mail do usuário deve ter no máximo 150 caracteres.";
        if (!EmailEhValido(request.Email.Trim())) return "O e-mail informado é inválido.";
        if (!PerfilEhValido(request.Perfil)) return "O perfil informado é inválido.";
        if (exigirSenha && string.IsNullOrWhiteSpace(request.Senha)) return "A senha do usuário é obrigatória.";
        if (!string.IsNullOrWhiteSpace(request.Senha) && request.Senha.Length < 8) return "A senha deve ter pelo menos 8 caracteres.";
        if (!string.IsNullOrWhiteSpace(request.Senha) && request.Senha.Length > 100) return "A senha deve ter no máximo 100 caracteres.";

        if (await ExisteEmailAtivoAsync(request.Email, idAtual, cancellationToken))
        {
            return "Já existe um usuário ativo cadastrado com este e-mail.";
        }

        return null;
    }

    private async Task<bool> ExisteEmailAtivoAsync(string email, Guid? idAtual, CancellationToken cancellationToken)
    {
        var emailNormalizado = email.Trim().ToLower();
        var query = _context.Usuarios.Where(u => u.Ativo && u.Email.ToLower() == emailNormalizado);
        if (idAtual.HasValue) query = query.Where(u => u.Id != idAtual.Value);
        return await query.AnyAsync(cancellationToken);
    }

    private static bool PerfilEhValido(string perfil)
    {
        return PerfisPermitidos.Contains(NormalizarPerfil(perfil));
    }

    private static string NormalizarPerfil(string perfil)
    {
        var normalizado = perfil.Trim();
        return PerfisPermitidos.FirstOrDefault(p => p.Equals(normalizado, StringComparison.OrdinalIgnoreCase)) ?? normalizado;
    }

    private static bool EmailEhValido(string email)
    {
        try { _ = new MailAddress(email); return true; }
        catch { return false; }
    }

    private static UsuarioResponse MapearResponse(Usuario usuario)
    {
        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            Ativo = usuario.Ativo,
            DataCriacao = usuario.DataCriacao
        };
    }
}

public class UsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Operador";
    public string? Senha { get; set; }
}

public class UsuarioResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
