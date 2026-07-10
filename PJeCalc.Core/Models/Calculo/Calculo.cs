using PJeCalc.Core.Common;
using PJeCalc.Core.Enums;
using PJeCalc.Core.Models.CartaoDePonto;
using PJeCalc.Core.Models.Custas;
using PJeCalc.Core.Models.Faltas;
using PJeCalc.Core.Models.Ferias;
using PJeCalc.Core.Models.Fgts;
using PJeCalc.Core.Models.HistoricoSalarial;
using PJeCalc.Core.Models.Honorarios;
using PJeCalc.Core.Models.Inss;
using PJeCalc.Core.Models.Irpf;
using PJeCalc.Core.Models.Juros;
using PJeCalc.Core.Models.Multas;
using PJeCalc.Core.Models.Pagamento;
using PJeCalc.Core.Models.PensaoAlimenticia;
using PJeCalc.Core.Models.PrevidenciaPrivada;
using PJeCalc.Core.Models.Processo;
using PJeCalc.Core.Models.SalarioFamilia;
using PJeCalc.Core.Models.SeguroDesemprego;
using PJeCalc.Core.Models.VerbaCalculo;

namespace PJeCalc.Core.Models.Calculo;

public class Calculo : EntityBase
{
    public DateTime? DataAdmissao { get; set; }
    public DateTime? DataDemissao { get; set; }
    public DateTime? DataDeLiquidacao { get; set; }
    public DateTime? DataAjuizamento { get; set; }
    public DateTime? DataInicioCalculo { get; set; }
    public DateTime? DataTerminoCalculo { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? InicioFeriasColetivas { get; set; }

    public decimal? Salario { get; set; }
    public decimal? MaiorRemuneracao { get; set; }
    public decimal? CargaHorariaMensal { get; set; }
    public decimal? DivisorDeHorasExtras { get; set; }

    public TipoCalculoEnum TipoDeCalculo { get; set; }
    public RegimeDoContratoEnum RegimeDoContrato { get; set; }

    public bool IncluirSabado { get; set; }
    public bool ConsiderarPagamento { get; set; }
    public bool PrescricaoQuinquenal { get; set; }
    public bool ProjetaAvisoIndenizado { get; set; }
    public bool ConsideraFeriadoEstadual { get; set; }
    public bool ConsideraFeriadoMunicipal { get; set; }
    public bool PrescricaoFgts { get; set; }
    public bool LimitarAvosAoPeriodoDoCalculo { get; set; }
    public bool ZeraValorNegativo { get; set; }
    public bool Ativo { get; set; } = true;
    public bool Validado { get; set; }

    public long? Versao { get; set; }

    // Navigation: one-to-one
    public Processo.Processo? Processo { get; set; }
    public Fgts.Fgts? Fgts { get; set; }
    public Inss.Inss? Inss { get; set; }
    public Irpf.Irpf? Irpf { get; set; }
    public CustasJudiciais? CustasJudiciais { get; set; }
    public ParametrosDeAtualizacao? ParametrosDeAtualizacao { get; set; }
    public PensaoAlimenticia.PensaoAlimenticia? PensaoAlimenticia { get; set; }
    public PrevidenciaPrivada.PrevidenciaPrivada? PrevidenciaPrivada { get; set; }
    public SalarioFamilia.SalarioFamilia? SalarioFamilia { get; set; }
    public SeguroDesemprego.SeguroDesemprego? SeguroDesemprego { get; set; }

    // Navigation: one-to-many
    public List<VerbaDeCalculo> Verbas { get; set; } = [];
    public List<HistoricoSalarial.HistoricoSalarial> HistoricosSalariais { get; set; } = [];
    public List<Ferias.Ferias> Ferias { get; set; } = [];
    public List<Falta> Faltas { get; set; } = [];
    public List<ApuracaoDeJuros> ApuracoesDeJuros { get; set; } = [];
    public List<Multa> Multas { get; set; } = [];
    public List<Honorario> Honorarios { get; set; } = [];
    public List<Pagamento.Pagamento> Pagamentos { get; set; } = [];
    public List<CartaoDePonto.CartaoDePonto> CartoesDePonto { get; set; } = [];
    public List<ExcecaoCargaHoraria> ExcecoesCargaHoraria { get; set; } = [];
    public List<ExcecaoSabado> ExcecoesSabado { get; set; } = [];
}
