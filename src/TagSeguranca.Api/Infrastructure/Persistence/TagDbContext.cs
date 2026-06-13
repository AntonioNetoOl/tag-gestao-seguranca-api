using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Domain.Entities;
using TagSeguranca.Api.Domain.Enums;

namespace TagSeguranca.Api.Infrastructure.Persistence;

public class TagDbContext : DbContext
{
    public TagDbContext(DbContextOptions<TagDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Casa> Casas => Set<Casa>();
    public DbSet<TipoEvento> TiposEvento => Set<TipoEvento>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<EventoFuncionario> EventoFuncionarios => Set<EventoFuncionario>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<PagamentoItem> PagamentoItens => Set<PagamentoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Perfil).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Ativo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Funcionario>(entity =>
        {
            entity.ToTable("funcionarios");

            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Cpf).IsUnique();
            entity.HasIndex(x => x.Rg).IsUnique();

            entity.Property(x => x.NomeCompleto).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Rg).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Cpf).HasMaxLength(11).IsRequired();
            entity.Property(x => x.ChavePix).HasMaxLength(200);
            entity.Property(x => x.Telefone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(150);
            entity.Property(x => x.Funcao).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Ativo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Casa>(entity =>
        {
            entity.ToTable("casas");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Endereco).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Cep).HasMaxLength(20);
        });

        modelBuilder.Entity<TipoEvento>(entity =>
        {
            entity.ToTable("tipos_evento");

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.ToTable("eventos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome).HasMaxLength(200).IsRequired();

            entity.Property(x => x.DataEvento).HasColumnType("date");
            entity.Property(x => x.HoraInicio).HasColumnType("time");
            entity.Property(x => x.HoraFim).HasColumnType("time");

            entity.Property(x => x.ValorDiaria).HasPrecision(12, 2);
            entity.Property(x => x.ValorHoraExtra).HasPrecision(12, 2);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(EventoStatus.Rascunho);

            entity.HasOne(x => x.Casa)
                .WithMany(x => x.Eventos)
                .HasForeignKey(x => x.CasaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TipoEvento)
                .WithMany(x => x.Eventos)
                .HasForeignKey(x => x.TipoEventoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventoFuncionario>(entity =>
        {
            entity.ToTable("evento_funcionarios");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.EventoId, x.FuncionarioId }).IsUnique();

            entity.Property(x => x.Pago).HasDefaultValue(false);
            entity.Property(x => x.Removido).HasDefaultValue(false);
            entity.Property(x => x.MotivoRemocao).HasMaxLength(300);

            entity.HasOne(x => x.Evento)
                .WithMany(x => x.Funcionarios)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Funcionario)
                .WithMany(x => x.Eventos)
                .HasForeignKey(x => x.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pagamento>(entity =>
        {
            entity.ToTable("pagamentos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ValorTotal).HasPrecision(12, 2);
            entity.Property(x => x.TotalHorasExtras).HasPrecision(12, 2);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(PagamentoStatus.Confirmado);

            entity.HasOne(x => x.Funcionario)
                .WithMany(x => x.Pagamentos)
                .HasForeignKey(x => x.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PagamentoItem>(entity =>
        {
            entity.ToTable("pagamento_itens");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.EventoFuncionarioId).IsUnique();

            entity.Property(x => x.ValorDiariaPago).HasPrecision(12, 2);
            entity.Property(x => x.ValorHoraExtraPago).HasPrecision(12, 2);
            entity.Property(x => x.QuantidadeHorasExtras).HasPrecision(12, 2);
            entity.Property(x => x.ValorTotalItem).HasPrecision(12, 2);

            entity.HasOne(x => x.Pagamento)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.PagamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EventoFuncionario)
                .WithOne(x => x.PagamentoItem)
                .HasForeignKey<PagamentoItem>(x => x.EventoFuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}