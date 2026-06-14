namespace TagSeguranca.Api.Application.Funcionarios;

public class FuncionarioRequest
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? ChavePix { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string Funcao { get; set; } = string.Empty;
}