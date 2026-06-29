using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Relatorios.Services;

public class PagamentosPdfService
{
    private readonly TagDbContext _context;
    private static readonly CultureInfo PtBr = new("pt-BR");

    public PagamentosPdfService(TagDbContext context)
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
            select new LinhaPagamentoDetalhePdf
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
            var inicioUtc = ConverterDataOperacionalParaUtc(dataInicio.Value.Date);
            query = query.Where(x => x.DataPagamento >= inicioUtc);
        }

        if (dataFim.HasValue)
        {
            var fimUtcExclusivo = ConverterDataOperacionalParaUtc(dataFim.Value.Date.AddDays(1));
            query = query.Where(x => x.DataPagamento < fimUtcExclusivo);
        }

        var detalhes = await query
            .OrderByDescending(x => x.DataPagamento)
            .ThenBy(x => x.FuncionarioNome)
            .ThenBy(x => x.DataEvento)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(buscaNormalizada))
        {
            var pagamentosEncontrados = detalhes
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

            detalhes = detalhes
                .Where(x => pagamentosEncontrados.Contains(x.PagamentoId))
                .ToList();
        }

        var resumos = detalhes
            .GroupBy(x => x.PagamentoId)
            .Select(g =>
            {
                var primeiro = g.First();

                return new LinhaPagamentoResumoPdf
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

        var filtros = MontarDescricaoFiltros(busca, dataInicio, dataFim);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container => AplicarCabecalho(container, "RELATÓRIO DE PAGAMENTOS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));
                    column.Item().PaddingTop(10).Table(table => AdicionarTabelaResumo(table, resumos));
                });

                AplicarRodape(page);
            });

            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container => AplicarCabecalho(container, "DETALHAMENTO DOS PAGAMENTOS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));
                    column.Item().PaddingTop(10).Table(table => AdicionarTabelaDetalhes(table, detalhes));
                });

                AplicarRodape(page);
            });
        }).GeneratePdf();
    }

    private static void AdicionarTabelaResumo(TableDescriptor table, IReadOnlyCollection<LinhaPagamentoResumoPdf> resumos)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1f);
            columns.RelativeColumn(2f);
            columns.RelativeColumn(1f);
            columns.RelativeColumn(1f);
            columns.RelativeColumn(1.8f);
            columns.RelativeColumn(0.8f);
            columns.RelativeColumn(1f);
            columns.RelativeColumn(1f);
        });

        table.Header(header =>
        {
            foreach (var titulo in new[]
            {
                "Data Pagamento", "Funcionário", "CPF", "RG", "Chave Pix",
                "Qtd. Eventos", "Total HE", "Valor Total Pago"
            })
            {
                header.Cell().Element(CellHeader).Text(titulo);
            }
        });

        if (resumos.Count == 0)
        {
            table.Cell().ColumnSpan(8).Element(CellBody).Text("Nenhum pagamento encontrado.");
            return;
        }

        foreach (var linha in resumos)
        {
            table.Cell().Element(CellBody).Text(linha.DataPagamento.ToString("dd/MM/yyyy HH:mm", PtBr));
            table.Cell().Element(CellBody).Text(linha.FuncionarioNome);
            table.Cell().Element(CellBody).Text(linha.Cpf);
            table.Cell().Element(CellBody).Text(linha.Rg);
            table.Cell().Element(CellBody).Text(linha.ChavePix ?? string.Empty);
            table.Cell().Element(CellBody).AlignRight().Text(linha.QuantidadeEventos.ToString(PtBr));
            table.Cell().Element(CellBody).AlignRight().Text(linha.TotalHorasExtras.ToString("N2", PtBr));
            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(linha.ValorTotal));
        }
    }

    private static void AdicionarTabelaDetalhes(TableDescriptor table, IReadOnlyCollection<LinhaPagamentoDetalhePdf> detalhes)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1f);
            columns.RelativeColumn(1.6f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(1.4f);
            columns.RelativeColumn(1.7f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(1f);
            columns.RelativeColumn(0.9f);
            columns.RelativeColumn(1f);
        });

        table.Header(header =>
        {
            foreach (var titulo in new[]
            {
                "Data Pagamento", "Funcionário", "CPF", "RG", "Data Evento",
                "Casa", "Evento", "Valor Diária", "Valor Hora Extra", "Qtd. HE", "Total do Evento"
            })
            {
                header.Cell().Element(CellHeader).Text(titulo);
            }
        });

        if (detalhes.Count == 0)
        {
            table.Cell().ColumnSpan(11).Element(CellBody).Text("Nenhum item de pagamento encontrado.");
            return;
        }

        foreach (var linha in detalhes)
        {
            table.Cell().Element(CellBody).Text(linha.DataPagamento.ToString("dd/MM/yyyy HH:mm", PtBr));
            table.Cell().Element(CellBody).Text(linha.FuncionarioNome);
            table.Cell().Element(CellBody).Text(linha.Cpf);
            table.Cell().Element(CellBody).Text(linha.Rg);
            table.Cell().Element(CellBody).Text(linha.DataEvento.ToString("dd/MM/yyyy", PtBr));
            table.Cell().Element(CellBody).Text(linha.CasaNome);
            table.Cell().Element(CellBody).Text(linha.EventoNome);
            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(linha.ValorDiariaPago));
            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(linha.ValorHoraExtraPago));
            table.Cell().Element(CellBody).AlignRight().Text(linha.QuantidadeHorasExtras.ToString("N2", PtBr));
            table.Cell().Element(CellBody).AlignRight().Text(FormatMoney(linha.ValorTotalItem));
        }
    }

    private static void AplicarCabecalho(IContainer container, string subtitulo)
    {
        container.Column(column =>
        {
            column.Item()
                .Background("#1F2937")
                .PaddingVertical(6)
                .AlignCenter()
                .Text("SOLUCAO FACILITIES")
                .Bold()
                .FontSize(16)
                .FontColor(Colors.White);

            column.Item()
                .Background("#E5E7EB")
                .PaddingVertical(4)
                .AlignCenter()
                .Text(subtitulo)
                .Bold()
                .FontSize(11)
                .FontColor("#111827");
        });
    }

    private static void AplicarLinhaFiltros(IContainer container, string filtros)
    {
        container
            .Border(0.5f)
            .BorderColor("#D1D5DB")
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(55);
                    columns.RelativeColumn();
                });

                table.Cell()
                    .Background("#F3F4F6")
                    .BorderRight(0.5f)
                    .BorderColor("#D1D5DB")
                    .Padding(4)
                    .Text("Filtros:")
                    .Bold();

                table.Cell()
                    .Padding(4)
                    .Text(filtros);
            });
    }

    private static IContainer CellHeader(IContainer container)
    {
        return container
            .Background("#374151")
            .Border(0.5f)
            .BorderColor("#9CA3AF")
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle()
            .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(7));
    }

    private static IContainer CellBody(IContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor("#D1D5DB")
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle();
    }

    private static void AplicarRodape(PageDescriptor page)
    {
        page.Footer()
            .AlignCenter()
            .Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(7).FontColor("#6B7280"));
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
    }

    private static string FormatMoney(decimal valor)
    {
        return valor.ToString("C", PtBr);
    }

    private static string MontarDescricaoFiltros(string? busca, DateTime? dataInicio, DateTime? dataFim)
    {
        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(busca))
            filtros.Add($"Busca: {busca.Trim()}");

        if (dataInicio.HasValue)
            filtros.Add($"Data inicial: {dataInicio.Value:dd/MM/yyyy}");

        if (dataFim.HasValue)
            filtros.Add($"Data final: {dataFim.Value:dd/MM/yyyy}");

        return filtros.Count == 0
            ? "Sem filtros aplicados"
            : string.Join(" | ", filtros);
    }

    private static DateTime ConverterDataOperacionalParaUtc(DateTime dataOperacional)
    {
        var dataLocal = DateTime.SpecifyKind(dataOperacional.Date, DateTimeKind.Unspecified);

        foreach (var timeZoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeToUtc(dataLocal, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return DateTime.SpecifyKind(dataLocal.AddHours(3), DateTimeKind.Utc);
    }

    private sealed class LinhaPagamentoResumoPdf
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

    private sealed class LinhaPagamentoDetalhePdf
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
}
