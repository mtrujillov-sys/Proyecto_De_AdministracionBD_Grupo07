using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class UsuariosController : Controller
{
    private const string RolAdmin = "administrador general";

    private readonly Db _db;

    public UsuariosController(Db db) => _db = db;

    private static Usuario Mapear(NpgsqlDataReader r) => new()
    {
        IdUsuario = r.GetInt32(0),
        Nombres = r.GetString(1),
        Correo = r.GetString(2),
        IdRol = r.GetInt32(3),
        IdCarrera = r.IsDBNull(4) ? null : r.GetInt32(4),
        RolNombre = r.GetString(5),
        CarreraNombre = r.IsDBNull(6) ? null : r.GetString(6)
    };

    private const string SqlBase =
        "SELECT u.id_usuario, u.nombres, u.correo, u.id_rol, u.id_carrera, r.nombre, c.nombre " +
        "FROM usuario u JOIN rol r ON r.id_rol = u.id_rol " +
        "LEFT JOIN carrera c ON c.id_carrera = u.id_carrera ";

    // Listas de roles y carreras para los desplegables
    private async Task CargarListasAsync()
    {
        ViewBag.Roles = await _db.ConsultarAsync(
            "SELECT id_rol, nombre FROM rol ORDER BY id_rol",
            r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));
        ViewBag.Carreras = await _db.ConsultarAsync(
            "SELECT c.id_carrera, c.nombre, f.nombre FROM carrera c JOIN facultad f ON f.id_facultad = c.id_facultad ORDER BY f.nombre, c.nombre",
            r => new SelectListItem(r.GetString(1) + " (" + r.GetString(2) + ")", r.GetInt32(0).ToString()));
    }

    // Solo el administrador general puede quedar sin carrera
    private async Task ValidarCarreraAsync(Usuario u)
    {
        if (u.IdRol == null || u.IdCarrera != null) return;
        var nombres = await _db.ConsultarAsync(
            "SELECT nombre FROM rol WHERE id_rol = @id",
            r => r.GetString(0),
            new NpgsqlParameter("id", u.IdRol));
        if (nombres.Count == 0 || nombres[0] != RolAdmin)
            ModelState.AddModelError("IdCarrera", "Solo el administrador general puede no tener carrera");
    }

    public async Task<IActionResult> Index()
    {
        var lista = await _db.ConsultarAsync(SqlBase + "ORDER BY u.nombres", Mapear);
        return View(lista);
    }

    public async Task<IActionResult> Crear()
    {
        await CargarListasAsync();
        return View(new Usuario());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Usuario u)
    {
        await ValidarCarreraAsync(u);
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(u);
        }
        try
        {
            await _db.EjecutarAsync(
                "INSERT INTO usuario (nombres, correo, id_rol, id_carrera) VALUES (@nombres, @correo, @idRol, @idCarrera)",
                new NpgsqlParameter("nombres", u.Nombres.Trim()),
                new NpgsqlParameter("correo", u.Correo.Trim().ToLowerInvariant()),
                new NpgsqlParameter("idRol", u.IdRol),
                new NpgsqlParameter("idCarrera", (object?)u.IdCarrera ?? DBNull.Value));
            TempData["Ok"] = "Usuario creado";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Correo", "Ya existe un usuario con ese correo");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            ModelState.AddModelError("IdRol", "El rol o la carrera elegidos no existen");
        }
        await CargarListasAsync();
        return View(u);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            SqlBase + "WHERE u.id_usuario = @id",
            Mapear,
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();
        await CargarListasAsync();
        return View(lista[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Usuario u)
    {
        await ValidarCarreraAsync(u);
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(u);
        }
        try
        {
            var filas = await _db.EjecutarAsync(
                "UPDATE usuario SET nombres = @nombres, correo = @correo, id_rol = @idRol, id_carrera = @idCarrera WHERE id_usuario = @id",
                new NpgsqlParameter("nombres", u.Nombres.Trim()),
                new NpgsqlParameter("correo", u.Correo.Trim().ToLowerInvariant()),
                new NpgsqlParameter("idRol", u.IdRol),
                new NpgsqlParameter("idCarrera", (object?)u.IdCarrera ?? DBNull.Value),
                new NpgsqlParameter("id", u.IdUsuario));
            if (filas == 0) return NotFound();
            TempData["Ok"] = "Usuario actualizado";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Correo", "Ya existe un usuario con ese correo");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            ModelState.AddModelError("IdRol", "El rol o la carrera elegidos no existen");
        }
        await CargarListasAsync();
        return View(u);
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _db.EjecutarAsync(
                "DELETE FROM usuario WHERE id_usuario = @id",
                new NpgsqlParameter("id", id));
            TempData["Ok"] = "Usuario eliminado";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            TempData["Error"] = "No se puede eliminar: el usuario tiene documentos asociados";
        }
        return RedirectToAction(nameof(Index));
    }
}
