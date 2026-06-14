using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Relatorios.Services;

public class EscalaExcelService
{
    private readonly TagDbContext _context;

    public EscalaExcelService(TagDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]?> GerarEscalaEventoAsync(
    Guid eventoId,
    CancellationToken cancellationToken = default)
    {
        var evento = await _context.Eventos
            .AsNoTracking()
            .Include(e => e.Casa)
            .Include(e => e.TipoEvento)
            .Include(e => e.Funcionarios.Where(ef => !ef.Removido))
                .ThenInclude(ef => ef.Funcionario)
            .FirstOrDefaultAsync(e => e.Id == eventoId, cancellationToken);

        if (evento is null)
        {
            return null;
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Escala");

        worksheet.Cell("A1").Value = "TAG GESTÃO DE SEGURANÇA";
        worksheet.Range("A1:F1").Merge();
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 18;
        worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
        worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;

        worksheet.Cell("A2").Value = "ESCALA DO EVENTO";
        worksheet.Range("A2:F2").Merge();
        worksheet.Cell("A2").Style.Font.Bold = true;
        worksheet.Cell("A2").Style.Font.FontSize = 13;
        worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");

        worksheet.Cell("A4").Value = "Evento:";
        worksheet.Cell("B4").Value = evento.Nome;

        worksheet.Cell("A5").Value = "Casa:";
        worksheet.Cell("B5").Value = evento.Casa.Nome;

        worksheet.Cell("A6").Value = "Tipo:";
        worksheet.Cell("B6").Value = evento.TipoEvento.Nome;

        worksheet.Cell("A7").Value = "Data:";
        worksheet.Cell("B7").Value = evento.DataEvento.ToString("dd/MM/yyyy");

        worksheet.Cell("A8").Value = "Horário:";
        worksheet.Cell("B8").Value = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}";

        worksheet.Cell("A9").Value = "Status:";
        worksheet.Cell("B9").Value = evento.Status.ToString();

        var dadosEventoRange = worksheet.Range("A4:B9");
        dadosEventoRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dadosEventoRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Range("A4:A9").Style.Font.Bold = true;
        worksheet.Range("A4:A9").Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

        worksheet.Cell("A11").Value = "Cooperado";
        worksheet.Cell("B11").Value = "RG";
        worksheet.Cell("C11").Value = "Função";
        worksheet.Cell("D11").Value = "Empresa";
        worksheet.Cell("E11").Value = "Pagamento";
        worksheet.Cell("F11").Value = "Hora Extra";

        AplicarEstiloCabecalhoGeral(worksheet.Range("A11:F11"));

        var linha = 12;

        foreach (var vinculo in evento.Funcionarios.OrderBy(f => f.Funcionario.NomeCompleto))
        {
            worksheet.Cell(linha, 1).Value = vinculo.Funcionario.NomeCompleto;
            worksheet.Cell(linha, 2).Value = vinculo.Funcionario.Rg;
            worksheet.Cell(linha, 3).Value = vinculo.Funcionario.Funcao;
            worksheet.Cell(linha, 4).Value = "TAG";
            worksheet.Cell(linha, 5).Value = evento.ValorDiaria;
            worksheet.Cell(linha, 6).Value = evento.ValorHoraExtra;

            worksheet.Cell(linha, 5).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(linha, 6).Style.NumberFormat.Format = "R$ #,##0.00";

            linha++;
        }

        AplicarBordas(worksheet.Range(11, 1, Math.Max(linha - 1, 11), 6));

        worksheet.SheetView.FreezeRows(11);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    public async Task<byte[]> GerarEscalaGeralAsync(
        Guid? casaId,
        DateTime? dataInicio,
        DateTime? dataFim,
        string? nomeEvento,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Eventos
            .AsNoTracking()
            .Include(e => e.Casa)
            .Include(e => e.TipoEvento)
            .Include(e => e.Funcionarios.Where(ef => !ef.Removido))
                .ThenInclude(ef => ef.Funcionario)
            .Where(e =>
                e.Status != EventoStatus.Finalizado &&
                e.Status != EventoStatus.Cancelado)
            .AsQueryable();

        if (casaId.HasValue)
        {
            query = query.Where(e => e.CasaId == casaId.Value);
        }

        if (dataInicio.HasValue)
        {
            query = query.Where(e => e.DataEvento.Date >= dataInicio.Value.Date);
        }

        if (dataFim.HasValue)
        {
            query = query.Where(e => e.DataEvento.Date <= dataFim.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(nomeEvento))
        {
            var termo = nomeEvento.Trim().ToLower();

            query = query.Where(e => e.Nome.ToLower().Contains(termo));
        }

        var eventos = await query
            .OrderBy(e => e.DataEvento)
            .ThenBy(e => e.HoraInicio)
            .ThenBy(e => e.Casa.Nome)
            .ThenBy(e => e.Nome)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Escalas");

        worksheet.Cell("A1").Value = "TAG GESTÃO DE SEGURANÇA";
        worksheet.Range("A1:K1").Merge();
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 18;
        worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
        worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;

        worksheet.Cell("A2").Value = "RELATÓRIO GERAL DE ESCALAS";
        worksheet.Range("A2:K2").Merge();
        worksheet.Cell("A2").Style.Font.Bold = true;
        worksheet.Cell("A2").Style.Font.FontSize = 13;
        worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");

        worksheet.Cell("A4").Value = "Data";
        worksheet.Cell("B4").Value = "Casa";
        worksheet.Cell("C4").Value = "Horário";
        worksheet.Cell("D4").Value = "Tipo";
        worksheet.Cell("E4").Value = "Evento";
        worksheet.Cell("F4").Value = "Cooperado";
        worksheet.Cell("G4").Value = "RG";
        worksheet.Cell("H4").Value = "Função";
        worksheet.Cell("I4").Value = "Empresa";
        worksheet.Cell("J4").Value = "Pagamento";
        worksheet.Cell("K4").Value = "Hora Extra";

        AplicarEstiloCabecalhoGeral(worksheet.Range("A4:K4"));

        var linha = 5;

        foreach (var evento in eventos)
        {
            var funcionarios = evento.Funcionarios
                .OrderBy(f => f.Funcionario.NomeCompleto)
                .ToList();

            if (funcionarios.Count == 0)
            {
                PreencherLinhaEscalaGeral(worksheet, linha, evento, null);
                linha++;
                continue;
            }

            foreach (var vinculo in funcionarios)
            {
                PreencherLinhaEscalaGeral(worksheet, linha, evento, vinculo);
                linha++;
            }
        }

        AplicarBordas(worksheet.Range(4, 1, Math.Max(linha - 1, 4), 11));

        worksheet.SheetView.FreezeRows(4);
        worksheet.Columns().AdjustToContents();

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void PreencherLinhaEscalaGeral(
    IXLWorksheet worksheet,
    int linha,
    Domain.Entities.Evento evento,
    Domain.Entities.EventoFuncionario? vinculo)
    {
        worksheet.Cell(linha, 1).Value = evento.DataEvento.ToString("dd/MM/yyyy");
        worksheet.Cell(linha, 2).Value = evento.Casa.Nome;
        worksheet.Cell(linha, 3).Value = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}";
        worksheet.Cell(linha, 4).Value = evento.TipoEvento.Nome;
        worksheet.Cell(linha, 5).Value = evento.Nome;
        worksheet.Cell(linha, 6).Value = vinculo?.Funcionario.NomeCompleto ?? string.Empty;
        worksheet.Cell(linha, 7).Value = vinculo?.Funcionario.Rg ?? string.Empty;
        worksheet.Cell(linha, 8).Value = vinculo?.Funcionario.Funcao ?? string.Empty;
        worksheet.Cell(linha, 9).Value = "TAG";
        worksheet.Cell(linha, 10).Value = evento.ValorDiaria;
        worksheet.Cell(linha, 11).Value = evento.ValorHoraExtra;

        worksheet.Cell(linha, 10).Style.NumberFormat.Format = "R$ #,##0.00";
        worksheet.Cell(linha, 11).Style.NumberFormat.Format = "R$ #,##0.00";

        if (vinculo is null)
        {
            var range = worksheet.Range(linha, 1, linha, 11);
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            worksheet.Cell(linha, 6).Value = "SEM COOPERADO VINCULADO";
        }
    }

    private static void AplicarEstiloCabecalho(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void AplicarBordas(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void AplicarEstiloCabecalhoGeral(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#374151");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    

}