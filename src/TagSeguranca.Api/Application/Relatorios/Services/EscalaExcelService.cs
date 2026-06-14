using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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

        worksheet.Cell("A1").Value = "TAG Gestão de Segurança";
        worksheet.Range("A1:J1").Merge();
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        worksheet.Cell("A3").Value = "Evento:";
        worksheet.Cell("B3").Value = evento.Nome;

        worksheet.Cell("A4").Value = "Casa:";
        worksheet.Cell("B4").Value = evento.Casa.Nome;

        worksheet.Cell("A5").Value = "Tipo:";
        worksheet.Cell("B5").Value = evento.TipoEvento.Nome;

        worksheet.Cell("A6").Value = "Data:";
        worksheet.Cell("B6").Value = evento.DataEvento.ToString("dd/MM/yyyy");

        worksheet.Cell("A7").Value = "Horário:";
        worksheet.Cell("B7").Value = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}";

        worksheet.Cell("A8").Value = "Status:";
        worksheet.Cell("B8").Value = evento.Status.ToString();

        worksheet.Cell("A10").Value = "Data";
        worksheet.Cell("B10").Value = "Casa";
        worksheet.Cell("C10").Value = "Horário";
        worksheet.Cell("D10").Value = "Evento";
        worksheet.Cell("E10").Value = "Nome";
        worksheet.Cell("F10").Value = "RG";
        worksheet.Cell("G10").Value = "Função";
        worksheet.Cell("H10").Value = "Empresa";
        worksheet.Cell("I10").Value = "Pagamento";
        worksheet.Cell("J10").Value = "Hora Extra";

        var headerRange = worksheet.Range("A10:J10");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var linha = 11;

        foreach (var vinculo in evento.Funcionarios.OrderBy(f => f.Funcionario.NomeCompleto))
        {
            worksheet.Cell(linha, 1).Value = evento.DataEvento.ToString("dd/MM/yyyy");
            worksheet.Cell(linha, 2).Value = evento.Casa.Nome;
            worksheet.Cell(linha, 3).Value = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}";
            worksheet.Cell(linha, 4).Value = evento.Nome;
            worksheet.Cell(linha, 5).Value = vinculo.Funcionario.NomeCompleto;
            worksheet.Cell(linha, 6).Value = vinculo.Funcionario.Rg;
            worksheet.Cell(linha, 7).Value = vinculo.Funcionario.Funcao;
            worksheet.Cell(linha, 8).Value = "TAG";
            worksheet.Cell(linha, 9).Value = evento.ValorDiaria;
            worksheet.Cell(linha, 10).Value = evento.ValorHoraExtra;

            worksheet.Cell(linha, 9).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(linha, 10).Style.NumberFormat.Format = "R$ #,##0.00";

            linha++;
        }

        var dataRange = worksheet.Range(10, 1, Math.Max(linha - 1, 10), 10);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}