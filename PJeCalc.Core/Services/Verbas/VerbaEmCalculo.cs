using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>Sabor da verba: define de onde vem o valor devido.</summary>
public enum TipoDaVerbaEnum
{
    /// <summary>Devido = base ÷ divisor × multiplicador × quantidade.</summary>
    Calculada,

    /// <summary>Devido = constante informada (proporcionalizada quando configurado).</summary>
    Informada,

    /// <summary>Devido derivado das ocorrências de outra(s) verba(s).</summary>
    Reflexo
}

/// <summary>Vínculo do reflexo (ou de uma principal com base em verba) com a origem.</summary>
public sealed record ItemBaseVerba(VerbaEmCalculo Verba, bool Integralizar = false);

/// <summary>
/// Histórico salarial vinculado a uma verba: salário por competência (dia 1 do mês).
/// Competência sem valor registrado contribui com zero — o motor original materializa a
/// evolução salarial em uma ocorrência por mês e não herda o salário anterior.
/// </summary>
public sealed record VinculoDeHistoricoSalarial(
    IReadOnlyDictionary<DateOnly, decimal> SalarioPorCompetencia,
    bool AplicarProporcionalidade = false,
    bool ParcelaVariavel = false);

/// <summary>Termo divisor da fórmula.</summary>
public sealed class TermoDivisor
{
    public DivisorDeVerbaEnum Tipo { get; set; } = DivisorDeVerbaEnum.OutroValor;
    public decimal OutroValor { get; set; } = 1m;
}

/// <summary>Termo quantidade da fórmula.</summary>
public sealed class TermoQuantidade
{
    public TipoDeQuantidadeEnum Tipo { get; set; } = TipoDeQuantidadeEnum.Informada;
    public decimal ValorInformado { get; set; } = 1m;
    public bool AplicarProporcionalidade { get; set; }

    /// <summary>Métrica usada quando <see cref="Tipo"/> é ImportadaDoCalendario.</summary>
    public TipoDeQuantidadeImportadaDoCalendarioEnum TipoImportadaDoCalendario { get; set; }
        = TipoDeQuantidadeImportadaDoCalendarioEnum.Repousos;
}

/// <summary>
/// Cartão de ponto consumido pela verba: valores mensais já apurados (a máquina que os
/// gera a partir de batidas é um módulo separado). O valor da competência é a soma das
/// ocorrências do mês.
/// </summary>
public sealed record CartaoDePontoDaVerba(string Nome, IReadOnlyList<(DateOnly Competencia, decimal Valor)> Ocorrencias)
{
    public decimal ValorNoMes(DateOnly mes) =>
        Ocorrencias.Where(o => o.Competencia.Year == mes.Year && o.Competencia.Month == mes.Month)
            .Sum(o => o.Valor);
}

/// <summary>Base tabelada da fórmula (última/maior remuneração, histórico, salário mínimo).</summary>
public sealed class TermoBaseTabelada
{
    public required BaseDeCalculoDoPrincipalEnum Tipo { get; init; }
    public bool AplicarProporcionalidade { get; set; }
}

/// <summary>Valor pago da verba: informado ou calculado sobre uma base tabelada.</summary>
public sealed class TermoValorPago
{
    public TipoValorPagoEnum Tipo { get; set; } = TipoValorPagoEnum.Informado;
    public decimal ValorInformado { get; set; }
    public bool AplicarProporcionalidade { get; set; }

    /// <summary>Base e quantidade usadas quando <see cref="Tipo"/> é Calculado.</summary>
    public TermoBaseTabelada? BaseTabelada { get; set; }
    public decimal Quantidade { get; set; } = 1m;
}

/// <summary>
/// Uma verba em cálculo: configuração da fórmula, período de apuração e as ocorrências
/// geradas/liquidadas pelo <see cref="MotorDeVerbas"/>.
/// </summary>
public sealed class VerbaEmCalculo
{
    public required string Nome { get; init; }
    public required TipoDaVerbaEnum Tipo { get; init; }

    public CaracteristicaDaVerbaEnum Caracteristica { get; set; } = CaracteristicaDaVerbaEnum.Comum;
    public OcorrenciaDePagamentoEnum OcorrenciaDePagamento { get; set; } = OcorrenciaDePagamentoEnum.Mensal;
    public DateOnly PeriodoInicial { get; set; }
    public DateOnly PeriodoFinal { get; set; }

    /// <summary>Proporcionaliza a constante da verba informada por dias do período.</summary>
    public bool AplicarProporcionalidade { get; set; }

    public bool ZeraValorNegativo { get; set; } = true;

    /// <summary>Se a verba compõe o principal (entra no capital dos juros de mora e no bruto).</summary>
    public bool ComporPrincipal { get; set; } = true;

    /// <summary>Marco inicial dos juros de mora das ocorrências desta verba.</summary>
    public JurosDoAjuizamentoEnum JurosDoAjuizamento { get; set; } = JurosDoAjuizamentoEnum.OcorrenciasVencidas;

    public bool ExcluirFeriasGozadas { get; set; }
    public bool ExcluirFaltaJustificada { get; set; }
    public bool ExcluirFaltaNaoJustificada { get; set; }

    /// <summary>O que esta verba entrega quando é origem de uma principal.</summary>
    public TipoDeGeracaoEnum GerarPrincipal { get; set; } = TipoDeGeracaoEnum.Diferenca;

    /// <summary>O que esta verba entrega quando é origem de um reflexo.</summary>
    public TipoDeGeracaoEnum GerarReflexo { get; set; } = TipoDeGeracaoEnum.Diferenca;

    /// <summary>
    /// Parcela de valor variável (média pela quantidade usa a média dos divisores das
    /// ocorrências em vez do divisor congelado da última).
    /// </summary>
    public bool ParcelaVariavel { get; set; }

    public List<CartaoDePontoDaVerba> CartoesDoDivisor { get; } = [];
    public List<CartaoDePontoDaVerba> CartoesDaQuantidade { get; } = [];

    // ---- fórmula da verba calculada / reflexo ----
    public TermoBaseTabelada? BaseTabelada { get; set; }
    public TermoDivisor Divisor { get; } = new();
    public decimal Multiplicador { get; set; } = 1m;
    public TermoQuantidade Quantidade { get; } = new();
    public bool Dobra { get; set; }
    public List<ItemBaseVerba> BasesVerba { get; } = [];

    // ---- fórmula da verba informada ----
    public decimal? Constante { get; set; }

    public TermoValorPago ValorPago { get; } = new();

    public List<VinculoDeHistoricoSalarial> HistoricosDaBase { get; } = [];
    public List<VinculoDeHistoricoSalarial> HistoricosDoPago { get; } = [];

    // ---- configuração do reflexo ----
    public ComportamentoDoReflexoEnum ComportamentoDoReflexo { get; set; } = ComportamentoDoReflexoEnum.ValorMensal;
    public PeriodoDaMediaDoReflexoEnum PeriodoDaMedia { get; set; } = PeriodoDaMediaDoReflexoEnum.PeriodoAquisitivo;
    public TratamentoDaFracaoDeMesDoReflexoEnum TratamentoDaFracaoDeMes { get; set; } = TratamentoDaFracaoDeMesDoReflexoEnum.Manter;

    public List<OcorrenciaDaVerba> Ocorrencias { get; } = [];

    public bool Liquidada { get; internal set; }

    /// <summary>Guarda de reentrância para detectar dependência cíclica entre verbas.</summary>
    internal bool Executando;

    public IEnumerable<OcorrenciaDaVerba> OcorrenciasAtivas => Ocorrencias.Where(o => o.Ativo);

    public TipoValorEnum TipoValor =>
        Tipo == TipoDaVerbaEnum.Informada ? TipoValorEnum.Informado : TipoValorEnum.Calculado;

    internal IEnumerable<OcorrenciaDaVerba> OcorrenciasDoMes(DateOnly mes) =>
        Ocorrencias.Where(o => o.DataInicial.Year == mes.Year && o.DataInicial.Month == mes.Month);

    internal IEnumerable<OcorrenciaDaVerba> OcorrenciasDoAno(int ano) =>
        Ocorrencias.Where(o => o.DataInicial.Year == ano);

    /// <summary>Ocorrências cuja competência cai nos 12 meses anteriores a <paramref name="mes"/>.</summary>
    internal IEnumerable<OcorrenciaDaVerba> OcorrenciasDosDozeMesesAnteriores(DateOnly mes)
    {
        var fim = PeriodoDeApuracao.Competencia(mes).AddMonths(-1);
        var inicio = PeriodoDeApuracao.Competencia(mes).AddMonths(-12);
        return Ocorrencias.Where(o =>
        {
            var competencia = PeriodoDeApuracao.Competencia(o.DataInicial);
            return competencia >= inicio && competencia <= fim;
        });
    }
}
