using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class FacultadesController : Controller
{
    private readonly Db _db;

    public FacultadesController(Db db) => _db = db;

    private static Facultad Mapear(NpgsqlDataReader r) => new()
    {
        IdFacultad = r.GetInt32(0),
        Nombre = r.GetString(1),
        Activa = r.GetBoolean(2)
    };

    public async Task<IActionResult> Index()
    {
        var lista = await _db.ConsultarAsync(
            "SELECT id_facultad, nombre, activa FROM facultad ORDER BY nombre",
            Mapear);
        return View(lista);
    }

    public IActionResult Crear() => View(new Facultad());

    [HttpPost]
    public async Task<IActionResult> Crear(Facultad f)
    {
        if (!ModelState.IsValid) return View(f);
        try
        {
            await _db.EjecutarAsync(
                "INSERT INTO facultad (nombre, activa) VALUES (@nombre, @activa)",
                new NpgsqlParameter("nombre", f.Nombre.Trim()),
                new NpgsqlParameter("activa", f.Activa));
            TempData["Ok"] = "Facultad creada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una facultad con ese nombre");
            return View(f);
        }
    }

    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            "SELECT id_facultad, nombre, activa FROM facultad WHERE id_facultad = @id",
            Mapear,
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();
        return View(lista[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Facultad f)
    {
        if (!ModelState.IsValid) return View(f);
        try
        {
            var filas = await _db.EjecutarAsync(
                "UPDATE facultad SET nombre = @nombre, activa = @activa WHERE id_facultad = @id",
                new NpgsqlParameter("nombre", f.Nombre.Trim()),
                new NpgsqlParameter("activa", f.Activa),
                new NpgsqlParameter("id", f.IdFacultad));
            if (filas == 0) return NotFound();
            TempData["Ok"] = "Facultad actualizada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una facultad con ese nombre");
            return View(f);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _db.EjecutarAsync(
                "DELETE FROM facultad WHERE id_facultad = @id",
                new NpgsqlParameter("id", id));
            TempData["Ok"] = "Facultad eliminada";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            TempData["Error"] = "No se puede eliminar: la facultad tiene carreras asociadas";
        }
        return RedirectToAction(nameof(Index));
    }
}
