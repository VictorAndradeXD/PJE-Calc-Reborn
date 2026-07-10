namespace PJeCalc.Core.Models.Processo;

public class IdentificadorDoProcesso
{
    public int Numero { get; set; }
    public int Ano { get; set; }
    public int Justica { get; set; }
    public int Regiao { get; set; }
    public int Vara { get; set; }
    public int Digito { get; set; }

    public string FormatarNumero() =>
        $"{Numero:D7}-{Digito:D2}.{Ano:D4}.{Justica}.{Regiao:D2}.{Vara:D4}";
}
