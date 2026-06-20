namespace TagSeguranca.Api.Application.Funcionarios;

public class FuncionarioResponse
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? ChavePix { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public Guid? FuncaoFuncionarioId { get; set; }
    public string Funcao { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
