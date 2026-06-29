using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Relatorios.Services;

public class PagamentosExcelService
{
    private readonly TagDbContext _context;

    public PagamentosExcelService(TagDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GerarAsync(
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken = default)
    {
        var buscaNormalizada = busca?.Trim().ToLower();

        var query =
            from pagamento in _context.Pagamentos.AsNoTracking()
            join funcionario in _context.Funcionarios.AsNoTracking()
                on pagamento.FuncionarioId equals funcionario.Id
            join item in _context.PagamentoItens.AsNoTracking()
                on pagamento.Id equals item.PagamentoId
            join eventoFuncionario in _context.EventoFuncionarios.AsNoTracking()
                on item.EventoFuncionarioId equals eventoFuncionario.Id
            join evento in _context.Eventos.AsNoTracking()
                on eventoFuncionario.EventoId equals evento.Id
            join casa in _context.Casas.AsNoTracking()
                on evento.CasaId equals casa.Id
            select new LinhaPagamentoRelatorio
            {
                PagamentoId = pagamento.Id,
                DataPagamento = pagamento.DataPagamento,
                QuantidadeEventos = pagamento.QuantidadeEventos,
                TotalHorasExtras = pagamento.TotalHorasExtras,
                ValorTotal = pagamento.ValorTotal,

                FuncionarioNome = funcionario.NomeCompleto,
                Cpf = funcionario.Cpf,
                Rg = funcionario.Rg,
                ChavePix = funcionario.ChavePix,

                EventoNome = evento.Nome,
                DataEvento = evento.DataEvento,
                CasaNome = casa.Nome,

                ValorDiariaPago = item.ValorDiariaPago,
                ValorHoraExtraPago = item.ValorHoraExtraPago,
                QuantidadeHorasExtras = item.QuantidadeHorasExtras,
                ValorTotalItem = item.ValorTotalItem
            };

        if (dataInicio.HasValue)
        {
            var inicio = dataInicio.Value.Date;
            query = query.Where(x => x.DataPagamento >= inicio);
        }

        if (dataFim.HasValue)
        {
            var fimExclusivo = dataFim.Value.Date.AddDays(1);
            query = query.Where(x => x.DataPagamento < fimExclusivo);
        }

        var todasAsLinhas = await query
            .OrderByDescending(x => x.DataPagamento)
            .ThenBy(x => x.FuncionarioNome)
            .ThenBy(x => x.DataEvento)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(buscaNormalizada))
        {
            var pagamentosEncontrados = todasAsLinhas
                .Where(x =>
                    (x.FuncionarioNome ?? string.Empty).ToLower().Contains(buscaNormalizada) ||
                    (x.Cpf ?? string.Empty).ToLower().Contains(buscaNormalizada) ||
                    (x.Rg ?? string.Empty).ToLower().Contains(buscaNormalizada) ||
                    (x.ChavePix ?? string.Empty).ToLower().Contains(buscaNormalizada) ||
                    (x.EventoNome ?? string.Empty).ToLower().Contains(buscaNormalizada) ||
                    (x.CasaNome ?? string.Empty).ToLower().Contains(buscaNormalizada))
                .Select(x => x.PagamentoId)
                .Distinct()
                .ToHashSet();

            todasAsLinhas = todasAsLinhas
                .Where(x => pagamentosEncontrados.Contains(x.PagamentoId))
                .ToList();
        }

        var linhasResumo = todasAsLinhas
            .GroupBy(x => x.PagamentoId)
            .Select(g =>
            {
                var primeiro = g.First();

                return new LinhaResumoPagamentoRelatorio
                {
                    PagamentoId = primeiro.PagamentoId,
                    DataPagamento = primeiro.DataPagamento,
                    FuncionarioNome = primeiro.FuncionarioNome,
                    Cpf = primeiro.Cpf,
                    Rg = primeiro.Rg,
                    ChavePix = primeiro.ChavePix,
                    QuantidadeEventos = primeiro.QuantidadeEventos,
                    TotalHorasExtras = primeiro.TotalHorasExtras,
                    ValorTotal = primeiro.ValorTotal
                };
            })
            .OrderByDescending(x => x.DataPagamento)
            .ThenBy(x => x.FuncionarioNome)
            .ToList();

        using var workbook = new XLWorkbook();

        CriarAbaResumo(workbook, linhasResumo, busca, dataInicio, dataFim);
        CriarAbaDetalhamento(workbook, todasAsLinhas, busca, dataInicio, dataFim);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void CriarAbaResumo(
        XLWorkbook workbook,
        IReadOnlyCollection<LinhaResumoPagamentoRelatorio> linhas,
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim)
    {
        var worksheet = workbook.Worksheets.Add("Resumo");

        const int totalColunas = 8;

        AplicarTituloRelatorio(
            worksheet,
            subtitulo: "RELATÓRIO DE PAGAMENTOS",
            totalColunas: totalColunas);

        AplicarLinhaFiltros(
            worksheet,
            totalColunas,
            busca,
            dataInicio,
            dataFim);

        const int linhaCabecalho = 6;

        worksheet.Cell(linhaCabecalho, 1).Value = "Data Pagamento";
        worksheet.Cell(linhaCabecalho, 2).Value = "Funcionário";
        worksheet.Cell(linhaCabecalho, 3).Value = "CPF";
        worksheet.Cell(linhaCabecalho, 4).Value = "RG";
        worksheet.Cell(linhaCabecalho, 5).Value = "Chave Pix";
        worksheet.Cell(linhaCabecalho, 6).Value = "Qtd. Eventos";
        worksheet.Cell(linhaCabecalho, 7).Value = "Total Horas Extras";
        worksheet.Cell(linhaCabecalho, 8).Value = "Valor Total Pago";

        AplicarEstiloCabecalhoGeral(worksheet.Range(linhaCabecalho, 1, linhaCabecalho, totalColunas));

        var linhaAtual = linhaCabecalho + 1;

        foreach (var linha in linhas)
        {
            worksheet.Cell(linhaAtual, 1).Value = linha.DataPagamento;
            worksheet.Cell(linhaAtual, 2).Value = linha.FuncionarioNome;
            worksheet.Cell(linhaAtual, 3).Value = linha.Cpf;
            worksheet.Cell(linhaAtual, 4).Value = linha.Rg;
            worksheet.Cell(linhaAtual, 5).Value = linha.ChavePix ?? string.Empty;
            worksheet.Cell(linhaAtual, 6).Value = linha.QuantidadeEventos;
            worksheet.Cell(linhaAtual, 7).Value = linha.TotalHorasExtras;
            worksheet.Cell(linhaAtual, 8).Value = linha.ValorTotal;

            worksheet.Cell(linhaAtual, 8).Style.NumberFormat.Format = "R$ #,##0.00";

            linhaAtual++;
        }

        AplicarFormatacaoResumo(worksheet);
        AplicarBordas(worksheet.Range(linhaCabecalho, 1, Math.Max(linhaAtual - 1, linhaCabecalho), totalColunas));

        worksheet.SheetView.FreezeRows(linhaCabecalho);
    }

    private static void CriarAbaDetalhamento(
        XLWorkbook workbook,
        IReadOnlyCollection<LinhaPagamentoRelatorio> linhas,
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim)
    {
        var worksheet = workbook.Worksheets.Add("Detalhamento");

        const int totalColunas = 11;

        AplicarTituloRelatorio(
            worksheet,
            subtitulo: "DETALHAMENTO DOS PAGAMENTOS",
            totalColunas: totalColunas);

        AplicarLinhaFiltros(
            worksheet,
            totalColunas,
            busca,
            dataInicio,
            dataFim);

        const int linhaCabecalho = 6;

        worksheet.Cell(linhaCabecalho, 1).Value = "Data Pagamento";
        worksheet.Cell(linhaCabecalho, 2).Value = "Funcionário";
        worksheet.Cell(linhaCabecalho, 3).Value = "CPF";
        worksheet.Cell(linhaCabecalho, 4).Value = "RG";
        worksheet.Cell(linhaCabecalho, 5).Value = "Data Evento";
        worksheet.Cell(linhaCabecalho, 6).Value = "Casa";
        worksheet.Cell(linhaCabecalho, 7).Value = "Evento";
        worksheet.Cell(linhaCabecalho, 8).Value = "Valor Diária";
        worksheet.Cell(linhaCabecalho, 9).Value = "Valor Hora Extra";
        worksheet.Cell(linhaCabecalho, 10).Value = "Qtd. Horas Extras";
        worksheet.Cell(linhaCabecalho, 11).Value = "Total do Evento";

        AplicarEstiloCabecalhoGeral(worksheet.Range(linhaCabecalho, 1, linhaCabecalho, totalColunas));

        var linhaAtual = linhaCabecalho + 1;

        foreach (var linha in linhas)
        {
            worksheet.Cell(linhaAtual, 1).Value = linha.DataPagamento;
            worksheet.Cell(linhaAtual, 2).Value = linha.FuncionarioNome;
            worksheet.Cell(linhaAtual, 3).Value = linha.Cpf;
            worksheet.Cell(linhaAtual, 4).Value = linha.Rg;
            worksheet.Cell(linhaAtual, 5).Value = linha.DataEvento;
            worksheet.Cell(linhaAtual, 6).Value = linha.CasaNome;
            worksheet.Cell(linhaAtual, 7).Value = linha.EventoNome;
            worksheet.Cell(linhaAtual, 8).Value = linha.ValorDiariaPago;
            worksheet.Cell(linhaAtual, 9).Value = linha.ValorHoraExtraPago;
            worksheet.Cell(linhaAtual, 10).Value = linha.QuantidadeHorasExtras;
            worksheet.Cell(linhaAtual, 11).Value = linha.ValorTotalItem;

            worksheet.Cell(linhaAtual, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(linhaAtual, 9).Style.NumberFormat.Format = "R$ #,##0.00";
            worksheet.Cell(linhaAtual, 11).Style.NumberFormat.Format = "R$ #,##0.00";

            linhaAtual++;
        }

        AplicarFormatacaoDetalhamento(worksheet);
        AplicarBordas(worksheet.Range(linhaCabecalho, 1, Math.Max(linhaAtual - 1, linhaCabecalho), totalColunas));

        worksheet.SheetView.FreezeRows(linhaCabecalho);
    }

    private static void AplicarTituloRelatorio(
        IXLWorksheet worksheet,
        string subtitulo,
        int totalColunas)
    {
        worksheet.Cell(1, 1).Value = "SOLUCAO FACILITIES";
        worksheet.Range(1, 1, 1, totalColunas).Merge();

        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 18;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

        worksheet.Cell(2, 1).Value = subtitulo;
        worksheet.Range(2, 1, 2, totalColunas).Merge();

        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 13;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
    }

    private static void AplicarLinhaFiltros(
        IXLWorksheet worksheet,
        int totalColunas,
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim)
    {
        worksheet.Cell(4, 1).Value = "Filtros:";
        worksheet.Cell(4, 2).Value = MontarDescricaoFiltros(busca, dataInicio, dataFim);

        if (totalColunas > 2)
        {
            worksheet.Range(4, 2, 4, totalColunas).Merge();
        }

        var filtrosRange = worksheet.Range(4, 1, 4, totalColunas);

        filtrosRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        filtrosRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
    }

    private static void AplicarEstiloCabecalhoGeral(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#374151");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void AplicarBordas(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void AplicarFormatacaoResumo(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        worksheet.Column(3).Style.NumberFormat.Format = "@";
        worksheet.Column(4).Style.NumberFormat.Format = "@";
        worksheet.Column(8).Style.NumberFormat.Format = "R$ #,##0.00";

        worksheet.Column(1).Width = 18;
        worksheet.Column(2).Width = 32;
        worksheet.Column(3).Width = 15;
        worksheet.Column(4).Width = 14;
        worksheet.Column(5).Width = 30;
        worksheet.Column(6).Width = 14;
        worksheet.Column(7).Width = 20;
        worksheet.Column(8).Width = 18;

        worksheet.Columns(1, 8).AdjustToContents();
    }

    private static void AplicarFormatacaoDetalhamento(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        worksheet.Column(3).Style.NumberFormat.Format = "@";
        worksheet.Column(4).Style.NumberFormat.Format = "@";
        worksheet.Column(5).Style.DateFormat.Format = "dd/MM/yyyy";
        worksheet.Column(8).Style.NumberFormat.Format = "R$ #,##0.00";
        worksheet.Column(9).Style.NumberFormat.Format = "R$ #,##0.00";
        worksheet.Column(11).Style.NumberFormat.Format = "R$ #,##0.00";

        worksheet.Column(1).Width = 18;
        worksheet.Column(2).Width = 32;
        worksheet.Column(3).Width = 15;
        worksheet.Column(4).Width = 14;
        worksheet.Column(5).Width = 14;
        worksheet.Column(6).Width = 28;
        worksheet.Column(7).Width = 36;
        worksheet.Column(8).Width = 16;
        worksheet.Column(9).Width = 18;
        worksheet.Column(10).Width = 18;
        worksheet.Column(11).Width = 18;

        worksheet.Columns(1, 11).AdjustToContents();
    }

    private static string MontarDescricaoFiltros(
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim)
    {
        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            filtros.Add($"Busca: {busca.Trim()}");
        }

        if (dataInicio.HasValue)
        {
            filtros.Add($"Data inicial: {dataInicio.Value:dd/MM/yyyy}");
        }

        if (dataFim.HasValue)
        {
            filtros.Add($"Data final: {dataFim.Value:dd/MM/yyyy}");
        }

        return filtros.Count == 0
            ? "Sem filtros aplicados"
            : string.Join(" | ", filtros);
    }

    private sealed class LinhaPagamentoRelatorio
    {
        public Guid PagamentoId { get; set; }
        public DateTime DataPagamento { get; set; }
        public int QuantidadeEventos { get; set; }
        public decimal TotalHorasExtras { get; set; }
        public decimal ValorTotal { get; set; }

        public string FuncionarioNome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Rg { get; set; } = string.Empty;
        public string? ChavePix { get; set; }

        public string EventoNome { get; set; } = string.Empty;
        public DateTime DataEvento { get; set; }
        public string CasaNome { get; set; } = string.Empty;

        public decimal ValorDiariaPago { get; set; }
        public decimal ValorHoraExtraPago { get; set; }
        public decimal QuantidadeHorasExtras { get; set; }
        public decimal ValorTotalItem { get; set; }
    }

    private sealed class LinhaResumoPagamentoRelatorio
    {
        public Guid PagamentoId { get; set; }
        public DateTime DataPagamento { get; set; }
        public string FuncionarioNome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Rg { get; set; } = string.Empty;
        public string? ChavePix { get; set; }
        public int QuantidadeEventos { get; set; }
        public decimal TotalHorasExtras { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
