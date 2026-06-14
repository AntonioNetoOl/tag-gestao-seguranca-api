namespace TagSeguranca.Api.Application.Escalas;

public class EventoFuncionarioResponse
{
    public Guid Id { get; set; }

    public Guid EventoId { get; set; }
    public Guid FuncionarioId { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Funcao { get; set; } = string.Empty;

    public bool Pago { get; set; }
    public bool Removido { get; set; }
    public string? MotivoRemocao { get; set; }

    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}