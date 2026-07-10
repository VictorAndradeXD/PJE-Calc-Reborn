namespace PJeCalc.Core.Models.Usuario;

using PJeCalc.Core.Common;

public class Usuario : EntityBase
{
    public string? Nome { get; set; }
    public string? Login { get; set; }
    public string? Senha { get; set; }
    public bool Ativo { get; set; }
    public int? IdSetor { get; set; }
    public List<SetorUsuario> Setores { get; set; } = [];
}
