using System.ComponentModel.DataAnnotations;

namespace RepositorioTutorias.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Los nombres son obligatorios")]
    [StringLength(150, ErrorMessage = "Maximo 150 caracteres")]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio")]
    [StringLength(150, ErrorMessage = "Maximo 150 caracteres")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "El correo no tiene un formato valido")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Debe elegir un rol")]
    public int? IdRol { get; set; }

    public int? IdCarrera { get; set; }

    // Solo para mostrar en el listado
    public string? RolNombre { get; set; }
    public string? CarreraNombre { get; set; }
}
