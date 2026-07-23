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
///
/// Férias geram por período aquisitivo: uma ocorrência por parte de gozo (dividida no fim
/// do período concessivo — a parte posterior recebe a dobra), saldo/indenizadas em
/// ocorrência de 1 dia na demissão e o período fracionário (≥ 15 dias até a demissão
/// projetada com o aviso). A prescrição quinquenal barra férias cujo concessivo terminou
/// antes da data de prescrição.
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
                    GerarOcorrenciasDePeriodoAquisitivo(verba);
                    break;
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

    private void GerarOcorrenciasDePeriodoAquisitivo(VerbaEmCalculo verba)
    {
        var demissao = _contexto.DataDemissao;
        var demissaoNoPeriodo = demissao is { } dem && dem <= verba.PeriodoFinal;

        // Período aquisitivo fracionário: do fim do último PA completo (ou admissão) até a
        // demissão projetada com o aviso; só conta com pelo menos 15 dias.
        PeriodoDeApuracao? fracionario = null;
        if (demissaoNoPeriodo)
        {
            var ultimoAquisitivo = _contexto.ListaDeFerias
                .Select(f => f.PeriodoAquisitivo)
                .OrderBy(pa => pa.Fim)
                .Cast<PeriodoDeApuracao?>()
                .LastOrDefault();
            var inicioFracionario = ultimoAquisitivo?.Fim.AddDays(1) ?? _contexto.DataAdmissao;
            inicioFracionario = AjustarPorFaltaQueReiniciaFerias(inicioFracionario);
            var projetada = _contexto.DataDemissaoProjetada!.Value;
            if (projetada.DayNumber - inicioFracionario.DayNumber + 1 >= MinimoDeDiasParaUmAvo)
                fracionario = new PeriodoDeApuracao(inicioFracionario, projetada);
        }

        foreach (var ferias in _contexto.ListaDeFerias)
        {
            var aquisitivo = ferias.PeriodoAquisitivo;
            var prescricaoPermite = !_contexto.PrescricaoQuinquenal ||
                _contexto.DataPrescricaoQuinquenal is not { } prescricao ||
                prescricao <= ferias.PeriodoConcessivo.Fim;

            switch (ferias.Situacao)
            {
                case SituacaoDaFeriasEnum.Gozadas:
                case SituacaoDaFeriasEnum.GozadasParcialmente:
                {
                    var fimDoConcessivo = ferias.PeriodoConcessivo.Fim;
                    var gozos = new (PeriodoDeApuracao? Gozo, bool Dobra)[]
                    {
                        (ferias.PeriodoDeGozo1, ferias.DobraDoPeriodoDeGozo1),
                        (ferias.PeriodoDeGozo2, ferias.DobraDoPeriodoDeGozo2),
                        (ferias.PeriodoDeGozo3, ferias.DobraDoPeriodoDeGozo3),
                    };
                    foreach (var (gozoTalvez, dobraDoGozo) in gozos)
                    {
                        if (gozoTalvez is not { } gozo)
                            continue;
                        if (gozo.Inicio >= verba.PeriodoInicial && gozo.Inicio <= verba.PeriodoFinal)
                        {
                            var partes = gozo.DividirNaData(fimDoConcessivo);
                            foreach (var parte in partes)
                            {
                                // Cruza o fim do concessivo: a parte dentro dele sai sem
                                // dobra; a posterior (ou o gozo inteiro) leva a do gozo.
                                var dobra = partes.Count == 2 && parte.Inicio <= fimDoConcessivo
                                    ? false
                                    : dobraDoGozo;
                                CriarOcorrencia(verba, parte, aquisitivo, dobra,
                                    feriasIndenizadas: false, feriasComAbono: ferias.Abono);
                            }
                        }
                    }

                    if (ferias.Prazo > ferias.TotalDeDiasDeGozo && demissaoNoPeriodo && prescricaoPermite)
                    {
                        // Saldo não gozado: ocorrência de 1 dia na demissão, indenizada.
                        CriarOcorrencia(verba, new PeriodoDeApuracao(demissao!.Value, demissao.Value),
                            aquisitivo, ferias.DobraGeral, feriasIndenizadas: true, feriasComAbono: ferias.Abono);
                    }
                    break;
                }

                case SituacaoDaFeriasEnum.Indenizadas when demissaoNoPeriodo && prescricaoPermite:
                    CriarOcorrencia(verba, new PeriodoDeApuracao(demissao!.Value, demissao.Value),
                        aquisitivo, ferias.DobraGeral, feriasIndenizadas: true, feriasComAbono: false);
                    break;
            }
        }

        if (fracionario is { } frac)
        {
            CriarOcorrencia(verba, new PeriodoDeApuracao(demissao!.Value, demissao.Value),
                frac, dobraObrigatoria: false, feriasIndenizadas: true, feriasComAbono: false);
        }
    }

    /// <summary>A última falta que reinicia férias empurra o início do PA fracionário.</summary>
    private DateOnly AjustarPorFaltaQueReiniciaFerias(DateOnly inicioFracionario)
    {
        var reinicios = GeradorDePeriodosDeFerias.ReiniciosPorFalta(_contexto);
        return reinicios.Count > 0 && reinicios[^1] > inicioFracionario ? reinicios[^1] : inicioFracionario;
    }

    private void CriarOcorrenciaNoDia(VerbaEmCalculo verba, int ano, int dia)
    {
        var data = new DateOnly(ano, 12, dia);
        CriarOcorrencia(verba, new PeriodoDeApuracao(data, data));
    }

    private static bool MesmoMesEAno(DateOnly a, DateOnly b) => a.Year == b.Year && a.Month == b.Month;

    private void CriarOcorrencia(
        VerbaEmCalculo verba, PeriodoDeApuracao periodo, PeriodoDeApuracao? periodoAquisitivo = null,
        bool dobraObrigatoria = false, bool feriasIndenizadas = false, bool feriasComAbono = false)
    {
        var ocorrencia = new OcorrenciaDaVerba
        {
            Verba = verba,
            DataInicial = periodo.Inicio,
            DataFinal = periodo.Fim,
            Valor = verba.TipoValor,
            InicioDoPeriodoAquisitivo = periodoAquisitivo?.Inicio,
            FimDoPeriodoAquisitivo = periodoAquisitivo?.Fim,
            FeriasIndenizadas = feriasIndenizadas,
            FeriasComAbono = feriasComAbono,
        };
        ocorrencia.Integralizador = valor => IntegralizarNaGeracao(verba, periodo, valor);

        ocorrencia.Divisor = ResolverDivisor(verba, periodo);
        if (ocorrencia.Divisor == 0m)
            ocorrencia.Ativo = false;

        ocorrencia.Multiplicador = verba.Tipo == TipoDaVerbaEnum.Informada ? null : verba.Multiplicador;

        var quantidade = ResolverQuantidade(verba, periodo, periodoAquisitivo, out var quantidadeIntegral);
        ocorrencia.Quantidade = quantidade;
        ocorrencia.QuantidadeIntegral = quantidadeIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, quantidade);

        var devido = ResolverDevidoDaGeracao(verba, periodo, out var devidoIntegral);
        ocorrencia.Devido = Arredondar(devido);
        ocorrencia.DevidoIntegral = devidoIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, devido);

        var pago = ResolverValorPago(verba, periodo, periodoAquisitivo, out var pagoIntegral);
        ocorrencia.Pago = Arredondar(pago);
        ocorrencia.PagoIntegral = pagoIntegral ?? IntegralizarNaGeracaoOuZero(verba, periodo, pago);

        ocorrencia.Dobra = verba.Tipo != TipoDaVerbaEnum.Informada && verba.Dobra;
        if (dobraObrigatoria)
            ocorrencia.Dobra = true;

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
                var valorDaBase = ObterValorDaBase(verba, ocorrencia, out var valorDaBaseIntegral);

                if (ocorrencia.FeriasComAbono && verba.TipoValor == TipoValorEnum.Calculado)
                {
                    // O abono é pago junto: a base do gozo é inflada pelo fator
                    // prazo ÷ (prazo − dias de abono); as incidências o retiram depois.
                    var fator = CalcularFatorAbono(ocorrencia);
                    ocorrencia.FatorAbono = fator;
                    valorDaBase *= fator;
                    valorDaBaseIntegral *= fator;
                }

                ocorrencia.Base = Arredondar(valorDaBase);
                ocorrencia.BaseIntegral = Arredondar(valorDaBaseIntegral);
                CalcularValorDevidoDaOcorrencia(ocorrencia);
                ocorrencia.IndiceAcumulado = _contexto.ObterIndiceAcumulado(ocorrencia.DataInicial);
            }
            verba.Liquidada = true;
        });
    }

    /// <summary>
    /// Fator do abono: prazo ÷ (prazo − dias de abono) das férias cujo período aquisitivo
    /// coincide com o da ocorrência; 1,5 quando não há férias correspondentes.
    /// </summary>
    private decimal CalcularFatorAbono(OcorrenciaDaVerba ocorrencia)
    {
        if (ocorrencia.PeriodoAquisitivo is { } aquisitivo)
        {
            foreach (var ferias in _contexto.ListaDeFerias)
            {
                if (ferias.PeriodoAquisitivo.CoincideCom(aquisitivo))
                    return ferias.FatorAbono;
            }
        }
        return 1.5m;
    }

    private decimal? ObterValorDaBase(VerbaEmCalculo verba, OcorrenciaDaVerba ocorrencia, out decimal? valorIntegral)
    {
        var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
        valorIntegral = null;
        switch (verba.Tipo)
        {
            case TipoDaVerbaEnum.Informada:
                return null;

            case TipoDaVerbaEnum.Reflexo:
            {
                var integral = new AcumuladorDeIntegral();
                var valor = ResolverBaseVerba(verba, ocorrencia, integral);
                valorIntegral = integral.Valor;
                return valor;
            }

            default: // Calculada: base tabelada + base em outras verbas
            {
                var integral = new AcumuladorDeIntegral();
                decimal? baseTabelada = null;
                if (verba.BaseTabelada is not null)
                    baseTabelada = ResolverBaseTabelada(verba, verba.BaseTabelada, periodo,
                        ocorrencia.PeriodoAquisitivo, ocorrencia.FeriasIndenizadas,
                        fasePago: false, integral, descontarAbonoNosDias: true);
                var baseVerba = verba.BasesVerba.Count > 0
                    ? ResolverBaseVerba(verba, ocorrencia, integral)
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
    internal sealed class AcumuladorDeIntegral
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

    private decimal? ResolverDivisor(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
        if (verba.Tipo == TipoDaVerbaEnum.Informada)
            return null;
        return verba.Divisor.Tipo switch
        {
            DivisorDeVerbaEnum.OutroValor => verba.Divisor.OutroValor,
            DivisorDeVerbaEnum.CargaHoraria => _contexto.ObterValorCargaHoraria(periodo),
            DivisorDeVerbaEnum.DiasUteis => CalendarioTrabalhista.TotalDeDiasUteis(
                periodo, _contexto.SabadoUtilComExcecoes, _contexto.EhFeriadoNaData),
            DivisorDeVerbaEnum.ImportadaDoCartao =>
                verba.CartoesDoDivisor.Sum(c => c.ValorNoMes(periodo.Inicio)),
            _ => throw new ArgumentOutOfRangeException(nameof(verba)),
        };
    }

    private decimal? ResolverQuantidade(
        VerbaEmCalculo verba, PeriodoDeApuracao periodo, PeriodoDeApuracao? periodoAquisitivo,
        out decimal? valorIntegral)
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
                return ContarAvos(verba, periodo, periodoAquisitivo);

            case TipoDeQuantidadeEnum.Apurada:
                return _contexto.QuantidadeDeDiasDoAvisoPrevio();

            case TipoDeQuantidadeEnum.ImportadaDoCalendario:
                return QuantidadeDoCalendario(verba, periodo);

            case TipoDeQuantidadeEnum.ImportadaDoCartao:
                return verba.CartoesDaQuantidade.Sum(c => c.ValorNoMes(periodo.Inicio));

            default:
                throw new ArgumentOutOfRangeException(nameof(verba));
        }
    }

    /// <summary>
    /// Métrica do calendário no período (repousos/dias úteis/feriados/repousos+feriados),
    /// descontando a mesma métrica nas interseções de faltas e férias gozadas conforme os
    /// flags de exclusão da verba. Sem piso — pode ficar negativa, como no motor.
    /// </summary>
    private decimal QuantidadeDoCalendario(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
        var sabado = _contexto.SabadoUtilComExcecoes;
        Func<PeriodoDeApuracao, int> metrica = verba.Quantidade.TipoImportadaDoCalendario switch
        {
            TipoDeQuantidadeImportadaDoCalendarioEnum.Repousos =>
                p => CalendarioTrabalhista.TotalDeRepousos(p, sabado),
            TipoDeQuantidadeImportadaDoCalendarioEnum.DiasUteis =>
                p => CalendarioTrabalhista.TotalDeDiasUteis(p, sabado, _contexto.EhFeriadoNaData),
            TipoDeQuantidadeImportadaDoCalendarioEnum.Feriados =>
                p => CalendarioTrabalhista.TotalDeFeriados(p, _contexto.EhFeriadoNaData),
            _ => p => CalendarioTrabalhista.TotalDeRepousosEFeriados(p, sabado, _contexto.EhFeriadoNaData),
        };

        var quantidade = metrica(periodo);
        if (verba.ExcluirFaltaJustificada)
            quantidade -= _contexto.ObterPeriodosDeFaltasJustificadas(periodo).Sum(metrica);
        if (verba.ExcluirFaltaNaoJustificada)
            quantidade -= _contexto.ObterPeriodosDeFaltasNaoJustificadas(periodo).Sum(metrica);
        if (verba.ExcluirFeriasGozadas)
            quantidade -= _contexto.ObterPeriodosDeFeriasGozadas(periodo).Sum(metrica);
        return quantidade;
    }

    private decimal ContarAvos(VerbaEmCalculo verba, PeriodoDeApuracao periodo, PeriodoDeApuracao? periodoAquisitivo)
    {
        return verba.OcorrenciaDePagamento switch
        {
            OcorrenciaDePagamentoEnum.Dezembro => ContarAvosDeDezembro(verba, periodo),
            OcorrenciaDePagamentoEnum.PeriodoAquisitivo when periodoAquisitivo is { } aquisitivo =>
                ContarAvosDoPeriodoAquisitivo(verba, aquisitivo),
            _ => 0m,
        };
    }

    /// <summary>
    /// Avos do 13º: um por mês com pelo menos 15 dias (descontadas faltas não justificadas)
    /// na janela do ano da ocorrência, limitada pela admissão e pela demissão projetada
    /// com o aviso indenizado. Quando a demissão em dezembro (após o dia 20) projeta o
    /// contrato para o ano seguinte, a ocorrência do dia da demissão conta os avos do
    /// período projetado (janela reiniciada em 1º de janeiro seguinte).
    /// </summary>
    private decimal ContarAvosDeDezembro(VerbaEmCalculo verba, PeriodoDeApuracao periodo)
    {
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
            var demissaoProjetada = _contexto.DataDemissaoProjetada!.Value;
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
            var dias = mes.Fim.Day - mes.Inicio.Day + 1 - _contexto.ObterFaltasNaoJustificadas(mes);
            if (dias >= MinimoDeDiasParaUmAvo)
                avos++;
        }
        return avos;
    }

    /// <summary>
    /// Avos do período aquisitivo (férias proporcionais): um por mês-aniversário completo
    /// dentro do PA; o resto conta se tiver pelo menos 15 dias. Faltas não reduzem aqui.
    /// </summary>
    private decimal ContarAvosDoPeriodoAquisitivo(VerbaEmCalculo verba, PeriodoDeApuracao aquisitivo)
    {
        var dataDeCorte = verba.PeriodoInicial.AddYears(-1);
        var avos = 0;
        var mesesCompletos = 1;
        var fimDoAvo = aquisitivo.Inicio.AddMonths(1).AddDays(-1);
        while (aquisitivo.Fim > fimDoAvo)
        {
            if (!_contexto.LimitarAvosAoPeriodoDoCalculo || dataDeCorte <= fimDoAvo)
                avos++;
            fimDoAvo = aquisitivo.Inicio.AddMonths(++mesesCompletos).AddDays(-1);
        }

        var inicioDoUltimoAvo = aquisitivo.Inicio.AddMonths(mesesCompletos - 1);
        var diasDoResto = aquisitivo.Fim.DayNumber - inicioDoUltimoAvo.DayNumber + 1;
        var restoConta = _contexto.LimitarAvosAoPeriodoDoCalculo
            ? dataDeCorte <= aquisitivo.Fim && diasDoResto >= MinimoDeDiasParaUmAvo
            : diasDoResto >= MinimoDeDiasParaUmAvo;
        if (restoConta)
            avos++;
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

    private decimal? ResolverValorPago(
        VerbaEmCalculo verba, PeriodoDeApuracao periodo, PeriodoDeApuracao? periodoAquisitivo,
        out decimal? valorIntegral)
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

        // Na geração, o parâmetro do motor original ainda não carrega o flag de férias
        // indenizadas — o pago calculado de férias usa "não indenizadas" aqui.
        var integral = new AcumuladorDeIntegral();
        var valor = ResolverBaseTabelada(verba, pago.BaseTabelada, periodo, periodoAquisitivo,
            feriasIndenizadas: false, fasePago: true, integral, descontarAbonoNosDias: false);
        if (valor is null)
            return null;

        valor *= pago.Quantidade;
        valorIntegral = integral.Valor is { } vi ? vi * pago.Quantidade : null;
        return valor;
    }

    private decimal? ResolverBaseTabelada(
        VerbaEmCalculo verba, TermoBaseTabelada termo, PeriodoDeApuracao periodo,
        PeriodoDeApuracao? periodoAquisitivo, bool feriasIndenizadas, bool fasePago,
        AcumuladorDeIntegral integral, bool descontarAbonoNosDias)
    {
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
        if (valor is not { } valorBase)
            return null;

        var ehRemuneracaoDeReferencia = termo.Tipo is BaseDeCalculoDoPrincipalEnum.UltimaRemuneracao
            or BaseDeCalculoDoPrincipalEnum.MaiorRemuneracao;

        if (verba.Caracteristica == CaracteristicaDaVerbaEnum.Ferias &&
            _contexto.RegimeDoContrato != RegimeDoContratoEnum.Intermitente)
        {
            return AplicarDesvioDeFerias(valorBase, periodo, periodoAquisitivo, feriasIndenizadas,
                ehRemuneracaoDeReferencia, descontarAbonoNosDias);
        }

        if (ehRemuneracaoDeReferencia && termo.AplicarProporcionalidade)
        {
            integral.Acumular(valorBase);
            return Proporcionalizacao.Proporcionalizar(
                periodo.Inicio, periodo.Fim, valorBase, DiasParaExcluir(verba, periodo));
        }
        return valorBase;
    }

    /// <summary>
    /// Desvio de férias da base tabelada: na ocorrência de 1 dia da demissão multiplica
    /// pelos dias devidos (prazo − gozos, menos o abono quando a base o desconta) ou pelo
    /// prazo proporcional do art. 130; a remuneração de referência é dividida por 30.
    /// No gozo normal, base = valor ÷ 30 × dias da ocorrência.
    /// </summary>
    private decimal AplicarDesvioDeFerias(
        decimal valor, PeriodoDeApuracao periodo, PeriodoDeApuracao? periodoAquisitivo,
        bool feriasIndenizadas, bool ehRemuneracaoDeReferencia, bool descontarAbonoNosDias)
    {
        var ocorrenciaDeUmDiaNaDemissao = periodo.Inicio == periodo.Fim &&
            _contexto.DataDemissao is { } demissao && periodo.Inicio == demissao;

        if (!ocorrenciaDeUmDiaNaDemissao)
            return ehRemuneracaoDeReferencia ? valor / 30m * periodo.TotalDeDias : valor;

        var ferias = periodoAquisitivo is { } aquisitivo
            ? _contexto.ListaDeFerias.FirstOrDefault(f => f.PeriodoAquisitivo == aquisitivo)
            : null;
        if (ferias is not null)
        {
            if (feriasIndenizadas)
            {
                var dias = ferias.Prazo - ferias.PeriodosDeGozo.Sum(g => g.TotalDeDias);
                if (descontarAbonoNosDias && ferias.Abono)
                    dias -= ferias.QuantidadeDiasAbono;
                valor *= dias;
            }
            if (ehRemuneracaoDeReferencia)
                valor /= 30m;
            return valor;
        }

        valor *= _contexto.PrazoDasFeriasProporcionais(periodoAquisitivo ?? periodo);
        if (ehRemuneracaoDeReferencia)
            valor /= 30m;
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

    private decimal ResolverBaseVerba(VerbaEmCalculo verba, OcorrenciaDaVerba ocorrencia, AcumuladorDeIntegral integral)
    {
        var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
        var valor = 0m;
        decimal? valorIntegral = verba.BasesVerba.Count > 0 ? 0m : null;
        foreach (var item in verba.BasesVerba)
        {
            if (!item.Verba.Liquidada)
                Liquidar(item.Verba);

            decimal valorDaBase, valorDaBaseIntegral;
            if (verba.Tipo == TipoDaVerbaEnum.Reflexo)
            {
                valorDaBase = valorDaBaseIntegral = ComportamentoDoReflexo.Resolver(this, _contexto, verba, item, ocorrencia);
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
            throw new NotSupportedException("Base em verba para férias principal calculada (etapa futura).");

        var origem = item.Verba;
        var valorDaBase = 0m;
        var valorDaBaseIntegral = 0m;
        var diasDaOcorrencia = periodo.TotalDeDias;

        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(periodo.Inicio, periodo.Fim))
        {
            var (valorDoPeriodo, valorDoPeriodoIntegral, diasCobertos, diasParaExcluir) =
                SomarOcorrenciasDoMes(_contexto, origem, mes.Fim, origem.GerarPrincipal);

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
        SomarOcorrenciasDoMes(ContextoDeVerbas contexto, VerbaEmCalculo origem, DateOnly mes, TipoDeGeracaoEnum geracao)
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
            diasParaExcluir += DiasParaExcluirDaOrigem(contexto, origem, ocorrencia);
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

    /// <summary>
    /// Dias a excluir de uma ocorrência conforme os flags da verba de ORIGEM
    /// (a regra do 31 é incondicional no motor: mês de 31 dias coberto conta 1 exclusão).
    /// </summary>
    internal static int DiasParaExcluirDaOrigem(ContextoDeVerbas contexto, VerbaEmCalculo origem, OcorrenciaDaVerba ocorrencia)
    {
        var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
        var exclusoes = 0;
        if (origem.ExcluirFeriasGozadas)
            exclusoes += contexto.ObterDiasFerias(periodo);
        exclusoes = Proporcionalizacao.AjustarExclusoesParaMesDe31(periodo.TotalDeDias, exclusoes);
        if (origem.ExcluirFaltaJustificada)
            exclusoes += contexto.ObterFaltasJustificadas(periodo);
        if (origem.ExcluirFaltaNaoJustificada)
            exclusoes += contexto.ObterFaltasNaoJustificadas(periodo);
        return exclusoes;
    }

    // ------------------------------------------------------------------
    // Apoio à média pela quantidade
    // ------------------------------------------------------------------

    /// <summary>
    /// Base unitária da origem num mês cheio (sem multiplicador/quantidade/divisor):
    /// base tabelada mais a média ponderada das bases em outras verbas — a
    /// reconstrução usada pela média pela quantidade.
    /// </summary>
    internal decimal ResolverBaseSimuladaDaOrigem(VerbaEmCalculo origem, PeriodoDeApuracao mesCheio)
    {
        if (origem.Tipo == TipoDaVerbaEnum.Informada)
            return 0m;

        var integral = new AcumuladorDeIntegral();
        var total = 0m;
        if (origem.Tipo == TipoDaVerbaEnum.Calculada && origem.BaseTabelada is not null)
        {
            total += ResolverBaseTabelada(origem, origem.BaseTabelada, mesCheio,
                periodoAquisitivo: null, feriasIndenizadas: false, fasePago: false,
                integral, descontarAbonoNosDias: true) ?? 0m;
        }
        foreach (var item in origem.BasesVerba)
        {
            if (item.Verba.ParcelaVariavel)
                throw new NotSupportedException(
                    "Base simulada sobre parcela variável (média por janela) — etapa futura.");
            if (!item.Verba.Liquidada)
                Liquidar(item.Verba);
            total += MediaPonderadaPorDias(origem, item, mesCheio, out _);
        }
        return total;
    }

    /// <summary>Resolve o termo divisor da fórmula da ORIGEM num período (fallback da média MQ).</summary>
    internal decimal? ResolverDivisorDaOrigem(VerbaEmCalculo origem, PeriodoDeApuracao periodo) =>
        ResolverDivisor(origem, periodo);

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
            exclusoes += _contexto.ObterDiasFerias(periodo);
        exclusoes = Proporcionalizacao.AjustarExclusoesParaMesDe31(periodo.TotalDeDias, exclusoes);
        if (verba.ExcluirFaltaJustificada)
            exclusoes += _contexto.ObterFaltasJustificadas(periodo);
        if (verba.ExcluirFaltaNaoJustificada)
            exclusoes += _contexto.ObterFaltasNaoJustificadas(periodo);
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
