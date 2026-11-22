using System.ComponentModel.DataAnnotations;

namespace RepositorioTutorias.Models;

public class Categoria
{
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "Maximo 100 caracteres")]
    public string Nombre { get; set; } = "";

    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;
}