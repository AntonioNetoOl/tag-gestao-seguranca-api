using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TagSeguranca.Api.Domain.Enums;
using TagSeguranca.Api.Infrastructure.Persistence;

namespace TagSeguranca.Api.Application.Relatorios.Services;

public class RelatoriosPdfService
{
    private readonly TagDbContext _context;
    private static readonly CultureInfo PtBr = new("pt-BR");

    public RelatoriosPdfService(TagDbContext context)
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

        var funcionarios = evento.Funcionarios
            .OrderBy(f => f.Funcionario.NomeCompleto)
            .Select(f => new LinhaEscalaEventoPdf
            {
                Cooperado = f.Funcionario.NomeCompleto,
                Rg = f.Funcionario.Rg,
                Funcao = f.Funcionario.Funcao,
                Empresa = "TAG",
                Pagamento = FormatMoney(evento.ValorDiaria),
                HoraExtra = FormatMoney(evento.ValorHoraExtra)
            })
            .ToList();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(container =>
                    AplicarCabecalhoRelatorio(container, "ESCALA DO EVENTO"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container =>
                    {
                        container.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn();
                            });

                            AdicionarLinhaInfo(table, "Evento:", evento.Nome);
                            AdicionarLinhaInfo(table, "Casa:", evento.Casa.Nome);
                            AdicionarLinhaInfo(table, "Tipo:", evento.TipoEvento.Nome);
                            AdicionarLinhaInfo(table, "Data:", evento.DataEvento.ToString("dd/MM/yyyy", PtBr));
                            AdicionarLinhaInfo(table, "Horário:", $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}");
                            AdicionarLinhaInfo(table, "Status:", evento.Status.ToString());
                        });
                    });

                    column.Item().PaddingTop(12).Element(container =>
                    {
                        container.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.4f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.1f);
                            });

                            table.Header(header =>
                            {
                                foreach (var titulo in new[] { "Cooperado", "RG", "Função", "Empresa", "Pagamento", "Hora Extra" })
                                {
                                    header.Cell().Element(CellHeader).Text(titulo);
                                }
                            });

                            if (funcionarios.Count == 0)
                            {
                                table.Cell().ColumnSpan(6).Element(CellBody).Text("Nenhum cooperado vinculado.");
                            }
                            else
                            {
                                foreach (var funcionario in funcionarios)
                                {
                                    table.Cell().Element(CellBody).Text(funcionario.Cooperado);
                                    table.Cell().Element(CellBody).Text(funcionario.Rg);
                                    table.Cell().Element(CellBody).Text(funcionario.Funcao);
                                    table.Cell().Element(CellBody).Text(funcionario.Empresa);
                                    table.Cell().Element(CellBody).AlignRight().Text(funcionario.Pagamento);
                                    table.Cell().Element(CellBody).AlignRight().Text(funcionario.HoraExtra);
                                }
                            }
                        });
                    });
                });

                AplicarRodape(page);
            });
        }).GeneratePdf();
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

        var linhas = new List<LinhaEscalaGeralPdf>();

        foreach (var evento in eventos)
        {
            var funcionarios = evento.Funcionarios
                .OrderBy(f => f.Funcionario.NomeCompleto)
                .ToList();

            if (funcionarios.Count == 0)
            {
                linhas.Add(new LinhaEscalaGeralPdf
                {
                    Data = evento.DataEvento.ToString("dd/MM/yyyy", PtBr),
                    Casa = evento.Casa.Nome,
                    Horario = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}",
                    Tipo = evento.TipoEvento.Nome,
                    Evento = evento.Nome,
                    Cooperado = "SEM COOPERADO VINCULADO",
                    Rg = string.Empty,
                    Funcao = string.Empty,
                    Empresa = "TAG",
                    Pagamento = FormatMoney(evento.ValorDiaria),
                    HoraExtra = FormatMoney(evento.ValorHoraExtra)
                });

                continue;
            }

            foreach (var vinculo in funcionarios)
            {
                linhas.Add(new LinhaEscalaGeralPdf
                {
                    Data = evento.DataEvento.ToString("dd/MM/yyyy", PtBr),
                    Casa = evento.Casa.Nome,
                    Horario = $"{evento.HoraInicio:hh\\:mm} às {evento.HoraFim:hh\\:mm}",
                    Tipo = evento.TipoEvento.Nome,
                    Evento = evento.Nome,
                    Cooperado = vinculo.Funcionario.NomeCompleto,
                    Rg = vinculo.Funcionario.Rg,
                    Funcao = vinculo.Funcionario.Funcao,
                    Empresa = "TAG",
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

        var filtros = MontarDescricaoFiltrosEscala(casaFiltro, dataInicio, dataFim, nomeEvento);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container =>
                    AplicarCabecalhoRelatorio(container, "RELATÓRIO GERAL DE ESCALAS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.8f);
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
                            table.Cell().ColumnSpan(11).Element(CellBody).Text("Nenhum registro encontrado.");
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

                AplicarRodape(page);
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GerarPagamentosAsync(
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
            var inicio = dataInicio.Value.Date;
            query = query.Where(x => x.DataPagamento >= inicio);
        }

        if (dataFim.HasValue)
        {
            var fimExclusivo = dataFim.Value.Date.AddDays(1);
            query = query.Where(x => x.DataPagamento < fimExclusivo);
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

        var filtros = MontarDescricaoFiltrosPagamento(busca, dataInicio, dataFim);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container =>
                    AplicarCabecalhoRelatorio(container, "RELATÓRIO DE PAGAMENTOS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));

                    column.Item().PaddingTop(10).Table(table =>
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
                        }
                        else
                        {
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
                    });
                });

                AplicarRodape(page);
            });

            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Element(container =>
                    AplicarCabecalhoRelatorio(container, "DETALHAMENTO DOS PAGAMENTOS"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(container => AplicarLinhaFiltros(container, filtros));

                    column.Item().PaddingTop(10).Table(table =>
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
                                "Casa", "Evento", "Valor Diária", "Valor Hora Extra",
                                "Qtd. HE", "Total do Evento"
                            })
                            {
                                header.Cell().Element(CellHeader).Text(titulo);
                            }
                        });

                        if (detalhes.Count == 0)
                        {
                            table.Cell().ColumnSpan(11).Element(CellBody).Text("Nenhum item de pagamento encontrado.");
                        }
                        else
                        {
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
                    });
                });

                AplicarRodape(page);
            });
        }).GeneratePdf();
    }

    private static void AplicarCabecalhoRelatorio(IContainer container, string subtitulo)
    {
        container.Column(column =>
        {
            column.Item()
                .Background("#1F2937")
                .PaddingVertical(6)
                .AlignCenter()
                .Text("TAG GESTÃO DE SEGURANÇA")
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

    private static void AdicionarLinhaInfo(TableDescriptor table, string label, string valor)
    {
        table.Cell()
            .Element(CellInfoLabel)
            .Text(label);

        table.Cell()
            .Element(CellInfoValue)
            .Text(valor);
    }

    private static IContainer CellInfoLabel(IContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor("#D1D5DB")
            .Background("#F3F4F6")
            .Padding(4)
            .DefaultTextStyle(x => x.Bold());
    }

    private static IContainer CellInfoValue(IContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor("#D1D5DB")
            .Padding(4);
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

    private static string MontarDescricaoFiltrosEscala(
        string? casa,
        DateTime? dataInicio,
        DateTime? dataFim,
        string? nomeEvento)
    {
        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(casa))
            filtros.Add($"Casa: {casa}");

        if (dataInicio.HasValue)
            filtros.Add($"Data inicial: {dataInicio.Value:dd/MM/yyyy}");

        if (dataFim.HasValue)
            filtros.Add($"Data final: {dataFim.Value:dd/MM/yyyy}");

        if (!string.IsNullOrWhiteSpace(nomeEvento))
            filtros.Add($"Evento: {nomeEvento.Trim()}");

        return filtros.Count == 0
            ? "Sem filtros aplicados"
            : string.Join(" | ", filtros);
    }

    private static string MontarDescricaoFiltrosPagamento(
        string? busca,
        DateTime? dataInicio,
        DateTime? dataFim)
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

    private sealed class LinhaEscalaEventoPdf
    {
        public string Cooperado { get; set; } = string.Empty;
        public string Rg { get; set; } = string.Empty;
        public string Funcao { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Pagamento { get; set; } = string.Empty;
        public string HoraExtra { get; set; } = string.Empty;
    }

    private sealed class LinhaEscalaGeralPdf
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