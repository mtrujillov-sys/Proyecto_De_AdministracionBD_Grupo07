using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class CarrerasController : Controller
{
    private readonly Db _db;

    public CarrerasController(Db db) => _db = db;

    private static Carrera Mapear(NpgsqlDataReader r) => new()
    {
        IdCarrera = r.GetInt32(0),
        IdFacultad = r.GetInt32(1),
        Nombre = r.GetString(2),
        Activa = r.GetBoolean(3),
        FacultadNombre = r.GetString(4)
    };

    private const string SqlBase =
        "SELECT c.id_carrera, c.id_facultad, c.nombre, c.activa, f.nombre " +
        "FROM carrera c JOIN facultad f ON f.id_facultad = c.id_facultad ";

    // Lista de facultades para el desplegable
    private async Task CargarFacultadesAsync()
    {
        ViewBag.Facultades = await _db.ConsultarAsync(
            "SELECT id_facultad, nombre FROM facultad ORDER BY nombre",
            r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));
    }

    public async Task<IActionResult> Index()
    {
        var lista = await _db.ConsultarAsync(SqlBase + "ORDER BY f.nombre, c.nombre", Mapear);
        return View(lista);
    }

    public async Task<IActionResult> Crear()
    {
        await CargarFacultadesAsync();
        return View(new Carrera());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Carrera c)
    {
        if (!ModelState.IsValid)
        {
            await CargarFacultadesAsync();
            return View(c);
        }
        try
        {
            await _db.EjecutarAsync(
                "INSERT INTO carrera (id_facultad, nombre, activa) VALUES (@idFacultad, @nombre, @activa)",
                new NpgsqlParameter("idFacultad", c.IdFacultad),
                new NpgsqlParameter("nombre", c.Nombre.Trim()),
                new NpgsqlParameter("activa", c.Activa));
            TempData["Ok"] = "Carrera creada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una carrera con ese nombre en la facultad");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            ModelState.AddModelError("IdFacultad", "La facultad elegida no existe");
        }
        await CargarFacultadesAsync();
        return View(c);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            SqlBase + "WHERE c.id_carrera = @id",
            Mapear,
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();
        await CargarFacultadesAsync();
        return View(lista[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Carrera c)
    {
        if (!ModelState.IsValid)
        {
            await CargarFacultadesAsync();
            return View(c);
        }
        try
        {
            var filas = await _db.EjecutarAsync(
                "UPDATE carrera SET id_facultad = @idFacultad, nombre = @nombre, activa = @activa WHERE id_carrera = @id",
                new NpgsqlParameter("idFacultad", c.IdFacultad),
                new NpgsqlParameter("nombre", c.Nombre.Trim()),
                new NpgsqlParameter("activa", c.Activa),
                new NpgsqlParameter("id", c.IdCarrera));
            if (filas == 0) return NotFound();
            TempData["Ok"] = "Carrera actualizada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una carrera con ese nombre en la facultad");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            ModelState.AddModelError("IdFacultad", "La facultad elegida no existe");
        }
        await CargarFacultadesAsync();
        return View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _db.EjecutarAsync(
                "DELETE FROM carrera WHERE id_carrera = @id",
                new NpgsqlParameter("id", id));
            TempData["Ok"] = "Carrera eliminada";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            TempData["Error"] = "No se puede eliminar: la carrera tiene usuarios asociados";
        }
        return RedirectToAction(nameof(Index));
    }
}
