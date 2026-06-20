namespace TagSeguranca.Api.Domain.Entities;

public class Funcionario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? ChavePix { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Guid? FuncaoFuncionarioId { get; set; }
    public FuncaoFuncionario? FuncaoFuncionario { get; set; }
    public string Funcao { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }

    public ICollection<EventoFuncionario> Eventos { get; set; } = new List<EventoFuncionario>();
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}
