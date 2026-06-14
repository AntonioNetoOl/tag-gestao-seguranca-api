namespace TagSeguranca.Api.Application.Pagamentos;

public class ConfirmarPagamentoItemRequest
{
    public Guid EventoFuncionarioId { get; set; }
    public decimal QuantidadeHorasExtras { get; set; }
}