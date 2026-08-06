using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Models.Indices;
using PJeCalc.Core.Models.Juros;
using PJeCalc.Core.Models.Referencia;
using PJeCalc.Data.Context;

// Pré-constrói o banco de referência (referencia.sqlite) com as séries mensais de
// índices extraídas do banco oficial do PJe-Calc. Reproduzível: rode novamente quando
// o TRT publicar novos índices (atualize antes os CSVs de entrada).
//
// Uso: dotnet run --project PJeCalc.Tools.RefData -- <dirCsv> <caminhoSaida.sqlite>

if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: RefData <dirCsv> <caminhoSaida.sqlite>");
    return 1;
}

var dirCsv = args[0];
var saida = Path.GetFullPath(args[1]);

var options = new DbContextOptionsBuilder<ReferenciaDbContext>()
    .UseSqlite($"Data Source={saida}")
    .Options;

Directory.CreateDirectory(Path.GetDirectoryName(saida)!);

using var db = new ReferenciaDbContext(options);
db.Database.EnsureDeleted();
db.Database.EnsureCreated();

var total = 0;

// Aceita os CSVs soltos no diretório ou organizados nas subpastas das fixtures.
string Achar(string arquivo)
{
    foreach (var subpasta in new[] { "", "Indices", "Juros", "Feriados", "Custas", "SalarioFamilia" })
    {
        var candidato = Path.Combine(dirCsv, subpasta, arquivo);
        if (File.Exists(candidato))
            return candidato;
    }
    throw new FileNotFoundException($"CSV não encontrado: {arquivo} (procurado em {dirCsv} e subpastas).");
}

void Importar<T>(string arquivo, Func<T> nova, Action<T, DateTime, decimal> preencher) where T : class
{
    var caminho = Achar(arquivo);
    var itens = new List<T>();
    foreach (var linha in File.ReadLines(caminho).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(linha)) continue;
        var p = linha.Split(',');
        var comp = DateTime.ParseExact(Limpar(p[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var taxa = decimal.Parse(Limpar(p[1]), NumberStyles.Float, CultureInfo.InvariantCulture);
        var item = nova();
        preencher(item, comp, taxa);
        itens.Add(item);
    }
    db.AddRange(itens);
    db.SaveChanges();
    total += itens.Count;
    Console.WriteLine($"  {arquivo,-20} {itens.Count,6} linhas");
}

// Faixas de juros: "DATAINICIO","DATAFIM","TAXA","TIPOJUROS","TIPOQUANTIDADE".
void ImportarJurosPadrao(string arquivo)
{
    var caminho = Achar(arquivo);
    var itens = new List<JurosPadrao>();
    foreach (var linha in File.ReadLines(caminho).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(linha)) continue;
        var p = linha.Split(',');
        itens.Add(new JurosPadrao
        {
            DataInicio = DateTime.ParseExact(Limpar(p[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DataFim = string.IsNullOrWhiteSpace(Limpar(p[1]))
                ? null
                : DateTime.ParseExact(Limpar(p[1]), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Aliquota = decimal.Parse(Limpar(p[2]), NumberStyles.Float, CultureInfo.InvariantCulture),
            TipoDeJuros = Limpar(p[3]) == "C" ? TipoDeJurosEnum.Compostos : TipoDeJurosEnum.Simples,
            TipoDeQuantidade = Limpar(p[4]) == "I"
                ? TipoDeQuantidadeDeJurosBaseEnum.Inteiro
                : TipoDeQuantidadeDeJurosBaseEnum.Fracao,
        });
    }
    db.AddRange(itens);
    db.SaveChanges();
    total += itens.Count;
    Console.WriteLine($"  {arquivo,-20} {itens.Count,6} faixas");
}

// Feriados: TBFERIADO ("IIDFERIADO","STPFERIADO","STPABRANGENCIA","SSGESTADO",
// "ICDMUNICIPIO","SNMFERIADO","DDTFERIADO","DDTINICIOVIGENCIA","DDTFIMVIGENCIA",
// "SFLFERIADOMOVEL") + TBEXCECAOFERIADO ("IIDFERIADO","DDTEXCECAOFERIADO").
void ImportarFeriados(string arquivoFeriados, string arquivoExcecoes)
{
    var excecoesPorFeriado = File.ReadLines(Achar(arquivoExcecoes)).Skip(1)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l.Split(','))
        .ToLookup(p => long.Parse(Limpar(p[0])), p => DateTime.ParseExact(Limpar(p[1]), "yyyy-MM-dd", CultureInfo.InvariantCulture));

    var feriados = new List<FeriadoReferencia>();
    foreach (var linha in File.ReadLines(Achar(arquivoFeriados)).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(linha)) continue;
        var p = linha.Split(',');
        var id = long.Parse(Limpar(p[0]));
        feriados.Add(new FeriadoReferencia
        {
            Id = id,
            Tipo = Limpar(p[1]) switch
            {
                "F" => TipoDeFeriadoEnum.Feriado,
                "P" => TipoDeFeriadoEnum.PontoFacultativo,
                _ => TipoDeFeriadoEnum.Bancario,
            },
            Abrangencia = Limpar(p[2]) switch
            {
                "F" => AbrangenciaDoFeriadoEnum.Federal,
                "E" => AbrangenciaDoFeriadoEnum.Estadual,
                _ => AbrangenciaDoFeriadoEnum.Municipal,
            },
            Estado = Limpar(p[3]) is { Length: > 0 } uf ? uf : null,
            Municipio = Limpar(p[4]) is { Length: > 0 } municipio ? long.Parse(municipio) : null,
            Nome = Limpar(p[5]),
            Data = Limpar(p[6]) is { Length: > 0 } data
                ? DateTime.ParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null,
            InicioVigencia = DateTime.ParseExact(Limpar(p[7]), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            FimVigencia = Limpar(p[8]) is { Length: > 0 } fim
                ? DateTime.ParseExact(fim, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null,
            Movel = Limpar(p[9]) == "S",
            Excecoes = excecoesPorFeriado[id].Select(d => new ExcecaoDeFeriadoReferencia { Data = d }).ToList(),
        });
    }
    db.AddRange(feriados);
    db.SaveChanges();
    total += feriados.Count;
    Console.WriteLine($"  {arquivoFeriados,-20} {feriados.Count,6} feriados ({feriados.Sum(f => f.Excecoes.Count)} datas de móveis)");
}

// Parâmetros das custas: inicioVigencia,fimVigencia,pisoConhecimento,tetoLiquidacao,
// tetoAutos + os 9 valores fixos por tipo de ato.
void ImportarParametrosDeCustas(string arquivo)
{
    var itens = new List<ParametroDeCustasReferencia>();
    foreach (var linha in File.ReadLines(Achar(arquivo)).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(linha)) continue;
        var p = linha.Split(',');
        decimal V(int i) => decimal.Parse(Limpar(p[i]), NumberStyles.Float, CultureInfo.InvariantCulture);
        itens.Add(new ParametroDeCustasReferencia
        {
            InicioVigencia = DateTime.ParseExact(Limpar(p[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            FimVigencia = Limpar(p[1]) is { Length: > 0 } fim
                ? DateTime.ParseExact(fim, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null,
            PisoConhecimento = V(2),
            TetoLiquidacao = V(3),
            TetoAutos = V(4),
            AtosUrbanos = V(5),
            AtosRurais = V(6),
            AgravoInstrumento = V(7),
            AgravoPeticao = V(8),
            ImpugnacaoSentenca = V(9),
            EmbargosArrematacao = V(10),
            EmbargosExecucao = V(11),
            EmbargosTerceiros = V(12),
            RecursoRevista = V(13),
        });
    }
    db.AddRange(itens);
    db.SaveChanges();
    total += itens.Count;
    Console.WriteLine($"  {arquivo,-20} {itens.Count,6} parâmetros de custas");
}

// Tabela do salário-família: competencia,ini1,fim1,cota1,ini2,fim2,cota2 (ini* não usados).
void ImportarSalarioFamilia(string arquivo)
{
    var itens = new List<SalarioFamiliaReferencia>();
    foreach (var linha in File.ReadLines(Achar(arquivo)).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(linha)) continue;
        var p = linha.Split(',');
        decimal? Opcional(int i) => string.IsNullOrWhiteSpace(Limpar(p[i]))
            ? null
            : decimal.Parse(Limpar(p[i]), NumberStyles.Float, CultureInfo.InvariantCulture);
        decimal Valor(int i) => Opcional(i) ?? 0m;
        itens.Add(new SalarioFamiliaReferencia
        {
            Competencia = DateTime.ParseExact(Limpar(p[0]), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            FinalFaixa1 = Opcional(2),
            CotaFaixa1 = Valor(3),
            FinalFaixa2 = Opcional(5),
            CotaFaixa2 = Valor(6),
        });
    }
    db.AddRange(itens);
    db.SaveChanges();
    total += itens.Count;
    Console.WriteLine($"  {arquivo,-20} {itens.Count,6} competências de salário-família");
}

static string Limpar(string campo) => campo.Trim().Trim('"');

Console.WriteLine($"Construindo {saida}");
Importar("igpm.csv",         () => new IndiceIGPM(),        (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("inpc.csv",         () => new IndiceINPC(),        (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("ipc.csv",          () => new IndiceIPC(),         (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("ipca.csv",         () => new IndiceIPCA(),        (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("ipcae.csv",        () => new IndiceIPCAE(),       (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("ipcaetr.csv",      () => new IndiceIPCAETR(),     (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("tr.csv",           () => new IndiceTR(),          (e, c, t) => { e.Competencia = c; e.Taxa = t; });
Importar("selicfazenda.csv", () => new IndiceSelicFazenda(),(e, c, t) => { e.Competencia = c; e.Taxa = t; });
ImportarJurosPadrao("jurospadrao.csv");
ImportarFeriados("feriado.csv", "feriado_excecao.csv");
ImportarParametrosDeCustas("parametro_custas.csv");
ImportarSalarioFamilia("salario_familia.csv");

Console.WriteLine($"Concluído: {total} registros em {saida}");
return 0;
