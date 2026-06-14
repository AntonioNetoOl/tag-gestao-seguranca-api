namespace TagSeguranca.Api.Application.TiposEvento;

public class TipoEventoResponse
{
	public Guid Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public DateTime DataCriacao { get; set; }
	public DateTime? DataAlteracao { get; set; }
}