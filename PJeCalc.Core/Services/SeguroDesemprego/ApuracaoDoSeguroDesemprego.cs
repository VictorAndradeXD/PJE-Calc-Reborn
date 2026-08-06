using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.SeguroDesemprego;

/// <summary>Resultado da apuração do seguro-desemprego.</summary>
public sealed record ResultadoDoSeguroDesemprego(
    decimal ValorDaParcela,
    decimal ValorDevido,
    decimal ValorDevidoCorrigido,
    decimal Juros,
    decimal Total);

/// <summary>
/// Apuração do seguro-desemprego (calculado): o valor da parcela vem da tabela sobre a
/// remuneração média das três competências anteriores à demissão; o devido é a parcela vezes o
/// número de parcelas, corrigido da demissão à liquidação e acrescido de juros.
///
/// <para>A remuneração média (média das diferenças das verbas dos três últimos meses + salário
/// pago) e os fatores de correção/juros vêm de módulos já validados e entram como parâmetros.</para>
/// </summary>
public static class ApuracaoDoSeguroDesemprego
{
    public static ResultadoDoSeguroDesemprego Apurar(
        TabelaSeguroDesemprego tabela,
        decimal remuneracaoMensal,
        int numeroDeParcelas,
        decimal indiceDeCorrecao = 1m,
        decimal taxaDeJuros = 0m)
    {
        ArgumentNullException.ThrowIfNull(tabela);

        var parcela = tabela.ValorDaParcela(remuneracaoMensal);
        var valorDevido = Arredondar(parcela * numeroDeParcelas);
        var corrigido = AplicacaoDeFator.Aplicar(valorDevido, indiceDeCorrecao);
        var juros = Arredondar(corrigido * (taxaDeJuros / 100m));

        return new ResultadoDoSeguroDesemprego(parcela, valorDevido, corrigido, juros, corrigido + juros);
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
