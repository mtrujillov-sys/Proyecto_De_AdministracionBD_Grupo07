-- datos_masivos.sql: datos ficticios para probar el rendimiento (prueba de diseno)
-- Ejecutar DESPUES de 00_base.sql, rf01.sql, rf02.sql y rf03.sql
-- Para generar mas o menos documentos, cambiar 200000 en la ultima consulta
-- Los archivos no existen fisicamente: solo sirven para probar consultas y paginacion

BEGIN;

INSERT INTO facultad (nombre)
VALUES ('MASIVO Facultad de Prueba')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO carrera (id_facultad, nombre)
SELECT f.id_facultad, 'MASIVO Carrera ' || n
FROM facultad f
CROSS JOIN generate_series(1, 10) AS n
WHERE f.nombre = 'MASIVO Facultad de Prueba'
ON CONFLICT (id_facultad, nombre) DO NOTHING;

INSERT INTO periodo (nombre, fecha_inicio, fecha_fin, estado)
SELECT 'MASIVO ' || (2017 + a)::text || '-' || s::text,
       make_date(2017 + a, CASE WHEN s = 1 THEN 4 ELSE 10 END, 1),
       make_date(2017 + a, CASE WHEN s = 1 THEN 9 ELSE 12 END, 28),
       'CERRADO'
FROM generate_series(1, 8) AS a
CROSS JOIN generate_series(1, 2) AS s
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO categoria_documental (nombre, descripcion)
SELECT 'MASIVO Categoria ' || n, 'Categoria de prueba masiva'
FROM generate_series(1, 5) AS n
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO periodo_categoria (id_periodo, id_categoria)
SELECT p.id_periodo, c.id_categoria
FROM periodo p
CROSS JOIN categoria_documental c
WHERE p.nombre LIKE 'MASIVO %' AND c.nombre LIKE 'MASIVO %'
ON CONFLICT DO NOTHING;

INSERT INTO usuario (nombres, correo, id_rol, id_carrera)
SELECT 'MASIVO Tutor ' || n,
       'masivo.tutor' || n || '@prueba.edu.ec',
       (SELECT id_rol FROM rol WHERE nombre = 'tutor'),
       (SELECT id_carrera FROM carrera WHERE nombre = 'MASIVO Carrera ' || ((n - 1) % 10 + 1))
FROM generate_series(1, 50) AS n
ON CONFLICT (correo) DO NOTHING;

WITH
  u AS (SELECT array_agg(id_usuario ORDER BY id_usuario) AS ids,
               array_agg(id_carrera ORDER BY id_usuario) AS carreras
        FROM usuario WHERE correo LIKE 'masivo.tutor%'),
  p AS (SELECT array_agg(id_periodo ORDER BY id_periodo) AS ids,
               array_agg(fecha_inicio ORDER BY id_periodo) AS inicios
        FROM periodo WHERE nombre LIKE 'MASIVO %'),
  c AS (SELECT array_agg(id_categoria ORDER BY id_categoria) AS ids
        FROM categoria_documental WHERE nombre LIKE 'MASIVO %')
INSERT INTO documento (titulo, nombre_archivo, ruta_archivo, tipo_archivo, tamano_bytes,
                       fecha_subida, id_usuario, id_periodo, id_categoria, id_carrera)
SELECT 'MASIVO Documento ' || g,
       'masivo_' || g || CASE WHEN g % 4 = 0 THEN '.docx' ELSE '.pdf' END,
       'uploads/masivo_' || g || CASE WHEN g % 4 = 0 THEN '.docx' ELSE '.pdf' END,
       CASE WHEN g % 4 = 0 THEN 'DOCX' ELSE 'PDF' END,
       50000 + g % 1000000,
       p.inicios[x.ip]::timestamp + (g % 120) * interval '1 day' + (g % 86400) * interval '1 second',
       u.ids[x.iu],
       p.ids[x.ip],
       c.ids[x.ic],
       u.carreras[x.iu]
FROM generate_series(1, 200000) AS g
CROSS JOIN u
CROSS JOIN p
CROSS JOIN c
CROSS JOIN LATERAL (
    SELECT 1 + g % cardinality(u.ids)       AS iu,
           1 + (g / 7) % cardinality(p.ids) AS ip,
           1 + (g / 3) % cardinality(c.ids) AS ic
) AS x;

COMMIT;

ANALYZE documento;