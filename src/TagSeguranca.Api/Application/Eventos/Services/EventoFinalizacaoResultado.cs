namespace TagSeguranca.Api.Application.Eventos.Services;

public class EventoFinalizacaoResultado
{
    public int QuantidadeEventosFinalizados { get; set; }
    public DateTime DataHoraProcessamento { get; set; } = DateTime.Now;
}