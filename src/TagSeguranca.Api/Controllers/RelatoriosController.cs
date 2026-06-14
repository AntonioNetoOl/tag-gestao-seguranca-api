using Microsoft.AspNetCore.Mvc;
using TagSeguranca.Api.Application.Relatorios.Services;

namespace TagSeguranca.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : BaseApiController
{
    private readonly EscalaExcelService _escalaExcelService;

    public RelatoriosController(EscalaExcelService escalaExcelService)
    {
        _escalaExcelService = escalaExcelService;
    }

    [HttpGet("escalas/excel")]
    public async Task<IActionResult> ExportarEscalasExcel(
        [FromQuery] Guid? casaId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? nomeEvento,
        CancellationToken cancellationToken)
    {
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
}