\# Dificultades técnicas y decisiones de RF-01 (Roselyn)



Para que Mayummy las integre al informe.



1\. \*\*Docker apagado.\*\* Al ejecutar los scripts SQL salió el error "failed to connect to the docker API", porque Docker Desktop estaba apagado. Lo inicié, esperé a que el motor estuviera en ejecución y arranqué el contenedor `postgres\_abd`. La clave de PostgreSQL la guardé con `dotnet user-secrets`, para que no quede escrita en ningún archivo del repositorio.

2\. \*\*Git y carpeta de trabajo.\*\* Algunos comandos se ejecutaron fuera de la carpeta del repositorio y Git respondió "not a git repository". Además, el primer commit falló porque Git no tenía configurada mi identidad (`user.name` y `user.email`). Los dos problemas se resolvieron entrando siempre a la carpeta del proyecto y configurando la identidad con `git config`.

3\. \*\*Vista no encontrada.\*\* Al crear un periodo la aplicación mostró "The view 'Crear' was not found", porque no existía `Views/Periodos/Crear.cshtml` con ese nombre y en esa ubicación. Se creó el archivo correcto y se reinició la aplicación.

4\. \*\*Periodo y categorías en una transacción.\*\* Al crear o editar un periodo se guardan el periodo y sus categorías en una sola transacción, para que no queden datos a medias si algo falla. Las categorías nuevas se insertan con `ON CONFLICT`, para reutilizar una que ya existe con el mismo nombre. Para eliminar un periodo se borran primero sus filas de `periodo\_categoria` dentro de la misma transacción. Si el periodo ya tiene documentos, la clave foránea `ON DELETE RESTRICT` lo impide y la aplicación muestra un mensaje claro.

5\. \*\*Nombres comunes y formato del README.\*\* Acordamos con el grupo que las claves primarias y foráneas se llamen `id\_<tabla>`, para que los scripts de los tres requisitos encajaran. Al pegar el README, el editor insertó caracteres de escape y líneas en blanco de más; lo corregí con un comando de PowerShell antes de subirlo.

