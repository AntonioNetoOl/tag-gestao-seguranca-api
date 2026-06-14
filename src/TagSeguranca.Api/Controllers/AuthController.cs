using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Application.Auth;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly TagDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        TagDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        JwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { mensagem = "E-mail e senha são obrigatórios." });

        var email = request.Email.Trim().ToLower();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Ativo);

        if (usuario is null)
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var resultadoSenha = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            request.Senha
        );

        if (resultadoSenha == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        var response = _jwtTokenService.GerarToken(usuario);

        return Ok(response);
    }
}