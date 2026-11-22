/* rf02.sql: facultades, carreras y usuarios. Requiere 00_base.sql (tabla rol). */

CREATE TABLE IF NOT EXISTS facultad (
    id_facultad INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL UNIQUE,
    activa BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT ck_facultad_nombre CHECK (length(trim(nombre)) > 0)
);

CREATE TABLE IF NOT EXISTS carrera (
    id_carrera INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_facultad INTEGER NOT NULL REFERENCES facultad(id_facultad) ON DELETE RESTRICT,
    nombre VARCHAR(150) NOT NULL,
    activa BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT ck_carrera_nombre CHECK (length(trim(nombre)) > 0),
    CONSTRAINT uq_carrera_facultad_nombre UNIQUE (id_facultad, nombre)
);
CREATE INDEX IF NOT EXISTS ix_carrera_facultad ON carrera (id_facultad);

CREATE TABLE IF NOT EXISTS usuario (
    id_usuario INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombres VARCHAR(150) NOT NULL,
    correo VARCHAR(150) NOT NULL UNIQUE,
    id_rol INTEGER NOT NULL REFERENCES rol(id_rol) ON DELETE RESTRICT,
    id_carrera INTEGER NULL REFERENCES carrera(id_carrera) ON DELETE RESTRICT,
    CONSTRAINT ck_usuario_nombres CHECK (length(trim(nombres)) > 0),
    CONSTRAINT ck_usuario_correo CHECK (correo ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$')
);
CREATE INDEX IF NOT EXISTS ix_usuario_rol ON usuario (id_rol);
CREATE INDEX IF NOT EXISTS ix_usuario_carrera ON usuario (id_carrera);
