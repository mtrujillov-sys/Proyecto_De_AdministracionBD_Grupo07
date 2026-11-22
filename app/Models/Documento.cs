using System.ComponentModel.DataAnnotations;

namespace RepositorioTutorias.Models;

public class Documento
{
    public int IdDocumento { get; set; }

    [Required(ErrorMessage = "El titulo es obligatorio")]
    [StringLength(200, ErrorMessage = "Maximo 200 caracteres")]
    public string Titulo { get; set; } = "";

    // Se llenan al subir el archivo (no los escribe el usuario)
    public string NombreArchivo { get; set; } = "";
    public string RutaArchivo { get; set; } = "";
    public string TipoArchivo { get; set; } = "";
    public int TamanoBytes { get; set; }
    public DateTime FechaSubida { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tutor")]
    public int IdUsuario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un periodo")]
    public int IdPeriodo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoria")]
    public int IdCategoria { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una carrera")]
    public int IdCarrera { get; set; }

    // Solo para mostrar en la lista (vienen de los JOIN)
    public string? NombrePeriodo { get; set; }
    public string? NombreCategoria { get; set; }
    public string? NombreCarrera { get; set; }
}