namespace TagSeguranca.Api.Domain.Entities;

public class FuncaoFuncionario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public Guid? UsuarioCriacaoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioAlteracaoId { get; set; }
    public DateTime? DataAlteracao { get; set; }

    public ICollection<Funcionario> Funcionarios { get; set; } = new List<Funcionario>();
}
