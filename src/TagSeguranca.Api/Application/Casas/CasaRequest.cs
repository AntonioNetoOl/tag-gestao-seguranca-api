namespace TagSeguranca.Api.Application.Casas;

public class CasaRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? Cep { get; set; }
}