using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PJeCalc.Core.Models.Indices;
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

void Importar<T>(string arquivo, Func<T> nova, Action<T, DateTime, decimal> preencher) where T : class
{
    var caminho = Path.Combine(dirCsv, arquivo);
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

Console.WriteLine($"Concluído: {total} índices em {saida}");
return 0;
