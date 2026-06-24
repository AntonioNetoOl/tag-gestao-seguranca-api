namespace TagSeguranca.Api.Application.Pagamentos;

public class PagamentoPendenteResumoResponse
{
    public Guid FuncionarioId { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Funcao { get; set; } = string.Empty;
    public string MeioPagamento { get; set; } = string.Empty;

    public int QuantidadeEventos { get; set; }
    public decimal TotalHorasExtras { get; set; }
    public decimal ValorTotalPendente { get; set; }
}
