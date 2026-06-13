namespace TagSeguranca.Api.Domain.Entities;

public class EventoFuncionario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public Guid FuncionarioId { get; set; }
    public Funcionario Funcionario { get; set; } = null!;

    public bool Pago { get; set; } = false;
    public bool Removido { get; set; } = false;
    public string? MotivoRemocao { get; set; }

    public PagamentoItem? PagamentoItem { get; set; }

    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }
}