using Npgsql;

namespace RepositorioTutorias.Data;

public class Db
{
    private readonly string _cadena;

    public Db(IConfiguration config)
    {
        _cadena = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Postgres");
    }

    public async Task<NpgsqlConnection> AbrirAsync()
    {
        var conn = new NpgsqlConnection(_cadena);
        await conn.OpenAsync();
        return conn;
    }

    // INSERT, UPDATE o DELETE con parametros. Devuelve las filas afectadas.
    public async Task<int> EjecutarAsync(string sql, params NpgsqlParameter[] parametros)
    {
        await using var conn = await AbrirAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parametros);
        return await cmd.ExecuteNonQueryAsync();
    }

    // SELECT con parametros. Convierte cada fila con la funcion "mapear".
    public async Task<List<T>> ConsultarAsync<T>(string sql, Func<NpgsqlDataReader, T> mapear, params NpgsqlParameter[] parametros)
    {
        await using var conn = await AbrirAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parametros);
        await using var reader = await cmd.ExecuteReaderAsync();
        var lista = new List<T>();
        while (await reader.ReadAsync()) lista.Add(mapear(reader));
        return lista;
    }
}