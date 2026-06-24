namespace TagSeguranca.Api.Domain.Entities;

public class EventoFuncionarioHistorico
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public Guid? EventoFuncionarioId { get; set; }
    public EventoFuncionario? EventoFuncionario { get; set; }

    public Guid? FuncionarioAnteriorId { get; set; }
    public Funcionario? FuncionarioAnterior { get; set; }

    public Guid? FuncionarioNovoId { get; set; }
    public Funcionario? FuncionarioNovo { get; set; }

    public string Acao { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string? Observacao { get; set; }

    public Guid? UsuarioAcaoId { get; set; }
    public Usuario? UsuarioAcao { get; set; }

    public DateTime DataAcao { get; set; } = DateTime.UtcNow;
}
