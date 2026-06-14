namespace TagSeguranca.Api.Application.Escalas;

public class SubstituirFuncionarioEventoRequest
{
    public Guid FuncionarioAntigoId { get; set; }
    public Guid FuncionarioNovoId { get; set; }
    public string? Motivo { get; set; }
}