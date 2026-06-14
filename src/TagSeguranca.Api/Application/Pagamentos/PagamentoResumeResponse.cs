namespace TagSeguranca.Api.Application.Pagamentos;

public class PagamentoResumoResponse
{
    public Guid Id { get; set; }

    public Guid FuncionarioId { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string MeioPagamento { get; set; } = string.Empty;

    public DateTime DataPagamento { get; set; }

    public decimal ValorTotal { get; set; }
    public decimal TotalHorasExtras { get; set; }
    public int QuantidadeEventos { get; set; }

    public string Status { get; set; } = string.Empty;
}