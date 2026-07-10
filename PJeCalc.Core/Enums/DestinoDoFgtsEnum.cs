using System.ComponentModel;

namespace PJeCalc.Core.Enums;

/// <summary>
/// Destino do FGTS: pagamento ao reclamante ou deposito em conta vinculada.
/// </summary>
public enum DestinoDoFgtsEnum
{
    /// <summary>
    /// Para pagamento ao reclamante.
    /// </summary>
    [Description("Pagar")]
    Pagar,

    /// <summary>
    /// Para deposito em conta vinculada.
    /// </summary>
    [Description("Recolher")]
    Depositar
}
