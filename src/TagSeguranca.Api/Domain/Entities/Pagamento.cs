using TagSeguranca.Api.Domain.Enums;

namespace TagSeguranca.Api.Domain.Entities;

public class Pagamento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FuncionarioId { get; set; }
    public Funcionario Funcionario { get; set; } = null!;

    public DateTime DataPagamento { get; set; } = DateTime.UtcNow;

    public decimal ValorTotal { get; set; }
    public decimal TotalHorasExtras { get; set; }
    public int QuantidadeEventos { get; set; }

    public PagamentoStatus Status { get; set; } = PagamentoStatus.Confirmado;

    public Guid? UsuarioPagamentoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<PagamentoItem> Itens { get; set; } = new List<PagamentoItem>();
}