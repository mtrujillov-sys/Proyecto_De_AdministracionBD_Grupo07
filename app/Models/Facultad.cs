using System.ComponentModel.DataAnnotations;

namespace RepositorioTutorias.Models;

public class Facultad
{
    public int IdFacultad { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150, ErrorMessage = "Maximo 150 caracteres")]
    public string Nombre { get; set; } = "";

    public bool Activa { get; set; } = true;
}
