namespace PJeCalc.Core.Models.Usuario;

using PJeCalc.Core.Common;

public class SetorUsuario : EntityBase
{
    public int? Setor { get; set; }
    public string? NomeSetor { get; set; }
    public char? Instancia { get; set; }
    public long UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
