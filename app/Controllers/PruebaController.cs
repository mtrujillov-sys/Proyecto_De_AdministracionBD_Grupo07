using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RepositorioTutorias.Data;

namespace RepositorioTutorias.Controllers;

public class PruebaController : Controller
{
    private readonly Db _db;

    public PruebaController(Db db) => _db = db;

    public async Task<IActionResult> Roles()
    {
        var roles = await _db.ConsultarAsync(
            "SELECT nombre FROM rol WHERE id_rol >= @minimo ORDER BY id_rol",
            r => r.GetString(0),
            new NpgsqlParameter("minimo", 1));
        return Content("Conexion OK. Roles: " + string.Join(", ", roles));
    }
}