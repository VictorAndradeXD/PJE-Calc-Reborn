using PJeCalc.Core.Enums;
using PJeCalc.Core.Services.CorrecaoMonetaria;

namespace PJeCalc.Core.Services.Verbas;

/// <summary>
/// Como a base de uma verba-reflexo é derivada das ocorrências da origem:
/// <list type="bullet">
/// <item><b>Valor mensal</b> — média, ponderada pelos dias de cada mês, das somas mensais
/// do devido/diferença da origem no período da ocorrência do reflexo.</item>
/// <item><b>Média pelo valor</b> — média das competências de uma janela (ano civil,
/// 12 meses anteriores, últimos 12 do contrato, período aquisitivo), dividida pela
/// quantidade <i>esperada</i> de competências (meses sem valor diluem a média), com
/// 4 tratamentos de fração de mês.</item>
/// <item><b>Média pelo valor corrigido</b> — idem, mas cada competência entra corrigida
/// e já arredondada a 2 casas.</item>
/// </list>
/// Reflexos com característica de FÉRIAS dividem por 30 e, na ocorrência de 1 dia da
/// demissão, multiplicam pelos dias devidos das férias (ou pelo prazo proporcional).
/// </summary>
internal static class ComportamentoDoReflexo
{
    public static decimal Resolver(
        MotorDeVerbas motor, ContextoDeVerbas contexto, VerbaEmCalculo reflexo, ItemBaseVerba item,
        OcorrenciaDaVerba ocorrencia)
    {
        var periodo = new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal);
        return reflexo.ComportamentoDoReflexo switch
        {
            ComportamentoDoReflexoEnum.ValorMensal => ValorMensal(contexto, reflexo, item, ocorrencia, periodo),
            ComportamentoDoReflexoEnum.MediaPeloValor =>
                MediaPeloValor(contexto, reflexo, item, ocorrencia, periodo, corrigir: false),
            ComportamentoDoReflexoEnum.MediaPeloValorCorrigido =>
                MediaPeloValor(contexto, reflexo, item, ocorrencia, periodo, corrigir: true),
            ComportamentoDoReflexoEnum.MediaPelaQuantidade =>
                MediaPelaQuantidade(motor, contexto, reflexo, item, ocorrencia, periodo),
            _ => throw new ArgumentOutOfRangeException(nameof(reflexo)),
        };
    }

    /// <summary>
    /// Média pela quantidade: média mensal das quantidades da origem (quantidade ×
    /// multiplicador, ×2 na dobra; sobre a DIFERENÇA, cada mês entra na proporção não
    /// paga — e meses de devido zero com pagamento viram abatimento), aplicada à base
    /// unitária da origem reconstruída no período do reflexo e dividida pelo divisor
    /// efetivo (o da ocorrência ativa mais recente quando a parcela é fixa; a média dos
    /// divisores da janela quando variável).
    /// </summary>
    private static decimal MediaPelaQuantidade(
        MotorDeVerbas motor, ContextoDeVerbas contexto, VerbaEmCalculo reflexo, ItemBaseVerba item,
        OcorrenciaDaVerba ocorrencia, PeriodoDeApuracao periodo)
    {
        var origem = item.Verba;
        var quantidadeEsperada = QuantidadeEsperadaDeCompetencias(contexto, reflexo, ocorrencia, periodo);
        if (quantidadeEsperada <= 0)
            return 0m;
        var grupos = AgruparPorCompetencia(OcorrenciasDaJanela(contexto, reflexo, origem, ocorrencia, periodo));
        if (grupos.Count == 0)
            return 0m;

        var mediaQuantidade = 0m;
        var abatimento = 0m;

        foreach (var (competencia, ocorrencias) in grupos)
        {
            var quantidadeDaCompetencia = 0m;
            decimal? quantidadeIntegralDaCompetencia = null;
            var abatimentoDaCompetencia = 0m;
            decimal? abatimentoIntegralDaCompetencia = null;
            var diasCobertos = new HashSet<DateOnly>();
            var diasParaExcluir = 0;

            foreach (var o in ocorrencias)
            {
                if (!o.Ativo)
                    continue;
                for (var d = o.DataInicial; d <= o.DataFinal; d = d.AddDays(1))
                    diasCobertos.Add(d);
                diasParaExcluir += MotorDeVerbas.DiasParaExcluirDaOrigem(contexto, origem, o);

                var multiplicador = o.Multiplicador ?? 1m;
                var quantidade = (o.Quantidade ?? 0m) * multiplicador;
                var quantidadeIntegral = o.QuantidadeIntegral is { } qi ? qi * multiplicador : (decimal?)null;
                if (o.Dobra)
                {
                    quantidade *= 2m;
                    quantidadeIntegral *= 2m;
                }

                if (origem.GerarReflexo == TipoDeGeracaoEnum.Diferenca)
                {
                    if ((o.Devido ?? 0m) == 0m)
                        abatimentoDaCompetencia += o.Diferenca;
                    else
                        quantidade = quantidade * o.Diferenca / o.Devido!.Value;

                    if ((o.DevidoIntegral ?? 0m) == 0m)
                        abatimentoIntegralDaCompetencia ??= o.DiferencaIntegral;
                    else if (quantidadeIntegral is not null)
                        quantidadeIntegral = quantidadeIntegral * o.DiferencaIntegral / o.DevidoIntegral!.Value;
                }

                quantidadeDaCompetencia += quantidade;
                quantidadeIntegralDaCompetencia ??= quantidadeIntegral;
            }
            if (diasCobertos.Count == 0)
                continue;

            var diasLiquidos = diasCobertos.Count - diasParaExcluir;
            switch (reflexo.TratamentoDaFracaoDeMes)
            {
                case TratamentoDaFracaoDeMesDoReflexoEnum.Integralizar when diasLiquidos > 0:
                    quantidadeDaCompetencia = quantidadeIntegralDaCompetencia
                        ?? MotorDeVerbas.IntegralizarPeriodoCondensado(
                            competencia, quantidadeDaCompetencia, diasCobertos.Count, diasParaExcluir);
                    abatimentoDaCompetencia = abatimentoIntegralDaCompetencia
                        ?? MotorDeVerbas.IntegralizarPeriodoCondensado(
                            competencia, abatimentoDaCompetencia, diasCobertos.Count, diasParaExcluir);
                    break;

                // Aqui o motor normaliza o mês de 31 dias para 30 antes de comparar
                // (diferente da média pelo valor, que compara com os dias reais do mês).
                case TratamentoDaFracaoDeMesDoReflexoEnum.Desprezar
                    when diasLiquidos < Math.Min(DateTime.DaysInMonth(competencia.Year, competencia.Month), 30):
                case TratamentoDaFracaoDeMesDoReflexoEnum.DesprezarMenorQue15Dias when diasLiquidos < 15:
                    quantidadeEsperada--;
                    continue;
            }

            mediaQuantidade += quantidadeDaCompetencia;
            abatimento += abatimentoDaCompetencia;
        }

        if (quantidadeEsperada <= 0 || mediaQuantidade <= 0m)
            return 0m;
        mediaQuantidade /= quantidadeEsperada;
        abatimento /= quantidadeEsperada;

        // Base unitária da origem reconstruída mês a mês no período da ocorrência do
        // reflexo (mês cheio), ponderada pelos dias de cada fração; idem para a média
        // dos divisores das ocorrências da janela.
        var janelaDoDivisor = JanelaParaMediaDoDivisor(contexto, reflexo, ocorrencia, grupos.Keys);
        var basePonderada = 0m;
        var divisorPonderado = 0m;
        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(periodo.Inicio, periodo.Fim))
        {
            var mesCheio = new PeriodoDeApuracao(
                PeriodoDeApuracao.Competencia(mes.Fim), PeriodoDeApuracao.UltimoDiaDoMes(mes.Fim));
            basePonderada += motor.ResolverBaseSimuladaDaOrigem(origem, mesCheio) * mes.TotalDeDias;

            var mediaDivisor = ObterMediaDoDivisor(origem, janelaDoDivisor);
            if (mediaDivisor == 0m && ocorrencia.PeriodoAquisitivo is { } aquisitivo)
                mediaDivisor = ObterMediaDoDivisor(origem, aquisitivo);
            divisorPonderado += mediaDivisor * mes.TotalDeDias;
        }
        basePonderada /= periodo.TotalDeDias;
        divisorPonderado /= periodo.TotalDeDias;
        if (divisorPonderado == 0m)
            divisorPonderado = 1m;

        if (!UtilizarDivisorVariavel(origem))
            divisorPonderado = DivisorDaParcelaFixa(motor, contexto, origem, periodo) ?? divisorPonderado;

        var valor = basePonderada * mediaQuantidade / divisorPonderado + abatimento;

        if (EhDestinoFerias(contexto, reflexo))
        {
            valor = EhOcorrenciaDeUmDiaNaDemissao(contexto, periodo)
                ? MultiplicarPelosDiasDeFerias(contexto, ocorrencia, valor, descontarAbono: true,
                    somenteSeIndenizada: true)
                : valor * periodo.TotalDeDias;
            valor /= 30m;
        }
        return valor;
    }

    /// <summary>
    /// Janela usada na média dos divisores: para o período aquisitivo, os últimos 12
    /// meses anteriores ao mês da ocorrência (recortados pela admissão); nas demais,
    /// o intervalo [min, max] das competências que têm ocorrência na janela da média.
    /// </summary>
    private static PeriodoDeApuracao JanelaParaMediaDoDivisor(
        ContextoDeVerbas contexto, VerbaEmCalculo reflexo, OcorrenciaDaVerba ocorrencia,
        IEnumerable<DateOnly> competenciasDaJanela)
    {
        if (reflexo.PeriodoDaMedia == PeriodoDaMediaDoReflexoEnum.PeriodoAquisitivo)
        {
            var mes = PeriodoDeApuracao.Competencia(ocorrencia.DataInicial);
            var inicio = mes.AddMonths(-12);
            var fim = PeriodoDeApuracao.UltimoDiaDoMes(mes.AddMonths(-1));
            if (contexto.DataAdmissao > fim)
                return new PeriodoDeApuracao(mes, PeriodoDeApuracao.UltimoDiaDoMes(mes));
            if (contexto.DataAdmissao > inicio)
                inicio = contexto.DataAdmissao;
            return new PeriodoDeApuracao(inicio, fim);
        }

        var lista = competenciasDaJanela.ToList();
        return lista.Count == 0
            ? new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal)
            : new PeriodoDeApuracao(lista.Min(), lista.Max());
    }

    /// <summary>
    /// Média dos divisores das ocorrências ativas da origem na janela: meses sem
    /// ocorrência (ou com divisor nulo/zero) saem do denominador — fiel ao motor, que
    /// pode decrementar mais de uma vez no mesmo mês.
    /// </summary>
    private static decimal ObterMediaDoDivisor(VerbaEmCalculo origem, PeriodoDeApuracao janela)
    {
        var soma = 0m;
        var meses = PeriodoDeApuracao.QuebrarEmMeses(janela.Inicio, janela.Fim);
        var totalDeMeses = meses.Count;
        foreach (var mes in meses)
        {
            var doMes = origem.OcorrenciasDoMes(mes.Fim).Where(o => o.Ativo).ToList();
            if (doMes.Count == 0)
            {
                totalDeMeses--;
                continue;
            }
            foreach (var o in doMes)
            {
                if (o.Divisor is not { } divisor || divisor == 0m)
                {
                    totalDeMeses--;
                    continue;
                }
                soma += divisor;
            }
        }
        return totalDeMeses > 0 ? soma / totalDeMeses : soma;
    }

    private static bool UtilizarDivisorVariavel(VerbaEmCalculo origem) =>
        origem.ParcelaVariavel ||
        origem.BasesVerba.Any(i => i.Verba.ParcelaVariavel) ||
        origem.HistoricosDaBase.Any(v => v.ParcelaVariavel);

    /// <summary>
    /// Divisor da origem de parcela fixa: o da ocorrência ativa mais recente do mês
    /// final do reflexo, retrocedendo mês a mês até a admissão; em último caso, o termo
    /// divisor da fórmula da origem resolvido no mês cheio.
    /// </summary>
    private static decimal? DivisorDaParcelaFixa(
        MotorDeVerbas motor, ContextoDeVerbas contexto, VerbaEmCalculo origem, PeriodoDeApuracao periodo)
    {
        var maisRecente = OcorrenciaAtivaMaisRecenteDoMes(origem, periodo.Fim);
        if (maisRecente?.Divisor is { } divisor)
            return divisor;

        var mes = PeriodoDeApuracao.Competencia(periodo.Fim).AddMonths(-1);
        while (contexto.DataAdmissao <= PeriodoDeApuracao.UltimoDiaDoMes(mes))
        {
            var candidato = OcorrenciaAtivaMaisRecenteDoMes(origem, mes);
            if (candidato?.Divisor is { } divisorDoMes)
                return divisorDoMes;
            if (contexto.DataAdmissao > mes)
                break;
            mes = mes.AddMonths(-1);
        }

        var mesCheio = new PeriodoDeApuracao(
            PeriodoDeApuracao.Competencia(periodo.Fim), PeriodoDeApuracao.UltimoDiaDoMes(periodo.Fim));
        return motor.ResolverDivisorDaOrigem(origem, mesCheio);
    }

    private static OcorrenciaDaVerba? OcorrenciaAtivaMaisRecenteDoMes(VerbaEmCalculo origem, DateOnly mes)
    {
        OcorrenciaDaVerba? maisRecente = null;
        foreach (var o in origem.OcorrenciasDoMes(mes))
        {
            if (o.Ativo && (maisRecente is null || o.DataFinal > maisRecente.DataFinal))
                maisRecente = o;
        }
        return maisRecente;
    }

    private static bool EhDestinoFerias(ContextoDeVerbas contexto, VerbaEmCalculo reflexo) =>
        reflexo.Caracteristica == CaracteristicaDaVerbaEnum.Ferias &&
        contexto.RegimeDoContrato != RegimeDoContratoEnum.Intermitente;

    private static decimal ValorMensal(
        ContextoDeVerbas contexto, VerbaEmCalculo reflexo, ItemBaseVerba item,
        OcorrenciaDaVerba ocorrencia, PeriodoDeApuracao periodo)
    {
        var origem = item.Verba;
        var total = 0m;

        foreach (var mes in PeriodoDeApuracao.QuebrarEmMeses(periodo.Inicio, periodo.Fim))
        {
            var (valor, valorIntegral, diasCobertos, diasParaExcluir) =
                MotorDeVerbas.SomarOcorrenciasDoMes(contexto, origem, mes.Fim, origem.GerarReflexo);

            if (diasCobertos == 0)
            {
                valor = 0m;
            }
            else if (reflexo.TratamentoDaFracaoDeMes == TratamentoDaFracaoDeMesDoReflexoEnum.Integralizar)
            {
                valor = valorIntegral ?? IntegralizarFracao(mes.Fim, valor, diasCobertos, diasParaExcluir);
            }

            total += valor * mes.TotalDeDias;
        }

        if (EhDestinoFerias(contexto, reflexo))
        {
            total /= 30m;
            if (EhOcorrenciaDeUmDiaNaDemissao(contexto, periodo))
                total = MultiplicarPelosDiasDeFerias(contexto, ocorrencia, total, descontarAbono: true);
            return total;
        }
        return total / periodo.TotalDeDias;
    }

    private static decimal MediaPeloValor(
        ContextoDeVerbas contexto, VerbaEmCalculo reflexo, ItemBaseVerba item,
        OcorrenciaDaVerba ocorrencia, PeriodoDeApuracao periodo, bool corrigir)
    {
        var origem = item.Verba;
        var quantidadeEsperada = QuantidadeEsperadaDeCompetencias(contexto, reflexo, ocorrencia, periodo);
        var grupos = AgruparPorCompetencia(OcorrenciasDaJanela(contexto, reflexo, origem, ocorrencia, periodo));
        var fatoresDeMoeda = corrigir ? null : FatorDeMoedaPorCompetencia(grupos.Keys);

        var media = 0m;
        DateOnly? ultimaCompetencia = null;

        foreach (var (competencia, ocorrencias) in grupos)
        {
            if (ultimaCompetencia is null || competencia > ultimaCompetencia)
                ultimaCompetencia = competencia;

            var valor = 0m;
            decimal? valorIntegral = null;
            var diasCobertos = new HashSet<DateOnly>();
            var diasParaExcluir = 0;
            foreach (var o in ocorrencias)
            {
                if (!o.Ativo)
                    continue;
                for (var d = o.DataInicial; d <= o.DataFinal; d = d.AddDays(1))
                    diasCobertos.Add(d);
                diasParaExcluir += MotorDeVerbas.DiasParaExcluirDaOrigem(contexto, origem, o);
                var (v, vi) = origem.GerarReflexo == TipoDeGeracaoEnum.Devido
                    ? (o.Devido ?? 0m, o.DevidoIntegral)
                    : (o.Diferenca, (decimal?)o.DiferencaIntegral);
                valor += v;
                valorIntegral ??= vi;
            }
            if (diasCobertos.Count == 0)
                continue; // não entra na média, mas também não reduz o divisor

            var diasLiquidos = diasCobertos.Count - diasParaExcluir;
            switch (reflexo.TratamentoDaFracaoDeMes)
            {
                case TratamentoDaFracaoDeMesDoReflexoEnum.Integralizar when diasLiquidos > 0:
                    valor = valorIntegral ?? IntegralizarFracao(competencia, valor, diasCobertos.Count, diasParaExcluir);
                    break;

                case TratamentoDaFracaoDeMesDoReflexoEnum.Desprezar
                    when diasLiquidos < DateTime.DaysInMonth(competencia.Year, competencia.Month):
                case TratamentoDaFracaoDeMesDoReflexoEnum.DesprezarMenorQue15Dias when diasLiquidos < 15:
                    quantidadeEsperada--;
                    continue;
            }

            media += corrigir
                ? AplicacaoDeFator.Aplicar(valor, contexto.ObterIndiceParaMediaCorrigida(competencia))
                : valor / fatoresDeMoeda![competencia];
        }

        if (quantidadeEsperada <= 0)
            return 0m;

        var resultado = media / quantidadeEsperada;

        if (EhDestinoFerias(contexto, reflexo))
        {
            // No dia da demissão multiplica pelos dias devidos (ou prazo proporcional);
            // nas demais ocorrências, pelos dias do período; sempre divide por 30.
            resultado = EhOcorrenciaDeUmDiaNaDemissao(contexto, periodo)
                ? MultiplicarPelosDiasDeFerias(contexto, ocorrencia, resultado, descontarAbono: true,
                    somenteSeIndenizada: true)
                : resultado * periodo.TotalDeDias;
            resultado /= 30m;
        }

        if (!corrigir && ultimaCompetencia is { } ultima &&
            ConversaoDeMoedas.ProdutoDosDivisoresEntre(ultima, periodo.Inicio) is { } fatorFinal && fatorFinal != 0m)
        {
            resultado /= fatorFinal;
        }
        return resultado;
    }

    private static bool EhOcorrenciaDeUmDiaNaDemissao(ContextoDeVerbas contexto, PeriodoDeApuracao periodo) =>
        periodo.Inicio == periodo.Fim && contexto.DataDemissao is { } demissao && periodo.Inicio == demissao;

    /// <summary>
    /// Multiplicador de férias na ocorrência da demissão: dias devidos das férias do
    /// mesmo período aquisitivo (prazo − gozos − abono) ou o prazo proporcional do
    /// art. 130 quando o PA não está cadastrado. No comportamento VALOR_MENSAL a
    /// multiplicação vale para INDENIZADAS/GOZADAS_PARCIALMENTE; nas médias, apenas
    /// quando a ocorrência é de férias indenizadas.
    /// </summary>
    private static decimal MultiplicarPelosDiasDeFerias(
        ContextoDeVerbas contexto, OcorrenciaDaVerba ocorrencia, decimal valor,
        bool descontarAbono, bool somenteSeIndenizada = false)
    {
        var aquisitivo = ocorrencia.PeriodoAquisitivo;
        var ferias = aquisitivo is { } pa
            ? contexto.ListaDeFerias.FirstOrDefault(f => f.PeriodoAquisitivo == pa)
            : null;

        if (ferias is not null)
        {
            var multiplicar = somenteSeIndenizada
                ? ocorrencia.FeriasIndenizadas
                : ferias.Situacao is SituacaoDaFeriasEnum.Indenizadas or SituacaoDaFeriasEnum.GozadasParcialmente;
            if (!multiplicar)
                return valor;
            var dias = ferias.Prazo - ferias.PeriodosDeGozo.Sum(g => g.TotalDeDias);
            if (descontarAbono && ferias.Abono)
                dias -= ferias.QuantidadeDiasAbono;
            return valor * dias;
        }

        return valor * contexto.PrazoDasFeriasProporcionais(
            aquisitivo ?? new PeriodoDeApuracao(ocorrencia.DataInicial, ocorrencia.DataFinal));
    }

    private static decimal IntegralizarFracao(DateOnly competencia, decimal valor, int diasCobertos, int diasParaExcluir)
    {
        if (diasCobertos - Math.Max(diasParaExcluir, 0) <= 0)
            throw new NotSupportedException(
                "Integralização de mês integralmente excluído depende da base simulada (etapa futura).");
        return MotorDeVerbas.IntegralizarPeriodoCondensado(competencia, valor, diasCobertos, diasParaExcluir);
    }

    /// <summary>
    /// Divisor da média: quantidade de competências que a janela DEVERIA ter, limitada
    /// pela admissão (e demissão) — não a quantidade de meses com valor.
    /// </summary>
    private static int QuantidadeEsperadaDeCompetencias(
        ContextoDeVerbas contexto, VerbaEmCalculo reflexo, OcorrenciaDaVerba ocorrencia, PeriodoDeApuracao periodo)
    {
        var competenciaDaAdmissao = PeriodoDeApuracao.Competencia(contexto.DataAdmissao);
        switch (reflexo.PeriodoDaMedia)
        {
            case PeriodoDaMediaDoReflexoEnum.AnoCivil:
            {
                var ano = periodo.Inicio.Year;
                var competenciaDaDemissao = contexto.DataDemissao is { } d ? PeriodoDeApuracao.Competencia(d) : (DateOnly?)null;
                var quantidade = 0;
                for (var mes = 1; mes <= 12; mes++)
                {
                    var competencia = new DateOnly(ano, mes, 1);
                    if (competencia >= competenciaDaAdmissao &&
                        (competenciaDaDemissao is null || competencia <= competenciaDaDemissao))
                        quantidade++;
                }
                return quantidade;
            }

            case PeriodoDaMediaDoReflexoEnum.DozeMesesAnterioresAoVencimentoDaParcela:
            {
                var vencimento = PeriodoDeApuracao.Competencia(periodo.Inicio);
                return Enumerable.Range(1, 12).Count(k => vencimento.AddMonths(-k) >= competenciaDaAdmissao);
            }

            case PeriodoDaMediaDoReflexoEnum.UltimosDozeMesesDoContrato:
            {
                if (contexto.DataDemissao is not { } demissao)
                    return 0;
                var fim = PeriodoDeApuracao.Competencia(demissao);
                return Enumerable.Range(0, 12).Count(k => fim.AddMonths(-k) >= competenciaDaAdmissao);
            }

            default: // PeriodoAquisitivo
            {
                if (JanelaDoPeriodoAquisitivo(contexto, ocorrencia) is not { } janela)
                    return 0;
                var meses = PeriodoDeApuracao.QuebrarEmMeses(janela.Inicio, janela.Fim).Count;
                return meses == 13 ? 12 : meses;
            }
        }
    }

    private static IEnumerable<OcorrenciaDaVerba> OcorrenciasDaJanela(
        ContextoDeVerbas contexto, VerbaEmCalculo reflexo, VerbaEmCalculo origem,
        OcorrenciaDaVerba ocorrencia, PeriodoDeApuracao periodo)
    {
        switch (reflexo.PeriodoDaMedia)
        {
            case PeriodoDaMediaDoReflexoEnum.AnoCivil:
            {
                var ocorrencias = origem.OcorrenciasDoAno(periodo.Fim.Year);
                if (contexto.DataDemissao is { } demissao)
                {
                    var competenciaDaDemissao = PeriodoDeApuracao.Competencia(demissao);
                    ocorrencias = ocorrencias.Where(o =>
                        PeriodoDeApuracao.Competencia(o.DataInicial) <= competenciaDaDemissao);
                }
                return ocorrencias;
            }

            case PeriodoDaMediaDoReflexoEnum.DozeMesesAnterioresAoVencimentoDaParcela:
                return origem.OcorrenciasDosDozeMesesAnteriores(periodo.Inicio);

            case PeriodoDaMediaDoReflexoEnum.UltimosDozeMesesDoContrato:
                return contexto.DataDemissao is { } d
                    ? origem.OcorrenciasDosDozeMesesAnteriores(PeriodoDeApuracao.Competencia(d).AddMonths(1))
                    : [];

            default: // PeriodoAquisitivo
            {
                if (JanelaDoPeriodoAquisitivo(contexto, ocorrencia) is not { } janela)
                    return [];
                var fim = janela.Fim;
                // Janela de 13 meses: descarta o último.
                if (PeriodoDeApuracao.QuebrarEmMeses(janela.Inicio, janela.Fim).Count == 13)
                    fim = fim.AddMonths(-1);
                var inicio = PeriodoDeApuracao.Competencia(janela.Inicio);
                var competenciaFinal = PeriodoDeApuracao.Competencia(fim);
                return origem.Ocorrencias.Where(o =>
                {
                    var competencia = PeriodoDeApuracao.Competencia(o.DataInicial);
                    return competencia >= inicio && competencia <= competenciaFinal;
                });
            }
        }
    }

    /// <summary>
    /// Janela da média por período aquisitivo: o PA da ocorrência, encerrado na demissão
    /// quando ela cai dentro dele (e recuado 1 ano quando o PA começa após a demissão).
    /// </summary>
    private static PeriodoDeApuracao? JanelaDoPeriodoAquisitivo(ContextoDeVerbas contexto, OcorrenciaDaVerba ocorrencia)
    {
        if (ocorrencia.PeriodoAquisitivo is not { } aquisitivo)
            return null;
        var inicio = aquisitivo.Inicio;
        var fim = aquisitivo.Fim;
        if (contexto.DataDemissao is { } demissao && aquisitivo.Fim > demissao)
        {
            fim = demissao;
            if (aquisitivo.Inicio > demissao)
                inicio = aquisitivo.Inicio.AddYears(-1);
        }
        return new PeriodoDeApuracao(inicio, fim);
    }

    /// <summary>Agrupa por competência da data FINAL da ocorrência, preservando a ordem.</summary>
    private static Dictionary<DateOnly, List<OcorrenciaDaVerba>> AgruparPorCompetencia(
        IEnumerable<OcorrenciaDaVerba> ocorrencias)
    {
        var grupos = new Dictionary<DateOnly, List<OcorrenciaDaVerba>>();
        foreach (var ocorrencia in ocorrencias)
        {
            var competencia = PeriodoDeApuracao.Competencia(ocorrencia.DataFinal);
            if (!grupos.TryGetValue(competencia, out var lista))
                grupos[competencia] = lista = [];
            lista.Add(ocorrencia);
        }
        return grupos;
    }

    /// <summary>
    /// Fator de conversão de moeda por competência da janela: competências anteriores ao
    /// último mês de troca de moeda dentro da janela são divididas pelo divisor dessa
    /// troca; as demais valem 1. Na era do Real todos os fatores são 1.
    /// </summary>
    private static Dictionary<DateOnly, decimal> FatorDeMoedaPorCompetencia(IEnumerable<DateOnly> competencias)
    {
        var lista = competencias.ToList();
        var fatores = lista.ToDictionary(c => c, _ => 1m);
        if (lista.Count == 0)
            return fatores;

        var mesDeConversao = ConversaoDeMoedas.UltimaCompetenciaDeConversaoEntre(lista.Min(), lista.Max());
        if (mesDeConversao is { } conversao)
        {
            var divisor = ConversaoDeMoedas.DivisorNaCompetencia(conversao);
            foreach (var competencia in lista.Where(c => c < conversao))
                fatores[competencia] = divisor;
        }
        return fatores;
    }
}
