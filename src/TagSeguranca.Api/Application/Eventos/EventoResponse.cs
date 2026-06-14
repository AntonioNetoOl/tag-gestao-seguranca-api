namespace TagSeguranca.Api.Application.Eventos;

public class EventoResponse
{
    public Guid Id { get; set; }

    public Guid CasaId { get; set; }
    public string CasaNome { get; set; } = string.Empty;

    public Guid TipoEventoId { get; set; }
    public string TipoEventoNome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public DateTime DataEvento { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }

    public decimal ValorDiaria { get; set; }
    public decimal ValorHoraExtra { get; set; }

    public string Status { get; set; } = string.Empty;

    public int QuantidadeFuncionarios { get; set; }

    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}