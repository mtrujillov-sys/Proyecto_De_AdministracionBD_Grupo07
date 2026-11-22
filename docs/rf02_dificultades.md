# Dificultades técnicas y decisiones de RF-02 (Kamila)

Para que Mayummy las integre al informe.

1. **Entorno incompleto.** Mi computadora no tenía Git ni el SDK de .NET 11. Los instalé antes de clonar el repositorio y compilar. También tuve que recuperar la clave de PostgreSQL de la configuración del contenedor de Docker y guardarla con `dotnet user-secrets`, para que no quede escrita en ningún archivo del repositorio.
2. **Regla del administrador general.** La columna `usuario.id_carrera` acepta vacío (NULL) porque el administrador general no pertenece a una sola carrera. Como la base de datos no puede distinguir por rol si el vacío es válido o no, decidimos validarlo en el controlador de C#: si el rol no es administrador general, la carrera es obligatoria. Con la misma regla, el coordinador de área también exige carrera.
3. **Nombre de carrera único por facultad.** El reparto solo pedía nombre único para la facultad. Para las carreras usé una restricción `UNIQUE (id_facultad, nombre)`, así dos facultades pueden tener una carrera con el mismo nombre, pero una misma facultad no la repite.
4. **Integración con RF-01 y RF-03.** Al traer `main` a mi rama no hubo conflictos y el proyecto compiló. Al revisar la rama de RF-03 vimos que su consulta de tutores usaba la columna `nombre`, pero en `usuario` se llama `nombres`, y que sus vistas estaban en `app/Documentos/` en vez de `app/Views/Documentos/`. Se lo comunicamos para que lo corrija.
5. **Capturas.** DBeaver cerró la conexión por inactividad y mostró "Not connected to database". Reconectamos y repetimos la captura. Además, la prueba de no eliminar un usuario con documentos depende de la tabla `documento` de RF-03, así que quedó pendiente.
