using Microsoft.AspNetCore.Mvc;
using TagSeguranca.Api.Application.Relatorios.Services;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : BaseApiController
{
    private readonly EscalaExcelService _escalaExcelService;
    private readonly EscalaPdfService _escalaPdfService;
    private readonly PagamentosExcelService _pagamentosExcelService;
    private readonly RelatoriosPdfService _relatoriosPdfService;

    public RelatoriosController(
        EscalaExcelService escalaExcelService,
        TagDbContext context,
        PagamentosExcelService pagamentosExcelService,
        RelatoriosPdfService relatoriosPdfService)
    {
        _escalaExcelService = escalaExcelService;
        _escalaPdfService = new EscalaPdfService(context);
        _pagamentosExcelService = pagamentosExcelService;
        _relatoriosPdfService = relatoriosPdfService;
    }

    [HttpGet("escalas/excel")]
    public async Task<IActionResult> ExportarEscalasExcel(
        [FromQuery] Guid? casaId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? nomeEvento,
        CancellationToken cancellationToken)
    {
        var erroPeriodo = ValidarPeriodoEscala(dataInicio, dataFim);
        if (erroPeriodo is not null)
        {
            return ApiBadRequest(erroPeriodo);
        }

        var arquivo = await _escalaExcelService.GerarEscalaGeralAsync(
            casaId,
            dataInicio,
            dataFim,
            nomeEvento,
            cancellationToken);

        var nomeArquivo = $"relatorio-geral-escalas-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomeArquivo);
    }

    [HttpGet("pagamentos/excel")]
    public async Task<IActionResult> ExportarPagamentosExcel(
        [FromQuery] string? busca,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim)
    {
        var arquivo = await _pagamentosExcelService.GerarAsync(busca, dataInicio, dataFim);

        var nomeArquivo = $"relatorio-pagamentos-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";

        return File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nomeArquivo
        );
    }

    [HttpGet("escalas/pdf")]
    public async Task<IActionResult> ExportarEscalasPdf(
        [FromQuery] Guid? casaId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? nomeEvento,
        CancellationToken cancellationToken)
    {
        var erroPeriodo = ValidarPeriodoEscala(dataInicio, dataFim);
        if (erroPeriodo is not null)
        {
            return ApiBadRequest(erroPeriodo);
        }

        var arquivo = await _escalaPdfService.GerarEscalaGeralAsync(
            casaId,
            dataInicio,
            dataFim,
            nomeEvento,
            cancellationToken);

        var nomeArquivo = $"relatorio-geral-escalas-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";

        return File(
            arquivo,
            "application/pdf",
            nomeArquivo
        );
    }

    [HttpGet("pagamentos/pdf")]
    public async Task<IActionResult> ExportarPagamentosPdf(
        [FromQuery] string? busca,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        var arquivo = await _relatoriosPdfService.GerarPagamentosAsync(
            busca,
            dataInicio,
            dataFim,
            cancellationToken);

        var nomeArquivo = $"relatorio-pagamentos-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";

        return File(
            arquivo,
            "application/pdf",
            nomeArquivo
        );
    }

    private static string? ValidarPeriodoEscala(DateTime? dataInicio, DateTime? dataFim)
    {
        if (!dataInicio.HasValue || !dataFim.HasValue)
        {
            return "Informe a data inicial e a data final para emitir o relatório de escala.";
        }

        if (dataInicio.Value.Date > dataFim.Value.Date)
        {
            return "A data inicial não pode ser maior que a data final.";
        }

        return null;
    }
}
