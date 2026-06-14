namespace TagSeguranca.Api.Application.Common.Responses;

public class ApiErrorResponse
{
    public string Mensagem { get; set; } = string.Empty;
    public object? Detalhes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiErrorResponse Criar(string mensagem, object? detalhes = null)
    {
        return new ApiErrorResponse
        {
            Mensagem = mensagem,
            Detalhes = detalhes,
            Timestamp = DateTime.UtcNow
        };
    }
}