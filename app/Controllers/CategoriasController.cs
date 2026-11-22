using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class CategoriasController : Controller
{
    private readonly Db _db;

    public CategoriasController(Db db) => _db = db;

    private static Categoria Mapear(NpgsqlDataReader r) => new()
    {
        IdCategoria = r.GetInt32(0),
        Nombre = r.GetString(1),
        Descripcion = r.IsDBNull(2) ? null : r.GetString(2),
        Activa = r.GetBoolean(3)
    };

    public async Task<IActionResult> Index()
    {
        var lista = await _db.ConsultarAsync(
            "SELECT id_categoria, nombre, descripcion, activa FROM categoria_documental ORDER BY nombre",
            Mapear);
        return View(lista);
    }

    public IActionResult Crear() => View(new Categoria());

    [HttpPost]
    public async Task<IActionResult> Crear(Categoria c)
    {
        if (!ModelState.IsValid) return View(c);
        try
        {
            await _db.EjecutarAsync(
                "INSERT INTO categoria_documental (nombre, descripcion, activa) VALUES (@nombre, @descripcion, @activa)",
                new NpgsqlParameter("nombre", c.Nombre.Trim()),
                new NpgsqlParameter("descripcion", (object?)c.Descripcion ?? DBNull.Value),
                new NpgsqlParameter("activa", c.Activa));
            TempData["Ok"] = "Categoria creada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una categoria con ese nombre");
            return View(c);
        }
    }

    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            "SELECT id_categoria, nombre, descripcion, activa FROM categoria_documental WHERE id_categoria = @id",
            Mapear,
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();
        return View(lista[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Categoria c)
    {
        if (!ModelState.IsValid) return View(c);
        try
        {
            var filas = await _db.EjecutarAsync(
                "UPDATE categoria_documental SET nombre = @nombre, descripcion = @descripcion, activa = @activa WHERE id_categoria = @id",
                new NpgsqlParameter("nombre", c.Nombre.Trim()),
                new NpgsqlParameter("descripcion", (object?)c.Descripcion ?? DBNull.Value),
                new NpgsqlParameter("activa", c.Activa),
                new NpgsqlParameter("id", c.IdCategoria));
            if (filas == 0) return NotFound();
            TempData["Ok"] = "Categoria actualizada";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Nombre", "Ya existe una categoria con ese nombre");
            return View(c);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _db.EjecutarAsync(
                "DELETE FROM categoria_documental WHERE id_categoria = @id",
                new NpgsqlParameter("id", id));
            TempData["Ok"] = "Categoria eliminada";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            TempData["Error"] = "No se puede eliminar: la categoria esta en uso en un periodo o documento";
        }
        return RedirectToAction(nameof(Index));
    }
}