# Harnesses de golden values (motor Java oficial, headless)

Estes fontes Java dirigem o motor de cálculo **original** do PJe-Calc 2.15.1 sem
Tomcat/Seam, para gerar os valores-verdade (golden) usados nos testes de paridade
em `PJeCalc.Tests/Fixtures/golden_*.csv`.

## Pré-requisitos
- Instalador original em `C:\Users\Judeu\Downloads\pjecalc-windows64-2.15.1\`
  (jars em `tomcat\webapps\pjecalc\WEB-INF\lib\`, JRE 8 em `bin\jre`, banco H2 em
  `.dados\pjecalc.h2.db` — user `pjecalc`, senha `/pjecalc/`).
- javac do sistema (compilar com `--release 8`), rodar na JRE 8 do instalador.

## Como rodar (Git Bash, a partir do diretório do instalador)

```bash
# CRÍTICO no Git Bash: a senha /pjecalc/ é destruída pela conversão de path do MSYS
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*"

CP="tomcat/webapps/pjecalc/WEB-INF/lib/*;tomcat/lib/h2-1.3.154.jar"
javac --release 8 -cp "$CP" -d out CAMINHO/GoldenGenXxx.java
./bin/jre/bin/java -cp "out;$CP" GoldenGenXxx
```

## Harnesses
| Arquivo | Gera golden de | Técnica |
|---|---|---|
| GoldenSmoke.java | sanidade do motor de correção | matemática pura |
| GoldenGen.java | correção monetária (16 casos: mensal, moeda pré-1994, SELIC aditiva, taxa negativa) | JDBC + `CalculadorDeIndices` |
| GoldenGenJuros.java | `PeriodoDeJuros` (meses fração/15-dias, taxa simples/composta/diária) | POJO puro |
| GoldenGenJurosTabela.java | taxa acumulada JurosPadrão por faixas | modo teste (`Utils.iniciarTeste` + stub de repositório JDBC + `Calculo` mínimo) |
| GoldenGenJurosCompleto.java | regimes fixos (1%/0,5%/0,0333%) + projeção da data inicial | modo teste |
| GoldenGenFgts.java | apuração mensal do FGTS | `OcorrenciaDeFgts` construída à mão |
| GoldenGenFgtsMulta.java | totais agregados + multa do FGTS | `Fgts` construído à mão |
| GoldenGenInss.java | alíquota do segurado (2 eras, 64 casos) | `TabelaPrevidenciariaSeguradoEmpregado` via JDBC |
| GoldenGenInssOcorrencia.java | aplicação por ocorrência (4 cotas, juros truncados) | grafo `Ocorrência→Inss→Calculo→Parametros` à mão |
| GoldenGenIrpf.java | tabela progressiva + RRA | `TabelaIrpf`/`OcorrenciaDeIrpf` via JDBC |
| GoldenGenVerbas.java | proporcionalizar/integralizar + fórmula da ocorrência | POJOs puros (setar campos `*Integral` evita lazy-load de repositório) |
| GoldenGenVerbasPipeline.java | pipeline completo: geração MENSAL/DESLIGAMENTO/DEZEMBRO+avos, termos, liquidação, reflexo VALOR_MENSAL e médias MV (17 cenários) | modo teste + stubs por herança: repositórios (`Utils.adicionarRepositorioParaTeste`), `ServicoDeCalculo` (subclasse), máquina injetada via `setMaquinaDeCalculorencias`, tabela de correção via `setTabelaDeCorrecaoMonetariaTrabalhista` (construtor vazio + override de `obterValorAcumuladoDoIndice`) |
| GoldenGenFeriasPipeline.java | férias: tabela do art. 130, breakInYears, salário em férias, pipeline PERIODO_AQUISITIVO (gozos/dobra/saldo/indenizadas/fracionário/abono/prescrição), faltas nos provedores, reflexos com destino férias | mesma receita do VerbasPipeline + `Ferias`/`Falta` à mão; gotcha: `obterDiasFerias` passa por `LazyloadSecure` — envolver `listaDeFerias` em `new PersistentSet(null, set)` quando alguma verba excluir férias gozadas |

## Exportação de dados de referência (CSV)
`export_refdata.sql` (índices/juros/salário mínimo — substituir OUTDIR),
`export_inss.sql`, `export_irpf.sql`. Rodar com:

```bash
java -cp tomcat/lib/h2-1.3.154.jar org.h2.tools.RunScript \
  -url "jdbc:h2:.dados/pjecalc;IFEXISTS=TRUE;ACCESS_MODE_DATA=r" \
  -user pjecalc -password "/pjecalc/" -script arquivo.sql
```

Os CSVs alimentam `PJeCalc.Tools.RefData` (constrói `referencia.sqlite`) e as
fixtures de `PJeCalc.Tests/Fixtures/`.
