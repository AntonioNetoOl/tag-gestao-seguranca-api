namespace TagSeguranca.Api.Application.Eventos;

public class EventoRequest
{
    public Guid CasaId { get; set; }
    public Guid TipoEventoId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public DateTime DataEvento { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }

    public decimal ValorDiaria { get; set; }
    public decimal ValorHoraExtra { get; set; }
}