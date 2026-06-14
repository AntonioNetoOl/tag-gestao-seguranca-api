namespace TagSeguranca.Api.Application.Dashboard;

public class DashboardResumoResponse
{
    public int QuantidadeProximosEventos { get; set; }
    public int QuantidadeEventosHoje { get; set; }
    public int QuantidadeFuncionariosPendentesPagamento { get; set; }

    public List<DashboardProximoEventoResponse> ProximosEventos { get; set; } = new();
}