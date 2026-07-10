namespace PJeCalc.Core.Common;

public class LogicoFuzzy
{
    public bool ValorBase { get; set; }
    public List<Periodo> Excecoes { get; set; } = [];

    public LogicoFuzzy() { }

    public LogicoFuzzy(bool valorBase)
    {
        ValorBase = valorBase;
    }

    public LogicoFuzzy(bool valorBase, List<Periodo> excecoes)
    {
        ValorBase = valorBase;
        Excecoes = excecoes;
    }

    public bool IsValido(DateTime data)
    {
        bool resultado = ValorBase;

        foreach (var excecao in Excecoes)
        {
            if (excecao.ContemData(data))
            {
                resultado = !resultado;
                break;
            }
        }

        return resultado;
    }

    public static LogicoFuzzy Verdadeiro => new(true);
    public static LogicoFuzzy Falso => new(false);
}
