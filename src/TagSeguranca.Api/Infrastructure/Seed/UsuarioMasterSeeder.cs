using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Infrastructure.Seed;

public static class UsuarioMasterSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var context = serviceProvider.GetRequiredService<TagDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var nome = configuration["SeedMaster:Nome"];
        var email = configuration["SeedMaster:Email"];
        var senha = configuration["SeedMaster:Senha"];

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            return;
        }

        email = email.Trim().ToLower();

        var usuarioExistente = await context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == email);

        if (usuarioExistente)
            return;

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = "Master",
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        usuario.SenhaHash = passwordHasher.HashPassword(usuario, senha);

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }
}