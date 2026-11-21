# Repositorio de Tutorías

Aplicación web para guardar y clasificar los documentos de tutorías por periodo (semestre), para cualquier facultad y carrera.
Proyecto de la materia Administración de Bases de Datos (UTEQ, 5.º nivel, Ingeniería de Software). Grupo 07.

## Tecnologías

- C# con ASP.NET Core MVC (.NET 11)
- Npgsql (conector de PostgreSQL) con consultas parametrizadas
- PostgreSQL (se puede ejecutar con Docker)
- Git y GitHub

## Requisitos previos

- .NET SDK 11
- Docker Desktop (o un PostgreSQL instalado)
- Git

## Cómo ejecutar el proyecto desde cero

Los comandos son para PowerShell, una línea cada uno.

### Paso 1. Clonar el repositorio

```
git clone https://github.com/mtrujillov-sys/Proyecto_De_AdministracionBD_Grupo07.git
cd Proyecto_De_AdministracionBD_Grupo07
```

### Paso 2. Iniciar PostgreSQL con Docker

Cambiar USUARIO y CLAVE por los propios:

```
docker run --name postgres_abd -e POSTGRES_USER=USUARIO -e POSTGRES_PASSWORD=CLAVE -p 5432:5432 -d postgres
```

Si el contenedor ya existe, solo se inicia con:

```
docker start postgres_abd
```

### Paso 3. Crear la base de datos

El nombre lleva mayúscula:

```
docker exec -it postgres_abd createdb -U USUARIO repositorioTutorias
```

### Paso 4. Ejecutar los scripts SQL en este orden

```
Get-Content sql\00_base.sql -Raw | docker exec -i postgres_abd psql -U USUARIO -d repositorioTutorias
Get-Content sql\rf01.sql -Raw | docker exec -i postgres_abd psql -U USUARIO -d repositorioTutorias
Get-Content sql\rf02.sql -Raw | docker exec -i postgres_abd psql -U USUARIO -d repositorioTutorias
Get-Content sql\rf03.sql -Raw | docker exec -i postgres_abd psql -U USUARIO -d repositorioTutorias
```

El orden es obligatorio: rf03.sql usa tablas creadas en rf01.sql y rf02.sql.

### Paso 5. Guardar la cadena de conexión

La clave no se sube a GitHub:

```
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=repositorioTutorias;Username=USUARIO;Password=CLAVE" --project app
```

### Paso 6. Ejecutar la aplicación

```
cd app
dotnet run
```

### Paso 7. Abrir el navegador

Abrir la dirección que muestra la consola (por defecto http://localhost:5093).

## Módulos

| Requisito | Módulo | Dirección | Responsable |
|---|---|---|---|
| RF-01 | Periodos y categorías documentales | /Periodos y /Categorias | Roselyn |
| RF-02 | Facultades, carreras y usuarios | /Facultades, /Carreras y /Usuarios | Kamila |
| RF-03 | Documentos (subir y consultar) | /Documentos | Mayummy |

Prueba de conexión con la base: /Prueba/Roles

## Estructura del proyecto

| Carpeta | Contenido |
|---|---|
| app/Controllers | Controladores en C# (lógica y validaciones) |
| app/Models | Modelos de datos |
| app/Views | Vistas Razor (HTML) |
| app/Data | Db.cs: conexión a PostgreSQL y consultas parametrizadas |
| sql | Scripts de PostgreSQL (00_base.sql, rf01.sql, rf02.sql, rf03.sql) |
| docs | Informe maestro |
| evidencias | Capturas de pruebas |

## Seguridad

- Todas las consultas usan parámetros. Nunca se arma el SQL pegando texto del usuario.
- Las claves se guardan con dotnet user-secrets y no se suben al repositorio.
- Las claves foráneas usan ON DELETE RESTRICT para no borrar datos que están en uso.
