namespace TagSeguranca.Api.Application.Pagamentos;

public class ConfirmarPagamentoRequest
{
    public Guid FuncionarioId { get; set; }
    public List<ConfirmarPagamentoItemRequest> Itens { get; set; } = new();
}