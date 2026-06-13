using TagSeguranca.Api.Domain.Enums;

namespace TagSeguranca.Api.Domain.Entities;

public class Evento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CasaId { get; set; }
    public Casa Casa { get; set; } = null!;

    public Guid TipoEventoId { get; set; }
    public TipoEvento TipoEvento { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public DateTime DataEvento { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }

    public decimal ValorDiaria { get; set; }
    public decimal ValorHoraExtra { get; set; }

    public EventoStatus Status { get; set; } = EventoStatus.Rascunho;

    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }

    public ICollection<EventoFuncionario> Funcionarios { get; set; } = new List<EventoFuncionario>();
}