using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using NpgsqlTypes;
using RepositorioTutorias.Data;
using RepositorioTutorias.Models;

namespace RepositorioTutorias.Controllers;

public class DocumentosController : Controller
{
    private const int TamanoMaximo = 10 * 1024 * 1024; // 10 MB
    private const int PorPagina = 10;

    private readonly Db _db;
    private readonly IWebHostEnvironment _env;

    public DocumentosController(Db db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private const string SelectDocumento = @"
        SELECT d.id_documento, d.titulo, d.nombre_archivo, d.ruta_archivo, d.tipo_archivo,
               d.tamano_bytes, d.fecha_subida, d.id_usuario, d.id_periodo, d.id_categoria,
               d.id_carrera, p.nombre, c.nombre, ca.nombre
        FROM documento d
        JOIN periodo p ON p.id_periodo = d.id_periodo
        JOIN categoria_documental c ON c.id_categoria = d.id_categoria
        JOIN carrera ca ON ca.id_carrera = d.id_carrera";

    private static Documento Mapear(NpgsqlDataReader r) => new()
    {
        IdDocumento = r.GetInt32(0),
        Titulo = r.GetString(1),
        NombreArchivo = r.GetString(2),
        RutaArchivo = r.GetString(3),
        TipoArchivo = r.GetString(4),
        TamanoBytes = r.GetInt32(5),
        FechaSubida = r.GetDateTime(6),
        IdUsuario = r.GetInt32(7),
        IdPeriodo = r.GetInt32(8),
        IdCategoria = r.GetInt32(9),
        IdCarrera = r.GetInt32(10),
        NombrePeriodo = r.GetString(11),
        NombreCategoria = r.GetString(12),
        NombreCarrera = r.GetString(13)
    };

    private static NpgsqlParameter ParamInt(string nombre, int? valor) =>
        new(nombre, NpgsqlDbType.Integer) { Value = (object?)valor ?? DBNull.Value };

    // Llena los menus desplegables
    private async Task CargarListas()
    {
        static SelectListItem Item(NpgsqlDataReader r) =>
            new() { Value = r.GetInt32(0).ToString(), Text = r.GetString(1) };

        ViewBag.Periodos = await _db.ConsultarAsync("SELECT id_periodo, nombre FROM periodo ORDER BY nombre", Item);
        ViewBag.Categorias = await _db.ConsultarAsync("SELECT id_categoria, nombre FROM categoria_documental ORDER BY nombre", Item);
        ViewBag.Carreras = await _db.ConsultarAsync("SELECT id_carrera, nombre FROM carrera ORDER BY nombre", Item);
        ViewBag.Tutores = await _db.ConsultarAsync( "SELECT id_usuario, nombres FROM usuario WHERE id_rol = (SELECT id_rol FROM rol WHERE nombre = 'tutor') ORDER BY nombres", Item);
    }

    // Reglas: periodo ACTIVO y categoria habilitada para ese periodo
    private async Task ValidarPeriodoCategoria(Documento d)
    {
        var estados = await _db.ConsultarAsync(
            "SELECT estado FROM periodo WHERE id_periodo = @p",
            r => r.GetString(0),
            new NpgsqlParameter("p", d.IdPeriodo));
        if (estados.Count > 0 && estados[0] == "CERRADO")
            ModelState.AddModelError("IdPeriodo", "El periodo esta CERRADO y no recibe documentos");

        var habilitada = await _db.ConsultarAsync(
            "SELECT 1 FROM periodo_categoria WHERE id_periodo = @p AND id_categoria = @c",
            r => r.GetInt32(0),
            new NpgsqlParameter("p", d.IdPeriodo),
            new NpgsqlParameter("c", d.IdCategoria));
        if (habilitada.Count == 0)
            ModelState.AddModelError("IdCategoria", "La categoria no esta habilitada para ese periodo");
    }

    // ---------- LISTA con filtros y paginacion ----------
    public async Task<IActionResult> Index(int? periodo, int? categoria, int? carrera, int pagina = 1)
    {
        if (pagina < 1) pagina = 1;

        const string where = @" WHERE (@periodo IS NULL OR d.id_periodo = @periodo)
                                  AND (@categoria IS NULL OR d.id_categoria = @categoria)
                                  AND (@carrera IS NULL OR d.id_carrera = @carrera)";

        var total = (await _db.ConsultarAsync(
            "SELECT COUNT(*) FROM documento d" + where,
            r => r.GetInt64(0),
            ParamInt("periodo", periodo), ParamInt("categoria", categoria), ParamInt("carrera", carrera)))[0];

        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        if (pagina > totalPaginas) pagina = totalPaginas;

        var lista = await _db.ConsultarAsync(
            SelectDocumento + where + " ORDER BY d.fecha_subida DESC LIMIT @limite OFFSET @desde",
            Mapear,
            ParamInt("periodo", periodo), ParamInt("categoria", categoria), ParamInt("carrera", carrera),
            new NpgsqlParameter("limite", PorPagina),
            new NpgsqlParameter("desde", (pagina - 1) * PorPagina));

        await CargarListas();
        ViewBag.Periodo = periodo;
        ViewBag.Categoria = categoria;
        ViewBag.Carrera = carrera;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.Total = total;
        return View(lista);
    }

    // ---------- SUBIR ----------
    public async Task<IActionResult> Crear()
    {
        await CargarListas();
        return View(new Documento());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Documento d, IFormFile? archivo)
    {
        // Estos campos los llena el controlador, no el formulario
        ModelState.Remove(nameof(Documento.NombreArchivo));
        ModelState.Remove(nameof(Documento.RutaArchivo));
        ModelState.Remove(nameof(Documento.TipoArchivo));

        string tipo = "";
        if (archivo == null || archivo.Length == 0)
            ModelState.AddModelError("archivo", "Seleccione un archivo");
        else
        {
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext == ".pdf") tipo = "PDF";
            else if (ext == ".docx") tipo = "DOCX";
            else ModelState.AddModelError("archivo", "Solo se permiten archivos PDF y DOCX");

            if (archivo.Length > TamanoMaximo)
                ModelState.AddModelError("archivo", "El archivo supera el maximo de 10 MB");
        }

        if (ModelState.IsValid) await ValidarPeriodoCategoria(d);

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(d);
        }

        // Guardar el archivo en wwwroot/uploads con nombre unico
        var carpeta = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(carpeta);
        var nombreGuardado = Guid.NewGuid() + Path.GetExtension(archivo!.FileName).ToLowerInvariant();
        using (var stream = System.IO.File.Create(Path.Combine(carpeta, nombreGuardado)))
            await archivo.CopyToAsync(stream);

        try
        {
            await _db.EjecutarAsync(
                @"INSERT INTO documento (titulo, nombre_archivo, ruta_archivo, tipo_archivo, tamano_bytes,
                                         id_usuario, id_periodo, id_categoria, id_carrera)
                  VALUES (@titulo, @nombre, @ruta, @tipo, @tamano, @usuario, @periodo, @categoria, @carrera)",
                new NpgsqlParameter("titulo", d.Titulo.Trim()),
                new NpgsqlParameter("nombre", Path.GetFileName(archivo.FileName)),
                new NpgsqlParameter("ruta", "uploads/" + nombreGuardado),
                new NpgsqlParameter("tipo", tipo),
                new NpgsqlParameter("tamano", (int)archivo.Length),
                new NpgsqlParameter("usuario", d.IdUsuario),
                new NpgsqlParameter("periodo", d.IdPeriodo),
                new NpgsqlParameter("categoria", d.IdCategoria),
                new NpgsqlParameter("carrera", d.IdCarrera));
            TempData["Ok"] = "Documento subido";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            System.IO.File.Delete(Path.Combine(carpeta, nombreGuardado));
            ModelState.AddModelError("", "Tutor, periodo, categoria o carrera no valido");
            await CargarListas();
            return View(d);
        }
    }

    // ---------- EDITAR (no cambia el archivo) ----------
    public async Task<IActionResult> Editar(int id)
    {
        var lista = await _db.ConsultarAsync(
            SelectDocumento + " WHERE d.id_documento = @id",
            Mapear,
            new NpgsqlParameter("id", id));
        if (lista.Count == 0) return NotFound();
        await CargarListas();
        return View(lista[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Documento d)
    {
        ModelState.Remove(nameof(Documento.NombreArchivo));
        ModelState.Remove(nameof(Documento.RutaArchivo));
        ModelState.Remove(nameof(Documento.TipoArchivo));

        if (ModelState.IsValid) await ValidarPeriodoCategoria(d);

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(d);
        }

        try
        {
            var filas = await _db.EjecutarAsync(
                @"UPDATE documento SET titulo = @titulo, id_usuario = @usuario, id_periodo = @periodo,
                                       id_categoria = @categoria, id_carrera = @carrera
                  WHERE id_documento = @id",
                new NpgsqlParameter("titulo", d.Titulo.Trim()),
                new NpgsqlParameter("usuario", d.IdUsuario),
                new NpgsqlParameter("periodo", d.IdPeriodo),
                new NpgsqlParameter("categoria", d.IdCategoria),
                new NpgsqlParameter("carrera", d.IdCarrera),
                new NpgsqlParameter("id", d.IdDocumento));
            if (filas == 0) return NotFound();
            TempData["Ok"] = "Documento actualizado";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            ModelState.AddModelError("", "Tutor, periodo, categoria o carrera no valido");
            await CargarListas();
            return View(d);
        }
    }

    // ---------- ELIMINAR ----------
    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        var rutas = await _db.ConsultarAsync(
            "SELECT ruta_archivo FROM documento WHERE id_documento = @id",
            r => r.GetString(0),
            new NpgsqlParameter("id", id));

        await _db.EjecutarAsync(
            "DELETE FROM documento WHERE id_documento = @id",
            new NpgsqlParameter("id", id));

        if (rutas.Count > 0)
        {
            var archivo = Path.Combine(_env.WebRootPath, rutas[0]);
            if (System.IO.File.Exists(archivo)) System.IO.File.Delete(archivo);
        }
        TempData["Ok"] = "Documento eliminado";
        return RedirectToAction(nameof(Index));
    }
}