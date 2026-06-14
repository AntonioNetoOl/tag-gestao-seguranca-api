namespace TagSeguranca.Api.Application.Pagamentos;

public class PagamentoConfirmadoItemResponse
{
    public Guid Id { get; set; }
    public Guid EventoFuncionarioId { get; set; }
    public Guid EventoId { get; set; }

    public string NomeEvento { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public string CasaNome { get; set; } = string.Empty;

    public decimal ValorDiariaPago { get; set; }
    public decimal ValorHoraExtraPago { get; set; }
    public decimal QuantidadeHorasExtras { get; set; }
    public decimal ValorTotalItem { get; set; }
}