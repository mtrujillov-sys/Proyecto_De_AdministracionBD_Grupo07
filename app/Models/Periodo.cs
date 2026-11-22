using System.ComponentModel.DataAnnotations;

namespace RepositorioTutorias.Models;

public class PeriodoFila
{
    public int IdPeriodo { get; set; }
    public string Nombre { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string Estado { get; set; } = "";
    public int NumCategorias { get; set; }
}

public class CategoriaUso
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
    public string UsadaEn { get; set; } = "";
}

public class PeriodoForm : IValidatableObject
{
    public int IdPeriodo { get; set; }

    [Required(ErrorMessage = "El nombre del periodo es obligatorio")]
    [StringLength(50, ErrorMessage = "Maximo 50 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
    public DateOnly? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria")]
    public DateOnly? FechaFin { get; set; }

    public string Estado { get; set; } = "ACTIVO";

    public List<int> CategoriasSeleccionadas { get; set; } = new();
    public string? NuevasCategorias { get; set; }
    public List<CategoriaUso> CategoriasDisponibles { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin <= FechaInicio)
            yield return new ValidationResult(
                "La fecha de fin debe ser posterior a la fecha de inicio",
                new[] { nameof(FechaFin) });

        if (Estado != "ACTIVO" && Estado != "CERRADO")
            yield return new ValidationResult("Estado no valido", new[] { nameof(Estado) });
    }
}