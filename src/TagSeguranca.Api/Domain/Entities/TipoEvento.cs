namespace TagSeguranca.Api.Domain.Entities;

public class TipoEvento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nome { get; set; } = string.Empty;

    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }

    public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}