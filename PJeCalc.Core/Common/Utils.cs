using System.Globalization;

namespace PJeCalc.Core.Common;

public static class Utils
{
    public static readonly CultureInfo LocalBR = new("pt-BR");

    public static decimal AplicarCorrecaoMonetaria(decimal valor, decimal indice)
    {
        if (indice == 0) return valor;
        return valor * indice;
    }

    public static decimal AplicarJuros(decimal valor, decimal taxa)
    {
        return valor * taxa / 100m;
    }

    public static decimal AplicarMulta(decimal valor, decimal percentual)
    {
        return valor * percentual / 100m;
    }

    public static decimal ArredondarValorMonetario(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ObterPercentualPara(decimal total, decimal percentual)
    {
        return ArredondarValorMonetario(total * percentual / 100m);
    }

    public static decimal ZerarSeNegativo(decimal valor)
    {
        return valor < 0 ? 0 : valor;
    }

    public static string FormatarMoeda(decimal valor)
    {
        return valor.ToString("C2", LocalBR);
    }

    public static string FormatarData(DateTime data)
    {
        return data.ToString("dd/MM/yyyy");
    }

    public static string FormatarCompetencia(int mes, int ano)
    {
        return $"{mes:D2}/{ano:D4}";
    }

    public static string FormatarCompetencia(DateTime data)
    {
        return FormatarCompetencia(data.Month, data.Year);
    }

    public static decimal ZerarSeNulo(decimal? valor) => valor ?? 0;
    public static decimal Somar(params decimal[] valores) => valores.Sum();
    public static decimal Subtrair(decimal a, decimal b) => a - b;
    public static decimal Multiplicar(decimal a, decimal b) => a * b;
    public static decimal Dividir(decimal a, decimal b) => b == 0 ? 0 : a / b;

    public static string FormatarValorPercentual(decimal valor)
        => $"{valor.ToString("N2", LocalBR)}%";

    public static string FormatarCPF(string cpf)
    {
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return cpf;
        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }

    public static string FormatarCNPJ(string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return cnpj;
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }

    public static bool ValidarCPF(string cpf)
    {
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return false;
        if (digits.Distinct().Count() == 1) return false;

        var sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);
        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (digits[9] - '0' != digit1) return false;

        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);
        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return digits[10] - '0' == digit2;
    }

    public static bool ValidarCNPJ(string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return false;
        if (digits.Distinct().Count() == 1) return false;

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var sum = weights1.Select((w, i) => w * (digits[i] - '0')).Sum();
        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (digits[12] - '0' != digit1) return false;

        sum = weights2.Select((w, i) => w * (digits[i] - '0')).Sum();
        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return digits[13] - '0' == digit2;
    }
}
