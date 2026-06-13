namespace TagSeguranca.Api.Domain.Entities;

public class Casa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? Cep { get; set; }

    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }

    public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}