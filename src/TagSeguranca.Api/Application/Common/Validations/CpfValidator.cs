namespace TagSeguranca.Api.Application.Common.Validations;

public static class CpfValidator
{
    public static bool EhValido(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return false;
        }

        var numeros = ApenasNumeros(cpf);

        if (numeros.Length != 11)
        {
            return false;
        }

        if (numeros.Distinct().Count() == 1)
        {
            return false;
        }

        var primeiroDigito = CalcularDigito(numeros[..9], 10);
        var segundoDigito = CalcularDigito(numeros[..10], 11);

        return numeros[9].ToString() == primeiroDigito.ToString()
            && numeros[10].ToString() == segundoDigito.ToString();
    }

    public static string ApenasNumeros(string valor)
    {
        return new string(valor.Where(char.IsDigit).ToArray());
    }

    private static int CalcularDigito(string baseCpf, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < baseCpf.Length; i++)
        {
            soma += int.Parse(baseCpf[i].ToString()) * (pesoInicial - i);
        }

        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }
}