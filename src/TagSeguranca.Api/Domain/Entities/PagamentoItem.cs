namespace TagSeguranca.Api.Domain.Entities;

public class PagamentoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PagamentoId { get; set; }
    public Pagamento Pagamento { get; set; } = null!;

    public Guid EventoFuncionarioId { get; set; }
    public EventoFuncionario EventoFuncionario { get; set; } = null!;

    public decimal ValorDiariaPago { get; set; }
    public decimal ValorHoraExtraPago { get; set; }
    public decimal QuantidadeHorasExtras { get; set; }
    public decimal ValorTotalItem { get; set; }
}