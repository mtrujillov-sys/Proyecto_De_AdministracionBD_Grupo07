-- rf03.sql: RF-03 Documentos
-- Ejecutar DESPUES de 00_base.sql, rf01.sql y rf02.sql

CREATE TABLE IF NOT EXISTS documento (
    id_documento    SERIAL PRIMARY KEY,
    titulo          VARCHAR(200)  NOT NULL,
    nombre_archivo  VARCHAR(255)  NOT NULL,
    ruta_archivo    VARCHAR(500)  NOT NULL,
    tipo_archivo    VARCHAR(10)   NOT NULL,
    tamano_bytes    INTEGER       NOT NULL,
    fecha_subida    TIMESTAMP     NOT NULL DEFAULT NOW(),
    id_usuario      INTEGER       NOT NULL,  -- tutor que sube el documento
    id_periodo      INTEGER       NOT NULL,
    id_categoria    INTEGER       NOT NULL,
    id_carrera      INTEGER       NOT NULL,

    CONSTRAINT ck_documento_titulo
        CHECK (length(trim(titulo)) > 0),
    CONSTRAINT ck_documento_tipo
        CHECK (tipo_archivo IN ('PDF', 'DOCX')),
    CONSTRAINT ck_documento_tamano
        CHECK (tamano_bytes > 0 AND tamano_bytes <= 10485760), -- max 10 MB

    CONSTRAINT fk_documento_usuario
        FOREIGN KEY (id_usuario)   REFERENCES usuario(id_usuario)                 ON DELETE RESTRICT,
    CONSTRAINT fk_documento_periodo
        FOREIGN KEY (id_periodo)   REFERENCES periodo(id_periodo)                 ON DELETE RESTRICT,
    CONSTRAINT fk_documento_categoria
        FOREIGN KEY (id_categoria) REFERENCES categoria_documental(id_categoria)  ON DELETE RESTRICT,
    CONSTRAINT fk_documento_carrera
        FOREIGN KEY (id_carrera)   REFERENCES carrera(id_carrera)                 ON DELETE RESTRICT
);

-- Indices para filtros y orden
CREATE INDEX IF NOT EXISTS idx_documento_periodo_categoria
    ON documento (id_periodo, id_categoria);
CREATE INDEX IF NOT EXISTS idx_documento_carrera
    ON documento (id_carrera);
CREATE INDEX IF NOT EXISTS idx_documento_fecha
    ON documento (fecha_subida DESC);