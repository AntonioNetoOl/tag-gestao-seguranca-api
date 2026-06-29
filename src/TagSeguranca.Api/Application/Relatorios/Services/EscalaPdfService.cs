using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Relatorios.Services;

public class EscalaPdfService
{
    private readonly TagDbContext _context;
    private static readonly CultureInfo PtBr = new("pt-BR");

    public EscalaPdfService(TagDbContext context)
    {
        _context = context;
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
            .Where(e => e.Status == EventoStatus.Escalado)
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

        var linhas = new List<LinhaEscalaPdf>();

        foreach (var evento in eventos)
        {
            var funcionarios = evento.Funcionarios
                .OrderBy(f => f.Funcionario.NomeCompleto)
                .ToList();

            if (funcionarios.Count == 0)
            {
                linhas.Add(new LinhaEscalaPdf
                {
                    Data = FormatarPeriodoEvento(evento),
                    Casa = evento.Casa.Nome,
                    Horario = FormatarHorarioEvento(evento),
                    Tipo = evento.TipoEvento.Nome,
                    Evento = evento.Nome,
                    Cooperado = "SEM COOPERADO VINCULADO",
                    Rg = string.Empty,
                    Funcao = string.Empty,
                    Empresa = "Solucao Facilities",
                    Pagamento = FormatMoney(evento.ValorDiaria),
                    HoraExtra = FormatMoney(evento.ValorHoraExtra)
                });

                continue;
            }

            foreach (var vinculo in funcionarios)
            {
                linhas.Add(new LinhaEscalaPdf
                {
                    Data = FormatarPeriodoEvento(evento),
                    Casa = evento.Casa.Nome,
                    Horario = FormatarHorarioEvento(evento),
                    Tipo = evento.TipoEvento.Nome,
                    Evento = evento.Nome,
                    Cooperado = vinculo.Funcionario.NomeCompleto,
                    Rg = vinculo.Funcionario.Rg,
                    Funcao = vinculo.Funcionario.Funcao,
                    Empresa = "Solucao Facilities",
                    Pagamento = FormatMoney(evento.ValorDiaria),
                    HoraExtra = FormatMoney(evento.ValorHoraExtra)
                });
            }
        }

        var casaFiltro = casaId.HasValue
            ? await _context.Casas
                .AsNoTracking()
                .Where(c => c.Id == casaId.Value)
                .Select(c => c.Nome)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var filtros = MontarDescricaoFiltros(casaFiltro, dataInicio, dataFim, nomeEvento);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container => AplicarCabecalho(container, "RELATÓRIO GERAL DE ESCALAS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.9f);
                        });

                        table.Header(header =>
                        {
                            foreach (var titulo in new[]
                            {
                                "Data", "Casa", "Horário", "Tipo", "Evento", "Cooperado",
                                "RG", "Função", "Empresa", "Pagamento", "Hora Extra"
                            })
                            {
                                header.Cell().Element(CellHeader).Text(titulo);
                            }
                        });

                        if (linhas.Count == 0)
                        {
                            table.Cell().ColumnSpan(11).Element(CellBody).Text("Nenhuma escala finalizada encontrada para os filtros informados.");
                        }
                        else
                        {
                            foreach (var linha in linhas)
                            {
                                table.Cell().Element(CellBody).Text(linha.Data);
                                table.Cell().Element(CellBody).Text(linha.Casa);
                                table.Cell().Element(CellBody).Text(linha.Horario);
                                table.Cell().Element(CellBody).Text(linha.Tipo);
                                table.Cell().Element(CellBody).Text(linha.Evento);
                                table.Cell().Element(CellBody).Text(linha.Cooperado);
                                table.Cell().Element(CellBody).Text(linha.Rg);
                                table.Cell().Element(CellBody).Text(linha.Funcao);
                                table.Cell().Element(CellBody).Text(linha.Empresa);
                                table.Cell().Element(CellBody).AlignRight().Text(linha.Pagamento);
                                table.Cell().Element(CellBody).AlignRight().Text(linha.HoraExtra);
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor("#6B7280"));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
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

    private static string FormatMoney(decimal valor)
    {
        return valor.ToString("C", PtBr);
    }

    private static string MontarDescricaoFiltros(string? casa, DateTime? dataInicio, DateTime? dataFim, string? nomeEvento)
    {
        var filtros = new List<string>();

        filtros.Add("Status: Escalado");

        if (!string.IsNullOrWhiteSpace(casa))
            filtros.Add($"Casa: {casa}");

        if (dataInicio.HasValue)
            filtros.Add($"Data inicial: {dataInicio.Value:dd/MM/yyyy}");

        if (dataFim.HasValue)
            filtros.Add($"Data final: {dataFim.Value:dd/MM/yyyy}");

        if (!string.IsNullOrWhiteSpace(nomeEvento))
            filtros.Add($"Evento: {nomeEvento.Trim()}");

        return string.Join(" | ", filtros);
    }

    private static string FormatarPeriodoEvento(Domain.Entities.Evento evento)
    {
        var dataInicio = evento.DataEvento.ToString("dd/MM/yyyy", PtBr);
        return evento.HoraFim < evento.HoraInicio
            ? $"{dataInicio} - {evento.DataEvento.AddDays(1):dd/MM/yyyy}"
            : dataInicio;
    }

    private static string FormatarHorarioEvento(Domain.Entities.Evento evento)
    {
        return $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}";
    }

    private sealed class LinhaEscalaPdf
    {
        public string Data { get; set; } = string.Empty;
        public string Casa { get; set; } = string.Empty;
        public string Horario { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Evento { get; set; } = string.Empty;
        public string Cooperado { get; set; } = string.Empty;
        public string Rg { get; set; } = string.Empty;
        public string Funcao { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Pagamento { get; set; } = string.Empty;
        public string HoraExtra { get; set; } = string.Empty;
    }
}
