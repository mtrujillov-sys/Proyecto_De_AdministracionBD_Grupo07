using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class PeriodosController : Controller
{
    private readonly Db _db;

    public PeriodosController(Db db) => _db = db;

    private static NpgsqlCommand Comando(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params NpgsqlParameter[] p)
    {
        var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddRange(p);
        return cmd;
    }

    // Categorias que se muestran en el formulario, con los periodos donde ya se usaron.
    private async Task CargarCategorias(PeriodoForm f)
    {
        f.CategoriasDisponibles = await _db.ConsultarAsync(
            @"SELECT c.id_categoria, c.nombre,
                     COALESCE((SELECT string_agg(p.nombre, ', ' ORDER BY p.fecha_inicio DESC)
                               FROM periodo_categoria pc
                               JOIN periodo p ON p.id_periodo = pc.id_periodo
                               WHERE pc.id_categoria = c.id_categoria), '')
              FROM categoria_documental c
              WHERE c.activa
                 OR c.id_categoria IN (SELECT id_categoria FROM periodo_categoria WHERE id_periodo = @id)
              ORDER BY c.nombre",
            r => new CategoriaUso { IdCategoria = r.GetInt32(0), Nombre = r.GetString(1), UsadaEn = r.GetString(2) },
            new NpgsqlParameter("id", f.IdPeriodo));
    }

    // Guarda el periodo y sus categorias en una sola transaccion.
    private async Task Guardar(PeriodoForm f, bool esNuevo)
    {
        await using var conn = await _db.AbrirAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var id = f.IdPeriodo;
        if (esNuevo)
        {
            await using var ins = Comando(conn, tx,
                "INSERT INTO periodo (nombre, fecha_inicio, fecha_fin) VALUES (@n, @fi, @ff) RETURNING id_periodo",
                new NpgsqlParameter("n", f.Nombre.Trim()),
                new NpgsqlParameter("fi", f.FechaInicio!.Value),
                new NpgsqlParameter("ff", f.FechaFin!.Value));
            id = (int)(await ins.ExecuteScalarAsync())!;
        }
        else
        {
            await using var upd = Comando(conn, tx,
                "UPDATE periodo SET nombre = @n, fecha_inicio = @fi, fecha_fin = @ff, estado = @e WHERE id_periodo = @id",
                new NpgsqlParameter("n", f.Nombre.Trim()),
                new NpgsqlParameter("fi", f.FechaInicio!.Value),
                new NpgsqlParameter("ff", f.FechaFin!.Value),
                new NpgsqlParameter("e", f.Estado),
                new NpgsqlParameter("id", id));
            await upd.ExecuteNonQueryAsync();
        }

        var categorias = new HashSet<int>(f.CategoriasSeleccionadas);

        var nuevas = (f.NuevasCategorias ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var nombre in nuevas)
        {
            await using var cat = Comando(conn, tx,
                "INSERT INTO categoria_documental (nombre) VALUES (@n) ON CONFLICT (nombre) DO UPDATE SET nombre = EXCLUDED.nombre RETURNING id_categoria",
                new NpgsqlParameter("n", nombre));
            categorias.Add((int)(await cat.ExecuteScalarAsync())!);
        }

        await using (var del = Comando(conn, tx,
            "DELETE FROM periodo_categoria WHERE id_periodo = @id",
            new NpgsqlParameter("id", id)))
        {
            await del.ExecuteNonQueryAsync();
        }

        foreach (var idCat in categorias)
        {
            await using var pc = Comando(conn, tx,
                "INSERT INTO periodo_categoria (id_periodo, id_categoria) VALUES (@p, @c)",
                new NpgsqlParameter("p", id),
                new NpgsqlParameter("c", idCat));
            await pc.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<IActionResult> Index()
    {
        var lista = await _db.ConsultarAsync(
            @"SELECT p.id_periodo, p.nombre, p.fecha_inicio, p.fecha_fin, p.estado, COUNT(pc.id_categoria)
              FROM periodo p
              LEFT JOIN periodo_categoria pc ON pc.id_periodo = p.id_periodo
              GROUP BY p.id_periodo, p.nombre, p.fecha_inicio, p.fecha_fin, p.estado
              ORDER BY p.fecha_inicio DESC",
            r => new PeriodoFila
            {
                IdPeriodo = r.GetInt32(0),
                Nombre = r.GetString(1),
                FechaInicio = r.GetFieldValue<DateOnly>(2),
                FechaFin = r.GetFieldValue<DateOnly>(3),
                Estado = r.GetString(4),
                NumCategorias = (int)r.GetInt64(5)
            });
        return View(lista);
    }

    public async Task<IActionResult> Crear()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var f = new PeriodoForm { FechaInicio = hoy, FechaFin = hoy.AddMonths(6) };

        // Por defecto se marcan las categorias del periodo mas reciente.
        f.CategoriasSeleccionadas = await _db.ConsultarAsync(
            @"SELECT id_categoria FROM periodo_categoria
              WHERE id_periodo = (SELECT id_periodo FROM periodo ORDER BY fecha_inicio DESC LIMIT 1)",
            r => r.GetInt32(0));
        await CargarCategorias(f);
        return View(f);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(PeriodoForm f)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await Guardar(f, true);
                TempData["Ok"] = "Periodo creado";
                return RedirectToAction(nameof(Index));
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ModelState.AddModelError("Nombre", "Ya existe un periodo con ese nombre");
            }
        }
        await CargarCategorias(f);
        return View(f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            "SELECT id_periodo, nombre, fecha_inicio, fecha_fin, estado FROM periodo WHERE id_periodo = @id",
            r => new PeriodoForm
            {
                IdPeriodo = r.GetInt32(0),
                Nombre = r.GetString(1),
                FechaInicio = r.GetFieldValue<DateOnly>(2),
                FechaFin = r.GetFieldValue<DateOnly>(3),
                Estado = r.GetString(4)
            },
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();

        var f = lista[0];
        f.CategoriasSeleccionadas = await _db.ConsultarAsync(
            "SELECT id_categoria FROM periodo_categoria WHERE id_periodo = @id",
            r => r.GetInt32(0),
            new NpgsqlParameter("id", id));
        await CargarCategorias(f);
        return View(f);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(PeriodoForm f)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await Guardar(f, false);
                TempData["Ok"] = "Periodo actualizado";
                return RedirectToAction(nameof(Index));
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ModelState.AddModelError("Nombre", "Ya existe un periodo con ese nombre");
            }
        }
        await CargarCategorias(f);
        return View(f);
    }

    [HttpPost]
    public async Task<IActionResult> Cerrar(int id)
    {
        await _db.EjecutarAsync(
            "UPDATE periodo SET estado = 'CERRADO' WHERE id_periodo = @id",
            new NpgsqlParameter("id", id));
        TempData["Ok"] = "Periodo cerrado";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await using var conn = await _db.AbrirAsync();
            await using var tx = await conn.BeginTransactionAsync();
            await using (var d1 = Comando(conn, tx,
                "DELETE FROM periodo_categoria WHERE id_periodo = @id", new NpgsqlParameter("id", id)))
            {
                await d1.ExecuteNonQueryAsync();
            }
            await using (var d2 = Comando(conn, tx,
                "DELETE FROM periodo WHERE id_periodo = @id", new NpgsqlParameter("id", id)))
            {
                await d2.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
            TempData["Ok"] = "Periodo eliminado";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            TempData["Error"] = "No se puede eliminar: el periodo ya tiene documentos";
        }
        return RedirectToAction(nameof(Index));
    }
}