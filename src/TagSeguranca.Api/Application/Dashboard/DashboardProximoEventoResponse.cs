namespace TagSeguranca.Api.Application.Dashboard;

public class DashboardProximoEventoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string CasaNome { get; set; } = string.Empty;
    public string TipoEventoNome { get; set; } = string.Empty;

    public DateTime DataEvento { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }

    public string Status { get; set; } = string.Empty;
    public int QuantidadeFuncionarios { get; set; }
}