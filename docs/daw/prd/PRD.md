# PRD-001: Módulo de Stock — Genera pedidos de mercadería automáticamente.

## Contexto y Problema
En un pequeño comercio de barrio que compra y vende artículos (por ejemplo, un negocio de repuestos de artículos del hogar), resulta difícil saber si se tiene en existencia la cantidad necesaria de cada artículo. Sin un control ordenado de compras y ventas, el dueño no sabe qué reponer ni cuánto, y termina quedándose sin stock de lo que más rota o comprando de más lo que no se vende.

Personas:
- **Administrador**: dueño/encargado del comercio. Da de alta usuarios y perfiles, mantiene el catálogo de artículos y necesita saber qué pedir.
- **Administrativo / Vendedor**: registra las compras y ventas del día a día y consulta el stock y los pedidos a generar.

## Objetivos
Este módulo va a ser un SITIO WEB que, con solo registrar las ventas y las compras de mercadería, permita inferir automáticamente la lista de artículos que hace falta pedir según los siguientes criterios:

- Pedir todos los artículos hasta alcanzar el stock mínimo de cada uno.
- Pedir todos los artículos hasta alcanzar el punto de pedido de cada uno.
- Pedir todos los artículos hasta alcanzar el stock ideal de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el stock mínimo de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el punto de pedido de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el stock ideal de cada uno.

## Requerimientos Funcionales

### Perfiles de seguridad
- RF-01: El sistema debe permitir dar de alta un perfil de seguridad con los campos ID (autonumérico) y Descripción (ejemplo: administrador, administrativo, vendedor).
- RF-02: El sistema debe permitir dar de baja un perfil de seguridad existente.
- RF-03: El sistema debe permitir modificar la Descripción de un perfil de seguridad existente.

### Usuarios
- RF-04: El sistema debe permitir dar de alta un usuario con los siguientes campos: UsuarioId, Usuario, NombreCompleto, Hash, Salt.
- RF-05: El sistema debe permitir dar de baja un usuario existente.
- RF-06: El sistema debe permitir modificar los datos de un usuario existente.

### Seguridad de credenciales
- RF-07: La contraseña de cada usuario debe almacenarse como un hash generado a partir de dicha contraseña, de forma que no sea posible desencriptarla ni recuperarla en texto plano.
- RF-08: El hash de la contraseña de cada usuario debe generarse utilizando un salt aleatorio propio de ese usuario, de forma que dos usuarios con la misma contraseña tengan hashes distintos entre sí.
- RF-09: El sistema debe rechazar el alta o la modificación de un usuario cuya contraseña tenga menos de 8 caracteres alfanuméricos, mostrando un mensaje de error y sin grabar el registro.

### Acceso
- RF-10: La carga de usuarios (RF-04, RF-05, RF-06) solo debe estar accesible para usuarios del perfil administrador.
- RF-11: El sistema debe tener una pantalla de inicio de sesión, donde se pida usuario y contraseña, y dicha contraseña debe validarse contra el hash asociado al usuario, teniendo en cuenta el salt del mismo usuario.
- RF-12: El sistema debe exigir una sesión autenticada válida (JWT) para acceder a cualquier funcionalidad, con excepción de la pantalla de inicio de sesión (RF-11). Toda solicitud a un endpoint protegido de la API sin un token JWT válido debe ser rechazada.

### Artículos
- RF-13: El sistema debe permitir dar de alta un artículo con los siguientes campos: Código, Descripción, Precio de Costo, Margen (%), Precio de Venta (calculado automáticamente según RF-16), Stock Mínimo, Punto de Pedido, Stock Ideal.
- RF-14: El sistema debe permitir dar de baja un artículo existente.
- RF-15: El sistema debe permitir modificar los datos de un artículo existente.
- RF-16: El sistema debe calcular automáticamente el Precio de Venta de cada artículo a partir del Precio de Costo y el Margen (%), aplicando la siguiente fórmula:

  Precio de Venta = Precio de Costo × (1 + Margen / 100)

- RF-17: El sistema debe rechazar el alta o la modificación de un artículo cuyo Código coincida con el de otro artículo ya existente, de forma que el Código sea único.
- RF-18: El sistema debe rechazar el alta o la modificación de un artículo si alguno de los campos Precio de Costo, Margen, Stock Mínimo, Punto de Pedido o Stock Ideal es un valor negativo.
- RF-19: El sistema debe rechazar el alta o la modificación de un artículo que no cumpla la condición Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal.

### Movimientos
- RF-20: El sistema debe permitir dar de alta un Movimiento (venta o compra), informando los siguientes campos en el encabezado: Tipo de Movimiento, Número y Fecha; y los siguientes campos en el detalle: Código, Cantidad, Precio Unitario y Precio Total. Cada campo Código del detalle debe contar con el botón de búsqueda y la Descripción asociada (ver RF-36, RF-38).
- RF-21: El sistema debe permitir dar de baja un Movimiento existente (encabezado y detalle).
- RF-22: El sistema debe permitir modificar un Movimiento existente (encabezado y detalle).
- RF-23: El sistema debe rechazar el alta o la modificación de un Movimiento en el que alguna línea de detalle tenga una Cantidad que no sea un número entero mayor que 0.
- RF-24: El sistema debe rechazar el alta o la modificación de un Movimiento de tipo venta que dejaría el Stock Actual de alguno de sus artículos por debajo de 0, mostrando un mensaje de error y sin grabar el movimiento.

### Consultas
- RF-25: El sistema debe tener una consulta por pantalla, exportable a Excel, llamada "Consulta de Stock Actual" que permita ver la cantidad en existencia actual de cada artículo. Los parámetros de esta consulta deben ser el rango de artículos (artículo inicial y artículo final, cada uno con su botón de búsqueda y su Descripción asociada según RF-36 y RF-38) y las columnas de la grilla deben ser: Código, Descripción y Cantidad. La cantidad de cada artículo se calcula del saldo de cada uno, según los movimientos registrados (las ventas restan y las compras suman). Los valores por defecto de estos parámetros están especificados en RF-43.
- RF-26: El sistema debe tener una consulta por pantalla, exportable a Excel, llamada "Generar Pedido" que permita ver la cantidad a pedir de cada artículo. Los parámetros de esta consulta deben ser "solo bajo mínimo" (boolean) y "Modo de Pedido" (lista desplegable con las opciones: Hasta Stock Mínimo, Hasta Punto Pedido, Hasta Stock Ideal); las columnas de la grilla deben ser: Código, Descripción y Cantidad a Pedir. El cálculo de la Cantidad a Pedir para cada una de las 6 combinaciones posibles de estos dos parámetros está especificado en AC-31 a AC-36. Los valores por defecto de estos parámetros están especificados en RF-42.

### Registro de errores
- RF-27: Ante cualquier error de ejecución, el mensaje debe guardarse en una tabla de errores, con las siguientes columnas: ErrorId (autonumérico), ErrorDateTime, MachineName, Message, FullException.

### Búsqueda de Artículos
- RF-28: El sistema debe tener una funcionalidad llamada "Búsqueda de Artículos", implementada como un pop up (ventana modal) en un componente aparte y reutilizable, independiente de las pantallas que lo consumen. Las pantallas que necesiten obtener un Código de Artículo solo deben invocarlo, sin reimplementar su lógica ni su interfaz.
- RF-29: El pop up de Búsqueda de Artículos debe contener un campo de texto "Descripción", un botón "Buscar" y una grilla de resultados con scroll vertical y dos columnas: Código y Descripción.
- RF-30: Al presionar "Buscar", el sistema debe llenar la grilla con los artículos cuya Descripción contenga el texto ingresado, filtrando por contenido mediante LIKE '%texto%' y sin distinguir mayúsculas de minúsculas.
- RF-31: Si el campo Descripción está vacío al presionar "Buscar", el sistema debe listar todos los artículos en la grilla.
- RF-32: La búsqueda debe devolver como máximo los primeros 500 artículos, ordenados por Código. Si existen más coincidencias que ese tope, el pop up debe mostrar el aviso "Se muestran los primeros 500 resultados. Refine la descripción para acotar la búsqueda."
- RF-33: El pop up de Búsqueda de Artículos no debe tener un alto mayor a 600 píxeles. El scroll vertical de la grilla debe quedar contenido dentro de ese alto.
- RF-34: Al hacer un clic sobre una fila de la grilla de resultados, el sistema debe cerrar el pop up y trasladar el Código de esa fila al textbox de Código de Artículo de la pantalla que lo invocó.
- RF-35: El pop up debe poder cerrarse sin seleccionar ningún artículo (botón "Cancelar" o cierre de la ventana modal), dejando el textbox invocador con su valor previo sin modificar.
- RF-36: En todas las pantallas donde se solicite un Código de Artículo, el sistema debe mostrar, inmediatamente al lado del textbox, un botón pequeño cuyo único contenido sea un ícono de lupa, que al presionarse abra el pop up de Búsqueda de Artículos (RF-28). Las pantallas alcanzadas son: el ABM de Artículos (RF-13, RF-14, RF-15), cada línea del detalle de Movimientos (RF-20) y los parámetros Artículo Inicial y Artículo Final de la Consulta de Stock Actual (RF-25).
- RF-37: El Código trasladado desde el pop up de búsqueda debe disparar exactamente las mismas validaciones, cálculos y operaciones asociadas que se ejecutarían si el usuario hubiese tecleado ese mismo código manualmente.
- RF-38: En todas las pantallas donde se solicite un Código de Artículo, el sistema debe mostrar la Descripción del artículo correspondiente, en modo solo lectura, ubicada debajo del textbox de Código.
- RF-39: La Descripción mostrada según RF-38 debe mantenerse sincronizada con el Código presente en el textbox en todo momento, actualizándose tanto cuando el usuario modifica el código manualmente como cuando lo selecciona desde el pop up de Búsqueda de Artículos.
- RF-40: Si el Código ingresado manualmente no corresponde a ningún artículo existente, el sistema debe mostrar el mensaje "Artículo inexistente" en el lugar donde se muestra la Descripción (RF-38) y no debe ejecutar las operaciones asociadas al código. Si el textbox de Código queda vacío, la Descripción también debe quedar vacía, sin mensaje de error.
- RF-41: El pop up de Búsqueda de Artículos y la resolución de la Descripción a partir de un Código deben obtener sus datos desde endpoints de la Web API, sujetos a la autenticación JWT exigida por RF-12.

### Valores por defecto de los parámetros de consulta
- RF-42: Al abrir la pantalla de la consulta "Generar Pedido" (RF-26), el sistema debe presentar sus parámetros con los siguientes valores sugeridos por defecto: "solo bajo mínimo" = No y "Modo de Pedido" = "Hasta Stock Ideal". El usuario debe poder modificarlos antes de ejecutar la consulta.
- RF-43: Al abrir la pantalla de la "Consulta de Stock Actual" (RF-25), el sistema debe presentar sus parámetros con los siguientes valores sugeridos por defecto: Código desde = el primer Código de artículo del catálogo y Código hasta = el último Código de artículo del catálogo, tomados según el orden ascendente de Código, de forma que el rango sugerido abarque la totalidad del catálogo. El usuario debe poder modificarlos antes de ejecutar la consulta.
- RF-44: Las Descripciones asociadas a los parámetros Código desde y Código hasta (RF-38) deben mostrarse ya resueltas al abrir la "Consulta de Stock Actual" con sus valores por defecto. Si el catálogo de artículos está vacío, ambos parámetros deben quedar vacíos, sin Descripción y sin mensaje de error.
- RF-45: Los valores por defecto de RF-42 y RF-43 son sugerencias iniciales y no restricciones: la consulta debe ejecutarse siempre con los valores efectivamente presentes en los parámetros al momento de ejecutarla.

### Carga de líneas de detalle de Movimientos
- RF-46: La pantalla de carga de Movimientos (RF-20) debe tener un botón "Agregar Línea" que, al presionarse, agregue una nueva línea de detalle vacía a la grilla del detalle, lista para ser completada por el usuario. La nueva línea debe incluir sus campos Código, Cantidad, Precio Unitario y Precio Total, junto con el botón de búsqueda y la Descripción asociada al Código (RF-36, RF-38).
- RF-47: El botón "Agregar Línea" no debe imponer ningún límite a la cantidad de líneas de detalle de un Movimiento: el usuario debe poder agregar tantas líneas como necesite.
- RF-48: Las líneas agregadas mediante "Agregar Línea" deben comportarse igual que cualquier otra línea del detalle, quedando sujetas a las mismas validaciones y operaciones asociadas (RF-23, RF-24, RF-36 a RF-40) y persistiéndose en la tabla de detalle al grabar el Movimiento (RF-20).
- RF-49: Cada línea de la grilla de detalle de la carga de Movimientos debe tener un botón "Eliminar Línea" que, al presionarse, quite esa línea del detalle, sin afectar los datos ya cargados en las demás líneas.
- RF-50: La eliminación de líneas debe poder aplicarse tanto a líneas recién agregadas con "Agregar Línea" y todavía no grabadas, como a líneas ya persistidas de un Movimiento existente que se está modificando (RF-22). En este último caso, la línea debe quedar efectivamente eliminada de la tabla de detalle al grabar el Movimiento.
- RF-51: El sistema debe rechazar el alta o la modificación de un Movimiento que no tenga al menos una línea de detalle, mostrando un mensaje de error y sin grabar el movimiento.
- RF-52: Al presionar "Eliminar Línea" (RF-49) sobre una línea que tenga datos cargados en alguno de sus campos, el sistema debe pedir confirmación al usuario antes de eliminarla. Si el usuario confirma, la línea se elimina; si el usuario cancela, la línea permanece en el detalle sin ninguna modificación. Si la línea está completamente vacía, debe eliminarse directamente, sin pedir confirmación.

### Total del detalle de Movimientos
- RF-53: La grilla del detalle de la carga de Movimientos debe totalizar la columna Precio Total, mostrando al pie de dicha columna la suma del Precio Total de todas las líneas presentes en la grilla.
- RF-54: El total de RF-53 debe mantenerse siempre actualizado según la información existente en la grilla en ese momento, esté persistida o no. Debe recalcularse en el acto ante cualquier cambio que afecte la columna Precio Total, sin necesidad de grabar el Movimiento ni de refrescar la pantalla. Los cambios que deben disparar el recálculo incluyen: agregar una línea (RF-46), eliminar una línea (RF-49) y modificar la Cantidad o el Precio Unitario de cualquier línea, lo que a su vez recalcula el Precio Total de esa línea (RF-56).
- RF-55: Si la grilla del detalle no tiene ninguna línea, el total debe mostrarse en 0. Las líneas incompletas, cuyo Precio Total todavía no está informado, deben computarse como 0 dentro del total, sin impedir su cálculo ni el de las líneas restantes.

### Precio Total de la línea de detalle
- RF-56: El sistema debe calcular automáticamente el Precio Total de cada línea del detalle de un Movimiento (RF-20) a partir de la Cantidad y el Precio Unitario de esa misma línea, aplicando la siguiente fórmula:

  Precio Total = Cantidad × Precio Unitario

- RF-57: El Precio Total de cada línea debe mostrarse en modo solo lectura: el usuario no debe poder editarlo directamente. Debe recalcularse en el acto cada vez que cambie la Cantidad o el Precio Unitario de esa línea, sin necesidad de grabar el Movimiento ni de refrescar la pantalla.
- RF-58: Si la Cantidad o el Precio Unitario de una línea todavía no están informados, el Precio Total de esa línea debe mostrarse en 0, computando como 0 dentro del total del detalle (RF-55).

### Sugerencia del Precio Unitario
- RF-59: Al informarse el Código de Artículo de una línea del detalle, ya sea tecleado manualmente o seleccionado desde el pop up de Búsqueda de Artículos (RF-37), el sistema debe sugerir automáticamente el Precio Unitario de esa línea según el Tipo de Movimiento indicado en el encabezado (RF-20): si el Tipo de Movimiento es compra, se sugiere el Precio de Costo del artículo; si es venta, se sugiere el Precio de Venta del artículo (RF-16).
- RF-60: Si el usuario reemplaza el Código de Artículo de una línea por el de otro artículo, el Precio Unitario debe volver a sugerirse con el precio correspondiente al nuevo artículo, según la misma regla de RF-59.
- RF-61: El Precio Unitario sugerido debe ser editable. El usuario puede reemplazarlo por cualquier otro valor, y ese valor es el que debe usarse para calcular el Precio Total de la línea (RF-56) y el que debe persistirse al grabar el Movimiento.
- RF-62: Si el Código de Artículo de una línea está vacío o no corresponde a ningún artículo existente (RF-40), el sistema no debe sugerir Precio Unitario para esa línea, dejando el campo en 0.
- RF-63: Si el usuario cambia el Tipo de Movimiento del encabezado cuando el detalle ya tiene líneas cargadas, el sistema debe pedir confirmación antes de re-sugerir los precios. Si el usuario confirma, el Precio Unitario de todas las líneas debe volver a sugerirse según el nuevo Tipo de Movimiento (RF-59), descartando los valores anteriores; si el usuario cancela, el Tipo de Movimiento debe volver a su valor previo y las líneas deben conservar sus Precios Unitarios sin modificación.

## Requerimientos No Funcionales

- RNF-01: El Front-End debe ser un sitio Web ASP.NET MVC con .NET 8.
- RNF-02: El Back-End debe estar implementado completamente en una Web API REST con .NET 8, en un proyecto aparte, y con autenticación JWT (JSON Web Token), que es invocada desde el Front-End (ver RF-12).
- RNF-03: La base de datos debe ser SQL Server 2017.
- RNF-04: Las consultas "Consulta de Stock Actual" y "Generar Pedido" deben responder en menos de 3 segundos (p95), incluso con hasta 10000 artículos.
- RNF-05: El sistema debe soportar entre 1 y 5 usuarios concurrentes.
- RNF-06: La contraseña de cada usuario debe tener un mínimo de 8 caracteres alfanuméricos.
- RNF-07: La búsqueda del pop up de Búsqueda de Artículos (RF-30, RF-31) debe responder en menos de 2 segundos (p95), incluso con un catálogo de hasta 10000 artículos y con la Descripción vacía.
- RNF-08: El pop up de Búsqueda de Artículos debe implementarse como un único componente compartido (una sola vista parcial y un solo script), reutilizado sin duplicación de código por todas las pantallas de RF-36.

## Criterios de Aceptación

### Perfiles
- AC-01 (RF-01): Dado un registro nuevo de perfil, Cuando se agrega, Entonces queda persistido y puede recuperarse por su ID.
- AC-02 (RF-02): Dado un perfil existente, Cuando se elimina, Entonces deja de existir y no puede recuperarse por su ID.
- AC-03 (RF-03): Dado un perfil existente, Cuando se modifica su Descripción, Entonces el cambio queda persistido.

### Usuarios
- AC-04 (RF-04): Dado un registro nuevo de usuario, Cuando se agrega, Entonces queda persistido y puede recuperarse por su UsuarioId.
- AC-05 (RF-05): Dado un usuario existente, Cuando se elimina, Entonces deja de existir y no puede recuperarse por su UsuarioId.
- AC-06 (RF-06): Dado un usuario existente, Cuando se modifican sus datos, Entonces el cambio queda persistido.

### Seguridad de credenciales
- AC-07 (RF-07): Dado el alta de un usuario con una contraseña, Cuando se graba el registro, Entonces la contraseña se almacena como hash y no en texto plano ni en un formato reversible.
- AC-08 (RF-08): Dadas dos altas de usuario con la misma contraseña, Cuando se generan sus registros, Entonces los salts grabados para cada usuario son distintos entre sí.
- AC-09 (RF-09): Dado el alta o modificación de un usuario con una contraseña de menos de 8 caracteres alfanuméricos, Cuando se intenta grabar, Entonces el sistema rechaza la operación, muestra un mensaje de error y no graba el registro.
- AC-10 (RF-09): Dado el alta o modificación de un usuario con una contraseña de 8 o más caracteres alfanuméricos, Cuando se graba, Entonces la operación se acepta y el registro queda persistido.

### Acceso
- AC-11 (RF-10): Dado un usuario cuyo perfil no es administrador, Cuando intenta acceder a la carga de usuarios, Entonces el sistema deniega el acceso.
- AC-12 (RF-11): Dado un usuario que no existe, Cuando intenta iniciar sesión, Entonces el sistema muestra el mensaje "Usuario o contraseña incorrectos" y no autoriza el ingreso.
- AC-13 (RF-11): Dado un usuario existente con contraseña incorrecta, Cuando intenta iniciar sesión, Entonces el sistema muestra el mensaje "Usuario o contraseña incorrectos" y no autoriza el ingreso.
- AC-14 (RF-11): Dado un usuario existente con contraseña correcta, Cuando inicia sesión, Entonces el sistema autoriza el ingreso.
- AC-15 (RF-12): Dado un request sin un token JWT válido, Cuando se invoca un endpoint protegido de la API, Entonces el sistema responde con error 401 (No autorizado) y deniega el acceso.

### Artículos
- AC-16 (RF-13): Dado un registro nuevo de artículo, Cuando se agrega, Entonces queda persistido y puede recuperarse por su Código.
- AC-17 (RF-14): Dado un artículo existente, Cuando se elimina, Entonces deja de existir y no puede recuperarse por su Código.
- AC-18 (RF-15): Dado un artículo existente, Cuando se modifican sus datos, Entonces el cambio queda persistido.
- AC-19 (RF-16): Dado un Precio de Costo y un Margen (%) cargados, Cuando se graba el artículo, Entonces el Precio de Venta se calcula como Precio de Costo × (1 + Margen / 100).
- AC-20 (RF-17): Dado un artículo cuyo Código coincide con el de otro artículo existente, Cuando se intenta grabar, Entonces el sistema rechaza la operación y no graba el registro.
- AC-21 (RF-18): Dado un artículo con Precio de Costo, Margen, Stock Mínimo, Punto de Pedido o Stock Ideal negativo, Cuando se intenta grabar, Entonces el sistema rechaza la operación y no graba el registro.
- AC-22 (RF-19): Dado un artículo que no cumple Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal, Cuando se intenta grabar, Entonces el sistema rechaza la operación y no graba el registro.

### Movimientos
- AC-23 (RF-20): Dado un movimiento nuevo, Cuando se agrega, Entonces queda persistido en las tablas de encabezado y detalle.
- AC-24 (RF-21): Dado un movimiento existente, Cuando se elimina, Entonces deja de existir en las tablas de encabezado y detalle.
- AC-25 (RF-22): Dado un movimiento existente, Cuando se modifica, Entonces el cambio queda persistido en las tablas de encabezado y detalle.
- AC-26 (RF-23): Dado un movimiento con una línea de detalle cuya Cantidad es 0, negativa o no entera, Cuando se intenta grabar, Entonces el sistema rechaza la operación y no graba el movimiento.
- AC-27 (RF-24): Dado un movimiento de venta que dejaría el Stock Actual de alguno de sus artículos por debajo de 0, Cuando se intenta grabar, Entonces el sistema rechaza la operación, muestra un mensaje de error y no graba el movimiento.
- AC-28 (RF-24): Dado un movimiento de venta que deja el Stock Actual de todos sus artículos en 0 o más, Cuando se graba, Entonces la operación se acepta y el movimiento queda persistido.

### Consulta de Stock Actual
- AC-29 (RF-25): Dado un rango de artículos, Cuando se ejecuta la Consulta de Stock Actual, Entonces devuelve las columnas Código, Descripción y Cantidad, calculada por saldo de movimientos (las ventas restan y las compras suman).
- AC-30 (RF-25): Dado un resultado de la Consulta de Stock Actual, Cuando se presiona el botón "Exportar a Excel", Entonces se descarga un archivo Excel con el contenido de la grilla.

### Generar Pedido
- AC-31 (RF-26): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Stock Mínimo", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Stock Mínimo − Stock Actual).
- AC-32 (RF-26): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Punto Pedido", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Punto de Pedido − Stock Actual).
- AC-33 (RF-26): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Stock Ideal", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Stock Ideal − Stock Actual).
- AC-34 (RF-26): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Stock Mínimo", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Stock Mínimo − Stock Actual.
- AC-35 (RF-26): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Punto Pedido", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Punto de Pedido − Stock Actual.
- AC-36 (RF-26): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Stock Ideal", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Stock Ideal − Stock Actual.
- AC-37 (RF-26): Dado un resultado de la consulta Generar Pedido, Cuando se presiona el botón "Exportar a Excel", Entonces se descarga un archivo Excel con el contenido de la grilla.

### Registro de errores
- AC-38 (RF-27): Dado un error de ejecución en el sistema, Cuando este ocurre, Entonces sus datos quedan grabados en la tabla de errores.

### Búsqueda de Artículos
- AC-39 (RF-28): Dada una pantalla que solicita un Código de Artículo, Cuando se abre el pop up de Búsqueda de Artículos, Entonces se muestra la misma ventana modal en todas ellas, provista por un único componente compartido.
- AC-40 (RF-29): Dado el pop up de Búsqueda de Artículos abierto, Cuando se visualiza, Entonces presenta un campo "Descripción", un botón "Buscar" y una grilla con scroll vertical con las columnas Código y Descripción.
- AC-41 (RF-30): Dada la descripción "torn" ingresada en el pop up, Cuando se presiona "Buscar", Entonces la grilla muestra únicamente los artículos cuya Descripción contiene "torn" en cualquier posición (por ejemplo "Tornillo" y "Destornillador"), sin distinguir mayúsculas de minúsculas.
- AC-42 (RF-31): Dado el campo Descripción vacío, Cuando se presiona "Buscar", Entonces la grilla lista todos los artículos, sujeto al tope de AC-43.
- AC-43 (RF-32): Dado un filtro que coincide con más de 500 artículos, Cuando se presiona "Buscar", Entonces la grilla muestra los primeros 500 ordenados por Código y se despliega el aviso "Se muestran los primeros 500 resultados. Refine la descripción para acotar la búsqueda."
- AC-44 (RF-30): Dada una descripción que no coincide con ningún artículo, Cuando se presiona "Buscar", Entonces la grilla queda vacía y se muestra el mensaje "No se encontraron artículos".
- AC-45 (RF-33): Dado el pop up de Búsqueda de Artículos abierto con más filas de las que entran en pantalla, Cuando se visualiza, Entonces su alto total no supera los 600 píxeles y las filas restantes se alcanzan mediante el scroll vertical de la grilla.
- AC-46 (RF-34): Dada una grilla con resultados, Cuando se hace un clic sobre una fila, Entonces el pop up se cierra y el Código de esa fila queda cargado en el textbox de Código de Artículo que invocó la búsqueda.
- AC-47 (RF-35): Dado el pop up de Búsqueda de Artículos abierto, Cuando se cierra o se cancela sin seleccionar ninguna fila, Entonces el textbox invocador conserva su valor previo sin modificación.
- AC-48 (RF-36): Dada cualquiera de las pantallas alcanzadas (ABM de Artículos, detalle de Movimientos, parámetros Artículo Inicial y Artículo Final de la Consulta de Stock Actual), Cuando se muestra un campo Código de Artículo, Entonces a su lado hay un botón pequeño con únicamente un ícono de lupa que abre el pop up de Búsqueda de Artículos.
- AC-49 (RF-36): Dado el detalle de un Movimiento con varias líneas, Cuando se presiona la lupa de una línea determinada y se selecciona un artículo, Entonces el Código se traslada al textbox de esa línea y no al de ninguna otra.
- AC-50 (RF-36): Dada la Consulta de Stock Actual, Cuando se usa la lupa del parámetro Artículo Inicial, Entonces el Código seleccionado se carga en Artículo Inicial y el parámetro Artículo Final permanece sin cambios (y viceversa).
- AC-51 (RF-37): Dado un Código seleccionado desde el pop up en el detalle de un Movimiento, Cuando se traslada al textbox, Entonces se ejecutan las mismas operaciones asociadas que en el ingreso manual (validación de existencia, carga de la Descripción y las operaciones de cálculo de la línea).
- AC-52 (RF-38): Dado un Código de Artículo válido en el textbox, Cuando se visualiza la pantalla, Entonces la Descripción del artículo correspondiente se muestra debajo del textbox en modo solo lectura.
- AC-53 (RF-39): Dado un textbox con un Código válido y su Descripción visible, Cuando el usuario reemplaza manualmente el Código por otro Código válido, Entonces la Descripción mostrada se actualiza a la del nuevo artículo.
- AC-54 (RF-39): Dado un Código seleccionado desde el pop up de Búsqueda de Artículos, Cuando se traslada al textbox, Entonces la Descripción mostrada corresponde a ese Código.
- AC-55 (RF-40): Dado un Código inexistente ingresado manualmente, Cuando el usuario termina de editar el campo, Entonces se muestra "Artículo inexistente" en lugar de la Descripción y no se ejecutan las operaciones asociadas al código.
- AC-56 (RF-40): Dado un textbox de Código de Artículo vacío, Cuando se evalúa el campo, Entonces la Descripción queda vacía y no se muestra ningún mensaje de error.
- AC-57 (RF-41): Dado un request al endpoint de Búsqueda de Artículos sin un token JWT válido, Cuando se invoca, Entonces el sistema responde con error 401 (No autorizado) y deniega el acceso.

### Valores por defecto de los parámetros de consulta
- AC-58 (RF-42): Dado un usuario que abre la consulta "Generar Pedido", Cuando se muestra la pantalla, Entonces el parámetro "solo bajo mínimo" se presenta en No y el "Modo de Pedido" se presenta en "Hasta Stock Ideal".
- AC-59 (RF-42): Dada la consulta "Generar Pedido" recién abierta con sus valores por defecto, Cuando se ejecuta sin modificarlos, Entonces el resultado corresponde al cálculo de AC-33 (Cantidad a Pedir = MAX(0, Stock Ideal − Stock Actual) para todos los artículos).
- AC-60 (RF-43): Dado un catálogo con artículos, Cuando el usuario abre la "Consulta de Stock Actual", Entonces el parámetro Código desde se presenta con el primer Código del catálogo y el parámetro Código hasta con el último, según el orden ascendente de Código.
- AC-61 (RF-43): Dada la "Consulta de Stock Actual" recién abierta con sus valores por defecto, Cuando se ejecuta sin modificarlos, Entonces el resultado incluye la totalidad de los artículos del catálogo.
- AC-62 (RF-44): Dada la "Consulta de Stock Actual" recién abierta con sus valores por defecto, Cuando se muestra la pantalla, Entonces debajo de cada uno de los dos parámetros se ve ya resuelta la Descripción del artículo correspondiente.
- AC-63 (RF-44): Dado un catálogo de artículos vacío, Cuando el usuario abre la "Consulta de Stock Actual", Entonces Código desde y Código hasta quedan vacíos, sin Descripción y sin mensaje de error.
- AC-64 (RF-45): Dada cualquiera de las dos consultas abierta con sus valores por defecto, Cuando el usuario modifica uno o más parámetros y ejecuta la consulta, Entonces esta se ejecuta con los valores elegidos por el usuario y no con los sugeridos.

### Carga de líneas de detalle de Movimientos
- AC-65 (RF-46): Dada la pantalla de carga de Movimientos, Cuando el usuario presiona el botón "Agregar Línea", Entonces se agrega al detalle una nueva línea vacía con sus campos Código, Cantidad, Precio Unitario y Precio Total, su botón de lupa y su lugar para la Descripción.
- AC-66 (RF-46): Dada una línea recién agregada con "Agregar Línea", Cuando el usuario ingresa un Código en ella (manualmente o desde el pop up de búsqueda), Entonces la Descripción se resuelve y las validaciones se aplican igual que en cualquier otra línea del detalle.
- AC-67 (RF-47): Dado un Movimiento en carga, Cuando el usuario presiona "Agregar Línea" repetidas veces, Entonces el sistema agrega una línea por cada pulsación, sin imponer un tope a la cantidad de líneas ni mostrar mensajes de límite alcanzado.
- AC-68 (RF-48): Dado un Movimiento con varias líneas agregadas mediante "Agregar Línea" y completadas correctamente, Cuando se graba, Entonces todas ellas quedan persistidas en la tabla de detalle.
- AC-69 (RF-48): Dado un Movimiento con una línea agregada mediante "Agregar Línea" cuya Cantidad es 0, negativa o no entera, Cuando se intenta grabar, Entonces el sistema rechaza la operación y no graba el movimiento (RF-23).
- AC-70 (RF-49): Dado un detalle de Movimiento con varias líneas cargadas, Cuando el usuario presiona "Eliminar Línea" en una de ellas, Entonces esa línea desaparece de la grilla y las demás conservan sus datos sin alteración.
- AC-71 (RF-50): Dada una línea agregada con "Agregar Línea" y todavía no grabada, Cuando se elimina y luego se graba el Movimiento, Entonces esa línea no queda persistida en la tabla de detalle.
- AC-72 (RF-50): Dado un Movimiento existente que se está modificando, Cuando se elimina una de sus líneas ya persistidas y se graba el Movimiento, Entonces esa línea deja de existir en la tabla de detalle y las restantes quedan persistidas.
- AC-73 (RF-51): Dado un Movimiento sin ninguna línea de detalle, ya sea porque se eliminaron todas o porque no se agregó ninguna, Cuando se intenta grabar, Entonces el sistema rechaza la operación, muestra un mensaje de error y no graba el movimiento.
- AC-74 (RF-51): Dado un Movimiento del que se eliminaron algunas líneas pero que conserva al menos una línea válida, Cuando se graba, Entonces la operación se acepta y quedan persistidas únicamente las líneas conservadas.
- AC-75 (RF-52): Dada una línea de detalle con datos cargados en alguno de sus campos, Cuando el usuario presiona "Eliminar Línea", Entonces el sistema pide confirmación antes de eliminarla.
- AC-76 (RF-52): Dada la confirmación pedida por AC-75, Cuando el usuario confirma, Entonces la línea se elimina del detalle.
- AC-77 (RF-52): Dada la confirmación pedida por AC-75, Cuando el usuario cancela, Entonces la línea permanece en el detalle con todos sus datos sin modificación.
- AC-78 (RF-52): Dada una línea de detalle completamente vacía, Cuando el usuario presiona "Eliminar Línea", Entonces la línea se elimina directamente, sin pedir confirmación.

### Total del detalle de Movimientos
- AC-79 (RF-53): Dada la pantalla de carga de Movimientos con líneas cargadas, Cuando se visualiza, Entonces al pie de la columna Precio Total se muestra un total igual a la suma del Precio Total de todas las líneas de la grilla.
- AC-80 (RF-54): Dado un detalle con líneas cargadas, Cuando el usuario agrega una línea con "Agregar Línea" y la completa, Entonces el total pasa a incluir el Precio Total de la nueva línea sin necesidad de grabar el Movimiento.
- AC-81 (RF-54): Dado un detalle con líneas cargadas, Cuando el usuario elimina una línea, Entonces el total se actualiza restando el Precio Total de la línea eliminada, sin necesidad de grabar el Movimiento.
- AC-82 (RF-54): Dada una línea del detalle ya cargada, Cuando el usuario modifica su Cantidad o su Precio Unitario, Entonces el total se actualiza al nuevo valor sin necesidad de grabar el Movimiento ni de refrescar la pantalla.
- AC-83 (RF-54): Dado un Movimiento existente que se abre para modificar, Cuando se muestra la pantalla, Entonces el total refleja la suma de las líneas persistidas; y a medida que el usuario agrega, elimina o modifica líneas, el total pasa a reflejar el contenido actual de la grilla y no el contenido persistido.
- AC-84 (RF-55): Dado un detalle sin ninguna línea, Cuando se visualiza la pantalla, Entonces el total se muestra en 0.
- AC-85 (RF-55): Dada una línea incompleta cuyo Precio Total todavía no está informado, Cuando se calcula el total, Entonces esa línea computa como 0 y el total se sigue mostrando con el aporte de las líneas restantes.

### Precio Total de la línea de detalle
- AC-86 (RF-56): Dada una línea de detalle con Cantidad = 3 y Precio Unitario = 250, Cuando se calcula la línea, Entonces su Precio Total es 750.
- AC-87 (RF-57): Dada una línea con su Precio Total ya calculado, Cuando el usuario modifica la Cantidad o el Precio Unitario, Entonces el Precio Total de esa línea se recalcula en el acto, sin necesidad de grabar el Movimiento ni de refrescar la pantalla.
- AC-88 (RF-57): Dada la grilla del detalle de un Movimiento, Cuando el usuario intenta editar directamente el campo Precio Total de una línea, Entonces el campo no admite edición.
- AC-89 (RF-58): Dada una línea cuya Cantidad o Precio Unitario todavía no están informados, Cuando se visualiza la grilla, Entonces su Precio Total se muestra en 0.
- AC-90 (RF-56): Dada una línea cuyo Precio Total se recalcula por un cambio de Cantidad o Precio Unitario, Cuando ocurre el recálculo, Entonces el total del detalle (RF-53) queda actualizado en la misma acción.

### Sugerencia del Precio Unitario
- AC-91 (RF-59): Dado un Movimiento cuyo Tipo de Movimiento es compra, Cuando se informa el Código de Artículo de una línea del detalle, Entonces el Precio Unitario de esa línea se sugiere con el Precio de Costo del artículo.
- AC-92 (RF-59): Dado un Movimiento cuyo Tipo de Movimiento es venta, Cuando se informa el Código de Artículo de una línea del detalle, Entonces el Precio Unitario de esa línea se sugiere con el Precio de Venta del artículo.
- AC-93 (RF-59): Dado un Código seleccionado desde el pop up de Búsqueda de Artículos, Cuando se traslada a la línea del detalle, Entonces el Precio Unitario se sugiere igual que si el Código se hubiese tecleado manualmente (RF-37).
- AC-94 (RF-60): Dada una línea con un Código cargado y su Precio Unitario ya sugerido, Cuando el usuario reemplaza el Código por el de otro artículo, Entonces el Precio Unitario se vuelve a sugerir con el precio correspondiente al nuevo artículo.
- AC-95 (RF-61): Dada una línea con Precio Unitario sugerido, Cuando el usuario lo reemplaza manualmente por otro valor y graba el Movimiento, Entonces queda persistido el valor ingresado por el usuario y no el sugerido.
- AC-96 (RF-61): Dada una línea con Precio Unitario sugerido, Cuando el usuario lo modifica, Entonces el Precio Total de esa línea y el total del detalle se recalculan con el nuevo valor (RF-56, RF-54).
- AC-97 (RF-62): Dada una línea cuyo Código de Artículo está vacío o no corresponde a ningún artículo existente, Cuando se evalúa el campo, Entonces no se sugiere Precio Unitario y el campo queda en 0.
- AC-98 (RF-63): Dado un Movimiento con líneas ya cargadas en el detalle, Cuando el usuario cambia el Tipo de Movimiento del encabezado, Entonces el sistema pide confirmación antes de re-sugerir los Precios Unitarios.
- AC-99 (RF-63): Dada la confirmación pedida por AC-98, Cuando el usuario confirma, Entonces el Precio Unitario de todas las líneas se vuelve a sugerir según el nuevo Tipo de Movimiento y el total del detalle queda actualizado.
- AC-100 (RF-63): Dada la confirmación pedida por AC-98, Cuando el usuario cancela, Entonces el Tipo de Movimiento vuelve a su valor previo y todas las líneas conservan sus Precios Unitarios sin modificación.

### Rendimiento (verificación de RNF)
- AC-101 (RNF-04): Dado un catálogo de 10000 artículos, Cuando se ejecutan las consultas "Consulta de Stock Actual" y "Generar Pedido", Entonces cada una responde en menos de 3 segundos (p95).
- AC-102 (RNF-07): Dado un catálogo de 10000 artículos y el campo Descripción vacío, Cuando se presiona "Buscar" en el pop up de Búsqueda de Artículos, Entonces el resultado se muestra en menos de 2 segundos (p95).

## Fuera de Alcance
- Queda fuera de alcance la carga de proveedores (Alta, Baja y Modificación).
- Queda fuera de alcance el manejo de múltiples proveedores por artículo.
- Queda fuera de alcance la generación de órdenes de compra.
- Queda fuera de alcance definir permisos de acceso por perfil para las pantallas distintas de la carga de usuarios: todo usuario autenticado (RF-12) puede acceder a las demás funcionalidades; la única restricción por perfil es la de RF-10.
- Queda fuera de alcance buscar artículos por Código dentro del pop up de Búsqueda de Artículos: el único criterio de filtrado es la Descripción (RF-30).
- Queda fuera de alcance la selección múltiple de artículos en el pop up de Búsqueda de Artículos: cada invocación devuelve un solo Código (RF-34).
- Queda fuera de alcance el paginado y el ordenamiento configurable por columna en la grilla del pop up de Búsqueda de Artículos: el resultado se acota con el tope de RF-32 y se ordena siempre por Código.

## Riesgos y Dependencias
- Riesgo: Que haya más de 10000 artículos. Mitigación: limitar las consultas con TOP 10000 y agregar filtro opcional por descripción con LIKE '%%'.
- Riesgo: Que la búsqueda con Descripción vacía sobre un catálogo grande degrade la respuesta del pop up. Mitigación: tope de 500 filas con aviso al usuario (RF-32) e índice sobre la Descripción del artículo.
- Riesgo: Que la resolución de la Descripción a partir del Código (RF-39) genere un llamado a la API por cada tecla, especialmente en el detalle de Movimientos con muchas líneas. Mitigación: resolver la Descripción al salir del campo (o con debounce) en lugar de por cada pulsación.
- Riesgo: Que el rango por defecto de la "Consulta de Stock Actual" (RF-43) haga que la consulta se ejecute siempre sobre la totalidad del catálogo, agravando el escenario de peor caso de RNF-04. Mitigación: el tope de TOP 10000 ya previsto y la verificación explícita de AC-101.
- Dependencia: ninguna.
