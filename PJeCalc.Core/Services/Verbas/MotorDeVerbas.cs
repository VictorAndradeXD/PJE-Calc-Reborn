using PJeCalc.Core.Enums;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Motor de verbas: gera as ocorrências (competências) conforme a ocorrência de pagamento
/// da verba e as liquida — resolvendo base, termos e índice de correção — com a mesma
/// matemática do PJe-Calc original (validada por golden values).
///
/// A geração grava divisor, multiplicador, quantidade, pago e seus integrais; a base da
/// verba calculada/reflexo só nasce na liquidação, quando a fórmula
/// <c>devido = base ÷ divisor × multiplicador × quantidade</c> (dobrada quando é o caso)
/// é aplicada e arredondada a 2 casas (HALF_EVEN). Dependências entre verbas são
/// resolvidas recursivamente, com detecção de ciclo.
/// </summary>
public sealed class MotorDeVerbas(ContextoDeVerbas contexto)
{
    private const int DiaDoVencimentoDoDecimoTerceiro = 20;
    private const int MinimoDeDiasParaUmAvo = 15;

    private readonly ContextoDeVerbas _contexto = contexto;

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
    private static decimal? Arredondar(decimal? valor) => valor is { } v ? Arredondar(v) : null;

    // ------------------------------------------------------------------
    // Geração de ocorrências
    // ------------------------------------------------------------------

    public void GerarOcorrencias(VerbaEmCalculo verba)
    {
        ExecutarComGuardaDeCiclo(verba, () =>
        {
            verba.Ocorrencias.Clear();
            verba.Liquidada = false;

            switch (verba.OcorrenciaDePagamento)
            {
                case OcorrenciaDePagamentoEnum.Mensal:
                    foreach (var periodo in PeriodoDeApuracao.QuebrarEmMeses(verba.PeriodoInicial, verba.PeriodoFinal))
                        CriarOcorrencia(verba, periodo);
                    break;

                case OcorrenciaDePagamentoEnum.Desligamento:
                    GerarOcorrenciasDeDesligamento(verba);
                    break;

                case OcorrenciaDePagamentoEnum.Dezembro:
                    GerarOcorrenciasDeDezembro(verba);
                    break;

                case OcorrenciaDePagamentoEnum.PeriodoAquisitivo:
                    throw new NotSupportedException(
                        "Geração por período aquisitivo (férias) será implementada na etapa de Férias.");
            }
        });
    }

    private void GerarOcorrenciasDeDesligamento(VerbaEmCalculo verba)
    {
        if (_contexto.DataDemissao is not { } demissao)
            return;

        if (demissao <= verba.PeriodoFinal)
        {
            var inicio = verba.Caracteristica == CaracteristicaDaVerbaEnum.AvisoPrevio
                ? demissao
                : InicioDoMesDaDemissaoRespeitandoAVerba(verba, demissao);
            CriarOcorrencia(verba, new PeriodoDeApuracao(inicio, demissao));
        }
        else if (MesmoMesEAno(verba.PeriodoFinal, demissao) && verba.Caracteristica == CaracteristicaDaVerbaEnum.Comum)
        {
            var inicio = InicioDoMesDaDemissaoRespeitandoAVerba(verba, demissao);
            CriarOcorrencia(verba, new PeriodoDeApuracao(inicio, verba.PeriodoFinal));
        }
    }

    private static DateOnly InicioDoMesDaDemissaoRespeitandoAVerba(VerbaEmCalculo verba, DateOnly demissao)
    {
        var inicio = PeriodoDeApuracao.Competencia(demissao);
        return verba.PeriodoInicial > inicio ? verba.PeriodoInicial : inicio;
    }

    private void GerarOcorrenciasDeDezembro(VerbaEmCalculo verba)
    {
        var demissao = _contexto.DataDemissao;
        var demissaoNoFimDaVerba = demissao is { } d && verba.PeriodoFinal == d;

        foreach (var periodo in PeriodoDeApuracao.QuebrarEmMeses(verba.PeriodoInicial, verba.PeriodoFinal, mes: 12))
        {
            if (demissaoNoFimDaVerba && MesmoMesEAno(periodo.Fim, demissao!.Value))
            {
                // Dezembro da demissão: paga no dia 20 e, se o aviso indenizado projeta o
                // contrato para o ano seguinte, uma segunda ocorrência no dia da demissão
                // apura os avos do período projetado.
                if (demissao.Value.Day > DiaDoVencimentoDoDecimoTerceiro &&
                    periodo.Inicio.Day <= DiaDoVencimentoDoDecimoTerceiro)
                {
                    CriarOcorrenciaNoDia(verba, periodo.Inicio.Year, DiaDoVencimentoDoDecimoTerceiro);
                    if (_contexto.ProjetaAvisoIndenizado)
                        CriarOcorrenciaNoDia(verba, periodo.Inicio.Year, demissao.Value.Day);
                }
                else
                {
                    CriarOcorrenciaNoDia(verba, periodo.Inicio.Year, demissao.Value.Day);
                }
                continue;
            }

            var contemODia20 = periodo.Inicio.Day <= DiaDoVencimentoDoDecimoTerceiro &&
                               periodo.Fim.Day >= DiaDoVencimentoDoDecimoTerceiro;
            if (contemODia20)
                CriarOcorrenciaNoDia(verba, periodo.Inicio.Year, DiaDoVencimentoDoDecimoTerceiro);
        }

        // Demissão fora de dezembro: 13º proporcional apurado no próprio dia da demissão.
        if (demissaoNoFimDaVerba && demissao!.Value.Month != 12)
            CriarOcorrencia(verba, new PeriodoDeApuracao(demissao.Value, demissao.Value));
    }

    private void CriarOcorrenciaNoDia(VerbaEmCalculo verba, int ano, int dia)
    {
        var data = new DateOnly(ano, 12, dia);
        CriarOcorrencia(verba, new PeriodoDeApuracao(data, data));
    }

    private static bool MesmoMesEAno(DateOnly a, DateOnly b) => a.Year == b.Year && a.Month == b.Month;

    private void CriarOcorrencia(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
        var ocorrencia = new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = periodo.Inicio,
            DataFinal = periodo.Fim,
            Valor = verba.TipoValor,
        };
        ocorrencia.Integralizador = valor => IntegralizarNaGeracao(verba, periodo, valor);

        ocorrencia.Divisor = ResolverDivisor(verba);
        if (ocorrencia.Divisor == 0m)
            ocorrencia.Ativo = false;

        ocorrencia.Multiplicador = verba.Tipo == TipoDaVerbaEnum.Informada ? null : verba.Multiplicador;

        var quantidade = ResolverQuantidade(verba, periodo, out var quantidadeIntegral);
        ocorrencia.Quantidade = quantidade;
        ocorrencia.QuantidadeIntegral = quantidadeIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, quantidade);

        var devido = ResolverDevidoDaGeracao(verba, periodo, out var devidoIntegral);
        ocorrencia.Devido = Arredondar(devido);
        ocorrencia.DevidoIntegral = devidoIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, devido);

        var pago = ResolverValorPago(verba, periodo, out var pagoIntegral);
        ocorrencia.Pago = Arredondar(pago);
        ocorrencia.PagoIntegral = pagoIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, pago);

        ocorrencia.Dobra = verba.Tipo != TipoDaVerbaEnum.Informada && verba.Dobra;

        CalcularValorDevidoDaOcorrencia(ocorrencia);
        verba.Ocorrencias.Add(ocorrencia);
    }

    /// <summary>
    /// Fórmula do devido de uma ocorrência calculada. Sem base (na geração), anula o
    /// devido — ele nasce na liquidação. Ocorrências informadas preservam a constante.
    /// </summary>
    public void CalcularValorDevidoDaOcorrencia(OcorrenciaDaVerba ocorrencia)
    {
        if (ocorrencia.Valor != TipoValorEnum.Calculado)
            return;

        if (ocorrencia.Base is { } base_ && ocorrencia.Divisor is { } divisor &&
            ocorrencia.Multiplicador is { } multiplicador && ocorrencia.Quantidade is { } quantidade)
        {
            var devido = base_ / divisor * multiplicador * quantidade;
            if (ocorrencia.Dobra)
                devido *= 2m;
            ocorrencia.Devido = Arredondar(devido);

            // Integral: base integral distinta prevalece; senão, quantidade integral distinta.
            decimal baseIntegral, quantidadeIntegral;
            if (ocorrencia.BaseIntegral is { } bi && bi != base_)
            {
                baseIntegral = bi;
                quantidadeIntegral = quantidade;
            }
            else
            {
                baseIntegral = base_;
                quantidadeIntegral = ocorrencia.QuantidadeIntegral is { } qi && qi != quantidade ? qi : quantidade;
            }

            var devidoIntegral = baseIntegral / divisor * multiplicador * quantidadeIntegral;
            if (ocorrencia.Dobra)
                devidoIntegral *= 2m;
            ocorrencia.DevidoIntegral = Arredondar(devidoIntegral);
        }
        else
        {
            ocorrencia.Devido = null;
            ocorrencia.DevidoIntegral = null;
        }
    }

    // ------------------------------------------------------------------
    // Liquidação
    // ------------------------------------------------------------------

    public void Liquidar(VerbaEmCalculo verba)
    {
        ExecutarComGuardaDeCiclo(verba, () =>
        {
            foreach (var ocorrencia in verba.OcorrenciasAtivas)
            {
                var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
                var valorDaBase = ObterValorDaBase(verba, periodo, out var valorDaBaseIntegral);

                ocorrencia.Base = Arredondar(valorDaBase);
                ocorrencia.BaseIntegral = Arredondar(valorDaBaseIntegral);
                CalcularValorDevidoDaOcorrencia(ocorrencia);
                ocorrencia.IndiceAcumulado = _contexto.ObterIndiceAcumulado(ocorrencia.DataInicial);
            }
            verba.Liquidada = true;
        });
    }

    private decimal? ObterValorDaBase(VerbaEmCalculo verba, PeriodoDeApuracao periodo, out decimal? valorIntegral)
    {
        valorIntegral = null;
        switch (verba.Tipo)
        {
            case TipoDaVerbaEnum.Informada:
                return null;

            case TipoDaVerbaEnum.Reflexo:
            {
                var integral = new AcumuladorDeIntegral();
                var valor = ResolverBaseVerba(verba, periodo, integral);
                valorIntegral = integral.Valor;
                return valor;
            }

            default: // Calculada: base tabelada + base em outras verbas
            {
                var integral = new AcumuladorDeIntegral();
                decimal? baseTabelada = null;
                if (verba.BaseTabelada is not null)
                    baseTabelada = ResolverBaseTabelada(verba, verba.BaseTabelada, periodo, fasePago: false, integral);
                var baseVerba = verba.BasesVerba.Count > 0
                    ? ResolverBaseVerba(verba, periodo, integral)
                    : (verba.BaseTabelada is null ? 0m : (decimal?)null);
                valorIntegral = integral.Valor;
                return (baseVerba, baseTabelada) switch
                {
                    ({ } bv, { } bt) => bv + bt,
                    ({ } bv, null) => bv,
                    (null, { } bt) => bt,
                    _ => null,
                };
            }
        }
    }

    /// <summary>Acumulador do "valor integral" propagado pelos termos (parametro.valorIntegral).</summary>
    private sealed class AcumuladorDeIntegral
    {
        public decimal? Valor { get; private set; }
        public void Acumular(decimal? valor)
        {
            if (valor is null)
                return;
            Valor = Valor is { } atual ? atual + valor.Value : valor;
        }
    }

    // ------------------------------------------------------------------
    // Termos
    // ------------------------------------------------------------------

    private decimal? ResolverDivisor(VerbaEmCalculo verba)
    {
        if (verba.Tipo == TipoDaVerbaEnum.Informada)
            return null;
        return verba.Divisor.Tipo switch
        {
            DivisorDeVerbaEnum.OutroValor => verba.Divisor.OutroValor,
            _ => throw new NotSupportedException(
                $"Divisor {verba.Divisor.Tipo} depende de carga horária/calendário/cartão de ponto (etapa futura)."),
        };
    }

    private decimal? ResolverQuantidade(VerbaEmCalculo verba, PeriodoDeApuracao periodo, out decimal? valorIntegral)
    {
        valorIntegral = null;
        if (verba.Tipo == TipoDaVerbaEnum.Informada)
            return null;

        switch (verba.Quantidade.Tipo)
        {
            case TipoDeQuantidadeEnum.Informada:
            {
                var valor = verba.Quantidade.ValorInformado;
                if (!verba.Quantidade.AplicarProporcionalidade)
                    return valor;
                valorIntegral = valor;
                return Proporcionalizacao.Proporcionalizar(
                    periodo.Inicio, periodo.Fim, valor, DiasParaExcluir(verba, periodo));
            }

            case TipoDeQuantidadeEnum.Avos:
                return ContarAvos(verba, periodo);

            case TipoDeQuantidadeEnum.Apurada:
                return _contexto.QuantidadeDeDiasDoAvisoPrevio();

            default:
                throw new NotSupportedException(
                    $"Quantidade {verba.Quantidade.Tipo} depende de calendário/cartão de ponto (etapa futura).");
        }
    }

    /// <summary>
    /// Avos do 13º: um por mês com pelo menos 15 dias (descontadas faltas não justificadas)
    /// na janela do ano da ocorrência, limitada pela admissão e pela demissão projetada
    /// com o aviso indenizado. Quando a demissão em dezembro (após o dia 20) projeta o
    /// contrato para o ano seguinte, a ocorrência do dia da demissão conta os avos do
    /// período projetado (janela reiniciada em 1º de janeiro seguinte).
    /// </summary>
    private decimal ContarAvos(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
        if (verba.OcorrenciaDePagamento != OcorrenciaDePagamentoEnum.Dezembro)
            throw new NotSupportedException("Avos por período aquisitivo (férias) serão implementados na etapa de Férias.");

        var ano = periodo.Inicio.Year;
        var inicioDaJanela = new DateOnly(ano, 1, 1);
        if (_contexto.LimitarAvosAoPeriodoDoCalculo)
        {
            if (verba.PeriodoInicial > inicioDaJanela)
            {
                var competenciaInicialDaVerba = PeriodoDeApuracao.Competencia(verba.PeriodoInicial);
                inicioDaJanela = _contexto.DataAdmissao > competenciaInicialDaVerba
                    ? _contexto.DataAdmissao
                    : competenciaInicialDaVerba;
            }
        }
        else if (_contexto.DataAdmissao > inicioDaJanela)
        {
            inicioDaJanela = _contexto.DataAdmissao;
        }

        var fimDaJanela = new DateOnly(ano, 12, 31);
        if (_contexto.DataDemissao is { } demissao)
        {
            var demissaoProjetada = _contexto.ProjetaAvisoIndenizado
                ? demissao.AddDays(_contexto.QuantidadeDeDiasDoAvisoPrevio())
                : demissao;
            if (fimDaJanela > demissaoProjetada)
            {
                fimDaJanela = demissaoProjetada;
            }
            else if (periodo.Fim == demissao)
            {
                fimDaJanela = demissaoProjetada;
                if (demissao.Month == 12 && demissao.Day > DiaDoVencimentoDoDecimoTerceiro)
                    inicioDaJanela = new DateOnly(demissaoProjetada.Year, 1, 1);
            }
        }

        var avos = 0;
        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(inicioDaJanela, fimDaJanela))
        {
            var dias = mes.Fim.Day - mes.Inicio.Day + 1 - _contexto.FaltasNaoJustificadas(mes);
            if (dias >= MinimoDeDiasParaUmAvo)
                avos++;
        }
        return avos;
    }

    private decimal? ResolverDevidoDaGeracao(VerbaEmCalculo verba, PeriodoDeApuracao periodo, out decimal? valorIntegral)
    {
        valorIntegral = null;
        if (verba.Tipo != TipoDaVerbaEnum.Informada)
            return 0m;

        var constante = verba.Constante;
        if (constante is null || !verba.AplicarProporcionalidade)
            return constante;

        valorIntegral = constante;
        return Proporcionalizacao.Proporcionalizar(
            periodo.Inicio, periodo.Fim, constante.Value, DiasParaExcluir(verba, periodo));
    }

    private decimal? ResolverValorPago(VerbaEmCalculo verba, PeriodoDeApuracao periodo, out decimal? valorIntegral)
    {
        valorIntegral = null;
        var pago = verba.ValorPago;

        if (pago.Tipo == TipoValorPagoEnum.Informado)
        {
            if (!pago.AplicarProporcionalidade)
                return pago.ValorInformado;
            valorIntegral = pago.ValorInformado;
            return Proporcionalizacao.Proporcionalizar(
                periodo.Inicio, periodo.Fim, pago.ValorInformado, DiasParaExcluir(verba, periodo));
        }

        if (pago.BaseTabelada is null)
            return null;

        var integral = new AcumuladorDeIntegral();
        var valor = ResolverBaseTabelada(verba, pago.BaseTabelada, periodo, fasePago: true, integral);
        if (valor is null)
            return null;

        valor *= pago.Quantidade;
        valorIntegral = integral.Valor is { } vi ? vi * pago.Quantidade : null;
        return valor;
    }

    private decimal? ResolverBaseTabelada(
        VerbaEmCalculo verba, TermoBaseTabelada termo, PeriodoDeApuracao periodo, bool fasePago,
        AcumuladorDeIntegral integral)
    {
        if (verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias)
            throw new NotSupportedException("Base tabelada de férias será implementada na etapa de Férias.");

        decimal? valor = termo.Tipo switch
        {
            BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao => _contexto.ValorUltimaRemuneracao,
            BaseDeCalculoDoPrincipalEnum.MaiorRemuneracao => _contexto.ValorMaiorRemuneracao,
            BaseDeCalculoDoPrincipalEnum.HistoricoSalarial =>
                ResolverHistoricoSalarial(verba, fasePago ? verba.HistoricosDoPago : verba.HistoricosDaBase, periodo, integral),
            BaseDeCalculoDoPrincipalEnum.SalarioMinimo =>
                _contexto.SalarioMinimoNaCompetencia?.Invoke(PeriodoDeApuracao.Competencia(periodo.Inicio))
                    ?? throw new NotSupportedException("Configure ContextoDeVerbas.SalarioMinimoNaCompetencia."),
            _ => throw new NotSupportedException($"Base tabelada {termo.Tipo} (etapa futura)."),
        };

        var ehRemuneracaoDeReferencia = termo.Tipo is BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao
            or BaseDeCalculoDoPrincipalEnum.MaiorRemuneracao;
        if (valor is { } v && ehRemuneracaoDeReferencia && termo.AplicarProporcionalidade)
        {
            integral.Acumular(v);
            valor = Proporcionalizacao.Proporcionalizar(
                periodo.Inicio, periodo.Fim, v, DiasParaExcluir(verba, periodo));
        }
        return valor;
    }

    /// <summary>
    /// Base do histórico salarial no período: soma, por vínculo, o salário registrado na
    /// competência (sem valor registrado, contribui zero). Períodos que cruzam meses são
    /// divididos em dois. Proporcionaliza por dias quando o vínculo pede, acumulando o
    /// valor cheio como integral; senão integraliza o valor da competência.
    /// </summary>
    private decimal ResolverHistoricoSalarial(
        VerbaEmCalculo verba, List<VinculoDeHistoricoSalarial> vinculos, PeriodoDeApuracao periodo,
        AcumuladorDeIntegral integral)
    {
        if (!periodo.DatasDoMesmoMes)
        {
            var fimDoPrimeiroMes = PeriodoDeApuracao.UltimoDiaDoMes(periodo.Inicio);
            var inicioDoSegundoMes = PeriodoDeApuracao.Competencia(periodo.Fim);
            var primeiro = ResolverHistoricoSalarialNoMes(verba, vinculos, new PeriodoDeApuracao(periodo.Inicio, fimDoPrimeiroMes), integral);
            var segundo = ResolverHistoricoSalarialNoMes(verba, vinculos, new PeriodoDeApuracao(inicioDoSegundoMes, periodo.Fim), integral);
            return primeiro + segundo;
        }
        return ResolverHistoricoSalarialNoMes(verba, vinculos, periodo, integral);
    }

    private decimal ResolverHistoricoSalarialNoMes(
        VerbaEmCalculo verba, List<VinculoDeHistoricoSalarial> vinculos, PeriodoDeApuracao periodo,
        AcumuladorDeIntegral integral)
    {
        var competencia = PeriodoDeApuracao.Competencia(periodo.Inicio);
        var total = 0m;
        foreach (var vinculo in vinculos)
        {
            if (!vinculo.SalarioPorCompetencia.TryGetValue(competencia, out var salario))
                continue;

            var exclusoes = DiasParaExcluir(verba, periodo);
            if (vinculo.AplicarProporcionalidade)
            {
                integral.Acumular(salario);
                total += Proporcionalizacao.Proporcionalizar(periodo.Inicio, periodo.Fim, salario, exclusoes);
            }
            else
            {
                if (periodo.TotalDeDias - exclusoes > 0)
                    integral.Acumular(Proporcionalizacao.Integralizar(periodo.Inicio, periodo.Fim, salario, exclusoes));
                total += salario;
            }
        }
        return total;
    }

    // ------------------------------------------------------------------
    // Base em outras verbas (principal e reflexo)
    // ------------------------------------------------------------------

    private decimal ResolverBaseVerba(VerbaEmCalculo verba, PeriodoDeApuracao periodo, AcumuladorDeIntegral integral)
    {
        var valor = 0m;
        decimal? valorIntegral = verba.BasesVerba.Count > 0 ? 0m : null;
        foreach (var item in verba.BasesVerba)
        {
            if (!item.Verba.Liquidada)
                Liquidar(item.Verba);

            decimal valorDaBase, valorDaBaseIntegral;
            if (verba.Tipo == TipoDaVerbaEnum.Reflexo)
            {
                valorDaBase = valorDaBaseIntegral = ComportamentoDoReflexo.Resolver(_contexto, verba, item, periodo);
            }
            else
            {
                valorDaBase = MediaPonderadaPorDias(verba, item, periodo, out valorDaBaseIntegral);
            }
            valor += valorDaBase;
            valorIntegral += valorDaBaseIntegral;
        }
        integral.Acumular(valorIntegral);
        return valor;
    }

    /// <summary>
    /// Base de uma verba principal em outra verba: soma mensal do devido/diferença da
    /// origem, ponderada pelos dias de cada mês do período e dividida pelos dias da
    /// ocorrência (média ponderada), com integralização opcional por item.
    /// </summary>
    private decimal MediaPonderadaPorDias(
        VerbaEmCalculo verba, ItemBaseVerba item, PeriodoDeApuracao periodo, out decimal valorIntegralDaBase)
    {
        if (verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias)
            throw new NotSupportedException("Base em verba para férias será implementada na etapa de Férias.");

        var origem = item.Verba;
        var valorDaBase = 0m;
        var valorDaBaseIntegral = 0m;
        var diasDaOcorrencia = periodo.TotalDeDias;

        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(periodo.Inicio, periodo.Fim))
        {
            var (valorDoPeriodo, valorDoPeriodoIntegral, diasCobertos, diasParaExcluir) =
                SomarOcorrenciasDoMes(origem, mes.Fim, origem.GerarPrincipal);

            if (diasCobertos == 0)
            {
                valorDoPeriodo = 0m;
                valorDoPeriodoIntegral = 0m;
            }
            else if (item.Integralizar)
            {
                valorDoPeriodo = valorDoPeriodoIntegral
                    ?? IntegralizarPeriodoCondensado(mes.Fim, valorDoPeriodo, diasCobertos, diasParaExcluir);
                valorDoPeriodoIntegral = valorDoPeriodo;
            }

            valorDaBase += valorDoPeriodo * mes.TotalDeDias;
            valorDaBaseIntegral += (valorDoPeriodoIntegral ?? 0m) * mes.TotalDeDias;
        }

        valorIntegralDaBase = valorDaBaseIntegral / diasDaOcorrencia;
        return valorDaBase / diasDaOcorrencia;
    }

    /// <summary>
    /// Soma devido/diferença das ocorrências ativas da origem no mês de <paramref name="mes"/>,
    /// com os dias cobertos, o primeiro valor integral não nulo e os dias a excluir da origem.
    /// </summary>
    internal static (decimal Valor, decimal? ValorIntegral, int DiasCobertos, int DiasParaExcluir)
        SomarOcorrenciasDoMes(VerbaEmCalculo origem, DateOnly mes, TipoDeGeracaoEnum geracao)
    {
        var valor = 0m;
        decimal? valorIntegral = null;
        var diasCobertos = new HashSet<DateOnly>();
        var diasParaExcluir = 0;

        foreach (var ocorrencia in origem.OcorrenciasDoMes(mes))
        {
            if (!ocorrencia.Ativo)
                continue;
            MarcarDias(diasCobertos, ocorrencia);
            diasParaExcluir += DiasParaExcluirDaOrigem(origem, ocorrencia);
            var (v, vi) = geracao == TipoDeGeracaoEnum.Devido
                ? (ocorrencia.Devido ?? 0m, ocorrencia.DevidoIntegral)
                : (ocorrencia.Diferenca, (decimal?)ocorrencia.DiferencaIntegral);
            valor += v;
            valorIntegral ??= vi;
        }
        return (valor, valorIntegral, diasCobertos.Count, diasParaExcluir);
    }

    private static void MarcarDias(HashSet<DateOnly> dias, OcorrenciaDaVerba ocorrencia)
    {
        for (var d = ocorrencia.DataInicial; d <= ocorrencia.DataFinal; d = d.AddDays(1))
            dias.Add(d);
    }

    /// <summary>
    /// Integraliza o valor coberto de um mês parcial usando o "período condensado" do
    /// motor: do dia 1 ao dia N (N = dias cobertos) do mesmo mês.
    /// </summary>
    internal static decimal IntegralizarPeriodoCondensado(DateOnly mes, decimal valor, int diasCobertos, int diasParaExcluir)
    {
        var competencia = PeriodoDeApuracao.Competencia(mes);
        var exclusoes = Math.Max(diasParaExcluir, 0);
        return Proporcionalizacao.Integralizar(competencia, competencia.AddDays(diasCobertos - 1), valor, exclusoes);
    }

    /// <summary>Dias a excluir de uma ocorrência conforme os flags da verba de ORIGEM.</summary>
    internal static int DiasParaExcluirDaOrigem(VerbaEmCalculo origem, OcorrenciaDaVerba ocorrencia)
    {
        // Nesta etapa os provedores de férias/faltas retornam zero; a regra do 31 ainda
        // se aplica quando algum flag de exclusão está ligado (paridade com o motor).
        var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
        var exclusoes = 0;
        exclusoes = Proporcionalizacao.AjustarExclusoesParaMesDe31(periodo.TotalDeDias, exclusoes);
        return exclusoes;
    }

    // ------------------------------------------------------------------
    // Apoio
    // ------------------------------------------------------------------

    /// <summary>
    /// Padrão de dias a excluir do motor: férias gozadas, regra do 31 e faltas —
    /// respeitando os flags da verba.
    /// </summary>
    internal int DiasParaExcluir(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
        var exclusoes = 0;
        if (verba.ExcluirFeriasGozadas)
            exclusoes += _contexto.DiasDeFeriasGozadas(periodo);
        exclusoes = Proporcionalizacao.AjustarExclusoesParaMesDe31(periodo.TotalDeDias, exclusoes);
        if (verba.ExcluirFaltaJustificada)
            exclusoes += _contexto.FaltasJustificadas(periodo);
        if (verba.ExcluirFaltaNaoJustificada)
            exclusoes += _contexto.FaltasNaoJustificadas(periodo);
        return exclusoes;
    }

    private decimal IntegralizarNaGeracao(VerbaEmCalculo verba, PeriodoDeApuracao periodo, decimal valor)
    {
        var exclusoes = DiasParaExcluir(verba, periodo);
        if (periodo.TotalDeDias <= exclusoes)
            exclusoes = 0;
        return Proporcionalizacao.Integralizar(periodo.Inicio, periodo.Fim, valor, exclusoes);
    }

    private decimal IntegralizarNaGeracaoOuZero(VerbaEmCalculo verba, PeriodoDeApuracao periodo, decimal? valor) =>
        valor is { } v ? IntegralizarNaGeracao(verba, periodo, v) : 0m;

    private static void ExecutarComGuardaDeCiclo(VerbaEmCalculo verba, Action acao)
    {
        if (verba.Executando)
            throw new InvalidOperationException($"Dependência cíclica na verba '{verba.Nome}'.");
        verba.Executando = true;
        try
        {
            acao();
        }
        finally
        {
            verba.Executando = false;
        }
    }
}
