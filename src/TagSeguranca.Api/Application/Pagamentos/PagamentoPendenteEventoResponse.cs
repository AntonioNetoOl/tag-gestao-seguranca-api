namespace TagSeguranca.Api.Application.Pagamentos;

public class PagamentoPendenteEventoResponse
{
    public Guid EventoFuncionarioId { get; set; }
    public Guid EventoId { get; set; }

    public string NomeEvento { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public string CasaNome { get; set; } = string.Empty;

    public decimal ValorDiaria { get; set; }
    public decimal ValorHoraExtra { get; set; }

    public decimal QuantidadeHorasExtras { get; set; }
    public decimal ValorTotal { get; set; }
}