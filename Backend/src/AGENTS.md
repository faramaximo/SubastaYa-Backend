En la clase el profe dijo esto: 

"
Aplicacion valida

Dominio se usa en las 4
1 controlador por cada recurso.

Pujas
Subasta
 1 controller por entidad.


Excepciones

Midware arriba de la capa de presentacion, atrapa todos los errores de controladores.


Program.cs

Todas las configuraciones de la aplicacion, tipo de inyecion de dependias. 

Para esta interfaz esta clase. 


Capa de aplicacion
Comand: escritura

Querys: consulta

Hunder: manejadore, excepciones, ofertar una subasta wue esta cerrada. Maneja una query o un comand.
Dominio metemos excepsiones de negocio. 

Int + texto no es de negocio, se tira por c#

Subasta vencida es de negocio. Pura y exclusivamente priblema de software.
Concurrencia atributo en la identidad, 

Db context le decimos
Date time

Utc.add
 Reposoty:
 Inyecion de dependcias
Cors validacion que hace ek navegador"

La consigna del proyecto es: 

"
Trabajo Práctico
"SubastaYa" es una plataforma web de subastas en tiempo real y comercio electrónico que
busca modernizar las compras y ventas competitivas en línea.

El modelo de negocio se apoya en dos pilares críticos:
1. Confianza y Solvencia Económica: En las plataformas tradicionales, los usuarios
suelen ofertar sin tener fondos reales, generando transacciones canceladas y
fricción con los vendedores. En SubastaYa, toda puja debe estar respaldada por
saldo real congelado en garantía (Escrow) dentro de la billetera virtual del
usuario.

2. Juego Limpio (Fair Play en Subastas): Muchos sistemas sufren de sniping (bots o
usuarios que ofertan en el último milisegundo para ganar sin dar tiempo a
respuesta). Para evitarlo, la plataforma incorpora un mecanismo de extensión
dinámica de tiempo (Anti-sniping) y una interfaz altamente reactiva donde el
usuario tiene visibilidad total del estado de las ofertas en vivo.

El objetivo del equipo de desarrollo es construir una solución web completa (Frontend,
Backend y Base de Datos) que ofrezca una experiencia visual atractiva y fluida para
compradores y vendedores, respaldada por un motor transaccional seguro y confiable.

1. Requerimientos Funcionales y Experiencia de Usuario (Frontend &
Flujo)
La aplicación web debe contar con una interfaz responsiva, intuitiva y visualmente cuidada.
Se espera que los alumnos desarrollen una experiencia de usuario interactiva dividida en
los siguientes módulos:

Módulo 1: Catálogo y Exploración de Subastas
● Filtros y Búsqueda: El usuario debe poder explorar subastas filtrando por:

○ Estado: Activas (en curso), Próximas (programadas para iniciar a futuro) y
Finalizadas.

○ Categoría: Ej. Tecnología, Coleccionables, Vehículos, Arte, etc.

○ Rango de precios y ordenamiento (por menor tiempo restante o mayor
puja).


● Cards de Producto Informativas: Cada tarjeta en el catálogo debe mostrar imagen
referencial, título, categoría, oferta más alta actual, cantidad de ofertas realizadas y
un contador regresivo visible.

Módulo 2: Creación y Publicación de Subastas (Vendedor)

● Formulario Interactivo de Publicación:

○ Datos del producto: Título, descripción detallada, URL de imagen y categoría.

○ Configuración económica: Precio base inicial y monto mínimo de incremento
por puja.

○ Ventana temporal: Fecha/hora de inicio y fecha/hora de finalización.

● Validaciones en pantalla: La fecha de finalización debe ser posterior a la de inicio;
el incremento mínimo y el precio base deben ser valores positivos coherentes.


Módulo 3: Sala de Subasta en Vivo (Live Bidding Room)
Es la vista principal de interacción en tiempo real:


● Temporizador en Vivo: Reloj visual con cuenta regresiva. Si el tiempo entra en
zona crítica (último minuto), debe cambiar de color (ej. amarillo/rojo) para alertar a
los postores.

● Historial de Ofertas en Pantalla: Lista cronológica con las últimas pujas (monto,
usuario anonimizado/seudónimo y hora exacta de la oferta).

● Consola de Puja Dinámica:

○ Sugerencia automática del próximo valor a ofertar: $\text{Puja Actual} +
\text{Incremento Mínimo}$.
○ Posibilidad de ingresar un monto personalizado superior.
○ Indicador visual inmediato de estado:

■ Liderando: Si la oferta más alta pertenece al usuario actual.
■ Superado (Outbid): Si otro usuario acaba de enviar una oferta
superior.

● Feedback y Alertas: Mensajes visuales claros (toasts / modales) cuando se
confirma una puja, cuando los fondos son insuficientes o si el tiempo de la subasta
fue extendido por regla anti-sniping.


Módulo 4: Billetera Virtual y Gestión de Fondos

● Panel de Saldo: Visualización clara de tres métricas financieras:

1. Saldo Total: Fondos totales depositados en la cuenta.

2. Saldo Retenido / En Garantía: Monto actualmente bloqueado en subastas
activas donde el usuario es el postor líder.

3. Saldo Disponible: $\text{Saldo Total} - \text{Saldo Retenido}$ (único dinero
disponible para nuevas pujas o retiros).

● Carga de Saldo Simulada: Formulario para acreditar saldo ficticio a la billetera.

● Historial de Movimientos: Tabla con el detalle de ingresos, retenciones por ofertas,
liberaciones por superación y débitos finales por subastas ganadas.


Módulo 5: Panel de Usuario ("Mis Actividades")

● Pestaña "Mis Compras / Pujas": Listado de subastas donde participó, indicando si
ganó el producto o si la subasta sigue abierta.

● Pestaña "Mis Publicaciones": Subastas creadas por el vendedor, con métricas de
recaudación y estado de adjudicación.


2. Lógica de Dominio y Reglas del Negocio (Backend & Procesos)
El backend debe garantizar la integridad de las transacciones y la correcta aplicación de las
reglas del negocio:

2.1 Manejo Atómico de Saldos (Garantía / Escrow)

● Cuando un usuario realiza la puja más alta, su saldo disponible pasa a estar
retenido por el valor de su oferta.

● Si un segundo usuario supera esa oferta válidamente:

○ El sistema debe liberar de forma automática e inmediata la retención del
primer usuario, devolviéndole su saldo disponible.

○ Se congela el saldo correspondiente al segundo usuario.

● Ambas operaciones (liberación del anterior y bloqueo del nuevo) deben ejecutarse
en un bloque transaccional atómico.

2.2 Regla Anti-Sniping (Extensión Automática de Tiempo)

● Si una oferta válida se registra dentro de los últimos 60 segundos previos al cierre
de la subasta, el sistema debe extender automáticamente la fecha de finalización por
2 minutos adicionales.

● Esto permite que los demás participantes tengan tiempo de reaccionar y evita
victorias injustas por latencia de red.

2.3 Adjudicación y Procesos en Segundo Plano (Background Worker)

● Debe existir un proceso en segundo plano (Worker / Tarea programada / Cron job)
que verifique periódicamente las subastas cuyo tiempo haya finalizado:


○ Con ganador: Marca la subasta como FINALIZADA, transfiere el saldo
retenido de la billetera del comprador a la del vendedor (liquidación final) y
registra la venta.

○ Sin ofertas: Si vence sin ninguna puja registrada, pasa a estado DESIERTA.


3.4 Auditoría de Eventos y Trazabilidad (Audit Log) Más allá del registro contable
(Ledger) y el historial de pujas, el sistema debe registrar de forma inmutable los eventos
críticos del negocio y del sistema en una tabla de auditoría. Se deben auditar
obligatoriamente:

● Cambios de estado de las subastas (ej. paso de ACTIVA a FINALIZADA ejecutado
por el Worker).

● Extensiones de tiempo gatilladas por la regla Anti-Sniping.

● Intentos de puja rechazados por concurrencia o validaciones de negocio críticas.

● Acreditaciones manuales de saldo en las billeteras.

3. Lineamientos y Restricciones Técnicas
El equipo asumirá el rol de Arquitectos de Software. No se proveerá un diagrama de base
de datos ni un archivo OpenAPI predefinido. El equipo debe diseñar estas estructuras y
justificar sus decisiones basándose en los siguientes estándares de la industria:

3.1 Backend y Base de Datos
● Stack Tecnológico: Arquitectura API REST utilizando alguno de:
○ C# (EF Core)
○ Java (Spring/Hibernate),
○ Python (SQLAlchemy),
○ Go (GORM)
○ Node.js (Sequelize/TypeORM).

● Code-First: El esquema relacional debe generarse obligatoriamente mediante
migraciones en el código.

● Preparación para Concurrencia: Es mandatorio incorporar mecanismos en el
modelo (ej. un campo Version) para soportar Optimistic Locking.


● Transaccionalidad (ACID): Las operaciones críticas deben ejecutarse bajo un
modelo transaccional. Ante cualquier fallo en la actualización de registros, estados o
logs de auditoría, se debe garantizar un Rollback completo para asegurar la
integridad de los datos.

● Estándares RESTful: Diseño de endpoints basados exclusivamente en sustantivos
plurales y jerarquías de recursos (ej. GET
/api/v1/recursos/{id}/subrecursos). Se prohíbe el uso de verbos en la
definición de las URLs. Asimismo, se debe garantizar el uso correcto de códigos de
estado HTTP para reflejar el resultado de la operación y un manejo de errores
preciso (ej. retornar 409 Conflict ante problemas de concurrencia o conflictos de
estado, en lugar de errores genéricos 500).

● Documentación: La API debe autogenerar su documentación utilizando OpenAPI
(Swagger UI).

● Comunicación en Tiempo Real (Sala de Subastas): Para garantizar una
experiencia fluida y justa en la vista de subasta en vivo, se espera que el
temporizador y las nuevas ofertas se sincronicen utilizando WebSockets (ej.
SignalR en .NET, Socket.io en Node, Spring WebSockets). Como alternativa mínima
aceptable, se permitirá la técnica de Short-Polling (consultas periódicas asíncronas
desde el frontend cada 2-3 segundos), aunque la implementación exitosa de
WebSockets será un fuerte diferencial en la nota de arquitectura.


3.2 Diagrama de base de datos sugerido



Diagrama sugerido de base de datos


3.3 Datos Semilla Obligatorios (Seed Data)

● 4 Usuarios y Billeteras:

○ vendedor@test.com: Creador de publicaciones (Saldo $0).

○ comprador1@test.com: Postor líder (Total: $150.000 / Retenido: $45.000 /
Disp: $105.000).

○ comprador2@test.com: Postor habilitado (Total: $200.000 / Disp:
$200.000).

○ sinfondos@test.com: Sin saldo (Total: $500, para probar rechazo de puja).

● 4 Categorías:

○ Tecnología, Coleccionables, Indumentaria, Vehículos.


● 5 Subastas (Casos de Prueba):

○ Activa estándar: Cierra en 20-30 min (con 2 pujas previas cargadas; líder
$45.000).

○ Activa crítica: Cierra en menos de 2 min (para probar alerta visual y
extensión anti-sniping).

○ Próxima: Inicio programado a +24 hs (pujas bloqueadas).
Cátedra: Proyecto de Software

○ Vencida con ganador: Fecha fin pasada + puja ganadora (para probar cierre
y liquidación del worker).

○ Vencida desierta: Fecha fin pasada sin pujas (para probar pase a
DESIERTA).

● Registros Contables y Pujas:

○ Historial de las 2 ofertas previas en la subasta activa.

○ Transacciones en el libro mayor (Ledger) que respalden depósitos y el saldo
retenido de $45.000.

3.4 Diseño de la API REST

El equipo definirá la totalidad de las rutas, parámetros y DTOs requeridos. A modo de
referencia de diseño, el backend deberá contemplar endpoints similares a los siguientes
(pudiendo adaptarlos o ampliarlos):

Recurso / Operación
Sugerida
Propósito de Referencia

GET /api/auctions Listado de subastas con paginación y filtros.

POST /api/auctions Creación y parametrización de una nueva subasta.

GET /api/auctions/{id} Detalle completo de la subasta, estado y puja actual.

POST /api/auctions/{id}/bids Registro de una nueva oferta (evalúa saldo,
incremento y anti-sniping).

GET /api/wallet/balance Consulta del desglose de saldos (Total, Retenido,
Disponible).

POST /api/wallet/deposit Acreditación de fondos simulados en la billetera.


3.5 Frontend e Interfaz de Usuario (UI/UX)

● Tecnologías: Se permite el uso de HTML/CSS/JS (Vanilla) o frameworks modernos
(React). Se recomienda el uso de librerías de estilos (Bootstrap, Tailwind, etc.) para
asegurar un diseño limpio.

● Integración: El frontend debe consumir la API REST desarrollada en el backend de
forma asíncrona (usando Fetch API o Axios).

● Experiencia de Usuario (UX/UI): Se requiere un diseño de interfaz de alta calidad,
intuitivo y centrado en el usuario. La solución debe priorizar la claridad visual y la
consistencia operativa, garantizando una retroalimentación constante y precisa ante
cada interacción. El sistema debe asegurar que el usuario tenga plena visibilidad del
estado de los procesos, proporcionando confirmaciones claras y una navegación
fluida en todo momento.

○ EJ: Mostrar spinners o indicadores de carga mientras el backend responde.

○ EJ: Mostrar alertas claras de éxito o error

● Validaciones: Se debe prevenir el envío de peticiones innecesarias al backend (ej.
deshabilitar el botón de un asiento que ya se muestra como ocupado).

3.6 Calidad de Código general
● Clean Code: Nomenclatura descriptiva, funciones pequeñas con responsabilidad
única y comentarios enfocados en el "por qué" y no en el "qué".

● Arquitectura: Separación clara de responsabilidades (Controladores,
Servicios/Casos de Uso, Repositorios). Evitar lógica de negocio acoplada a las
vistas o a los controladores HTTP.


4. Pautas de Entrega y Evaluación
4.1 Modalidad de Entrega
● Repositorio de Código: La entrega del proyecto (tanto de la Entrega 1 como de la
Entrega 2) se realizará exclusivamente a través de un repositorio público en GitHub.

● El repositorio debe contener tanto el código del Backend como el del Frontend.

● Se debe incluir un archivo README.md detallado que explique los pasos necesarios
para compilar el proyecto, levantar la base de datos, ejecutar las migraciones y
lanzar la aplicación.

○ Prueba de Concurrencia (Stress Test): El documento debe incluir una
explicación breve (puede ser un bloque de código, un script de bash con
curl, o exportación de Postman/JMeter) que demuestre cómo el equipo
testeó la concurrencia optimista. Se debe probar que si se envían dos
peticiones de puja idénticas en el mismo milisegundo, la base de datos
registra solo una y rechaza la otra (ej. retornando HTTP 409 Conflict).

● Se debe enviar el enlace del repositorio al docente a través del campus virtual antes
de la fecha y hora de cierre estipulada. (Si el repositorio es privado, se debe invitar al
docente como colaborador).

4.2 Criterios de Evaluación
El proyecto se evaluará sobre una nota máxima de 10 puntos, distribuidos en los siguientes
ejes:

1. Arquitectura y Base de Datos [2 pts]:
○ Correcta normalización e integridad del esquema relacional.
○ Implementación exitosa del enfoque Code-First.
○ Manejo adecuado de la concurrencia (Optimistic Locking) y transaccionalidad
(ACID) en operaciones críticas.

2. Estándares API REST y Lógica de Negocio [3 pts]:
○ Cumplimiento estricto de las convenciones RESTful (URLs, Verbos HTTP).
○ Uso correcto de códigos de estado HTTP (ej. 409 Conflict, 400 Bad
Request, 200 OK).
○ Resolución correcta de las reglas de negocio (procesos en segundo plano,
validación de caducidad).

3. Calidad de Código (Clean Architecture & SOLID) [2 pts]:
○ Separación clara de responsabilidades en el backend.
○ Código limpio, legible y modular.

4. Frontend y UX [3 pts]:
○ Interfaz intuitiva y funcional.
○ Correcto manejo asíncrono de las peticiones a la API.
○ Retroalimentación visual adecuada frente a errores (especialmente fallas de
concurrencia) y estados de carga.

4.3 Auditoría de Trabajo y Calificación Individual (IMPORTANTE)
El desarrollo de software es un esfuerzo colaborativo, pero la evaluación y calificación final
será estrictamente individual.

● Análisis de Repositorio: El docente auditará el historial de commits, los Pull
Requests y la pestaña de Insights/Contributors del repositorio de GitHub para
evaluar el nivel de actividad, la frecuencia de los aportes y la calidad del código
subido por cada uno de los integrantes del equipo.

● Defensa del Proyecto: Tras la entrega, el equipo realizará una defensa oral del
trabajo práctico. Durante esta instancia, el docente podrá realizar preguntas técnicas
sobre cualquier segmento del código, decisiones de diseño de la base de datos o
arquitectura de la API a cualquier miembro del equipo.

● Notas Dispares: Si se evidencia una participación desigual en el desarrollo del
proyecto (reflejada en el repositorio) o si un integrante no logra justificar las
decisiones de implementación durante la defensa oral, el docente asignará notas
dispares a los miembros del equipo, pudiendo resultar en la aprobación de un
alumno y la desaprobación de otro dentro del mismo grupo.


RECURSOS:
1. Tutorial API Net Core:
https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore
-3.1&tabs=visual-studio
2. Tutorial API Spring Boot:
https://www.nigmacode.com/java/Crear-API-REST-con-Spring
3. Tutorial API Flask: https://flask-restful.readthedocs.io/en/latest/quickstart.html
4. Tutorial Go:
https://blog.devgenius.io/simple-rest-service-in-golang-with-openapi-spec-and-orm-a
447b1086e21
5. Tutorial Node JS:
https://www.freecodecamp.org/news/how-to-build-explicit-apis-with-openapi
6. Tutorial HTML : https://www.w3schools.com/html/default.asp
7. Tutorial CSS : https://www.w3schools.com/css/default.asp
8. Tutorial JS: https://www.w3schools.com/js/default.asp
9. Framework de UI:
a. Bootstrap: https://getbootstrap.com/
b. Foundation: https://get.foundation/sites.html
c. Pure: https://purecss.io/
d. Skeleton: http://getskeleton.com/
"

La guia del profesor que usa otra app de ejemplo es: 

"IA, Datos y Arquitectura

Proyecto de Software · Clase 4

IA, Datos y Arquitectura

Cómo se programa hoy con asistentes de IA, cómo se diseña la base de datos que hay debajo, y cómo se ordena el código que las une.

Prof. Leonardo Julián Cabral

.NET · EF Core · SQL Server

Agenda

Qué vamos a ver hoy

01 — IA para programar: herramientas y metodología — 65'

02 — Bases de datos y SQL — 30'

03 — ORM y Entity Framework Core — 40'

04 — Principios SOLID — 30'

05 — Layers Pattern e Inyección de Dependencias — 25'

06 — Repository, Unit of Work y CQRS — 30'

El caso que nos acompaña toda la clase

Sistema de Pedidos

Una tienda necesita registrar pedidos. Un Cliente hace muchos Pedidos. Cada Pedido tiene varios Ítems, y cada Ítem referencia un Producto con cantidad y precio.

Al confirmar un pedido hay que:

* Verificar y descontar stock
* Calcular el total
* Aplicar descuentos según el tipo de cliente
* Notificar al cliente por mail

Todo lo que veamos hoy — herramientas de IA, base de datos, ORM, capas, patrones — lo vamos a ver sobre este mismo ejemplo.

PARTE 01

IA para programar

Qué herramientas existen, en qué se diferencian y cómo se trabaja bien con ellas

El punto de partida

Esto ya no es opcional

La discusión sobre si usar IA para programar está terminada: se usa. La discusión que importa 
ahora es cómo, porque la diferencia entre usarla bien y usarla mal es enorme — y se nota en el código.

Lo que cambia

* Escribir código deja de ser el cuello de botella
* El costo de explorar tres alternativas baja mucho
* Entrar a un proyecto ajeno es más rápido

Lo que no cambia

* Decidir qué construir y cómo estructurarlo
* Saber si lo que te devolvió está bien
* La responsabilidad sobre lo que sale a producción

El panorama · quién fabrica los modelos

Los proveedores

OpenAI
GPT · Codex

Anthropic
Claude

Google
Gemini

Microsoft
Azure AI Foundry

Meta
Llama · pesos abiertos

Mistral
Codestral · europeo

Amazon
Bedrock · Nova

Modelos abiertos
DeepSeek · Qwen · Kimi

Distinción útil para un proyecto: los modelos los fabrican estas empresas; 
las herramientas que usás todos los días son otra cosa, y muchas veces son de un 
tercero que consume el modelo por API. Cambiar de modelo detrás de la misma herramienta es habitual.

El panorama · qué usás vos

Las herramientas, por categoría

Chat

ChatGPT
OpenAI

Claude
Anthropic

Gemini
Google

Le Chat
Mistral

Copiloto en el editor

GitHub Copilot
VS Code · Visual Studio

Cursor Tab
editor propio

JetBrains AI
Rider · IntelliJ

Amazon Q
AWS

Agente

Claude Code
terminal · IDE

Cursor Agent
Composer

Codex
OpenAI

Windsurf
Cascade

También en el mapa:

Copilot Agent

Gemini Code Assist

v0

Devin

Junie

La distinción que hay que entender

Chat, copiloto y agente no son tres marcas: son tres formas distintas de trabajar.

Cambia qué ve la herramienta, cuánto hace sola y cómo la verificás.

Comparación

Las tres categorías, lado a lado

Dónde vive | Chat | Copiloto | Agente

Dónde vive:

* El navegador, fuera del proyecto
* Dentro del editor
* Editor o terminal, con acceso al repo

Qué ve:

* Solo lo que vos le pegás
* El archivo abierto y algunos vecinos
* El proyecto entero: archivos, tests, git

Qué hace:

* Responde texto
* Completa mientras escribís
* Lee, edita varios archivos, ejecuta comandos e itera

Unidad de trabajo:

* Una pregunta
* Una línea o un bloque
* Una tarea completa

Quién ejecuta:

* Vos, copiando a mano
* Vos, aceptando la sugerencia
* La herramienta, en tu máquina

Cómo se verifica:

* Lo leés antes de copiar
* Aceptás o rechazás cada sugerencia
* Revisás el diff y corrés los tests

Riesgo:

* Bajo: no toca nada
* Medio: entra código sin pensarlo
* Alto: modifica y ejecuta

Mejor para:

* Entender, diseñar, comparar alternativas
* Boilerplate y código repetitivo
* Refactors, features completas, bugs con reproducción

Criterio

Cuál usar en cada momento del proyecto

Chat

Cuando todavía no sabés qué hacer. Discutir el modelo de datos, comparar tres 
formas de resolver los descuentos, entender un error que nunca viste.

No toca tu código: podés equivocarte gratis

Copiloto

Cuando ya sabés exactamente qué escribir y es tedioso. Los DTOs, el mapeo, los using, el 
test número catorce que es igual al trece.

Cuidado: acepta rápido y mete cosas que no leíste

Agente

Cuando la tarea toca varios archivos y tenés forma de verificarla: un refactor, 
migrar un patrón, hacer pasar un test que falla.

Necesita que sepas revisar un diff

El error más común de un estudiante

Usar el agente para lo que no entiende. El agente amplifica tu criterio: si no tenés criterio, 
amplifica el vacío y te deja 600 líneas que no podés defender.

Lo mínimo que hay que saber del motor

Dos hechos con consecuencias prácticas

1 · Predice, no consulta

El modelo genera el texto más probable a continuación. No ejecuta lo que escribe ni verifica que exista.

Consecuencia

Puede inventar un método, un paquete NuGet o una propiedad de EF Core que no existe. 
Se llama alucinación y es más frecuente cuanto más específica y menos común es tu pregunta.

2 · Solo sabe lo que tiene a la vista

La ventana de contexto es todo lo que puede mirar a la vez: tu pregunta, la conversación y los archivos que recibió.

Consecuencia

Si tu proyecto no está en el contexto, el modelo no lo conoce — y va a inventar nombres de clases razonables que no son los tuyos.

Las dos consecuencias tienen la misma solución, y es el concepto más importante de este bloque.

El concepto central

Grounding: anclar al modelo en la verdad de tu proyecto.

En vez de dejar que recuerde, mostrale. Casi todo error de un asistente es un error de grounding.

Grounding

Qué significa en la práctica

Grounding es darle al modelo fuentes de verdad verificables en lugar de confiar en lo que aprendió durante el entrenamiento.

Sin grounding

«Hacé un repositorio de pedidos con EF Core.»

Inventa una entidad Order con propiedades que no son las tuyas, usa una versión de API que quizá no corresponde y devuelve algo que casi anda.

Con grounding

«Este es Pedido.cs, este es AppDbContext.cs, y así se ve IProductoRepository. Hacé IPedidoRepository siguiendo el mismo patrón.»

Devuelve código que compila contra tu proyecto.

Grounding

Las seis fuentes de verdad que tenés a mano

* El código real. Los archivos relevantes, pegados o referenciados con @archivo. La forma más directa y la más efectiva.
* El índice del repositorio. Cursor y los agentes indexan el proyecto y buscan solos los archivos que necesitan. Vos igual conviene que les digas por dónde empezar.
* El compilador y los tests. Pegar el error exacto — no «me tira un error» — o dejar que el agente corra dotnet build y lea la salida.
* El esquema real de la base. El DDL o el DbContext, no tu descripción de memoria de las tablas.
* La documentación de la versión que usás. EF Core 6, 8 y 9 tienen diferencias reales; el modelo mezcla versiones si no le aclarás.
* MCP — Model Context Protocol. Un estándar para conectar el asistente a sistemas reales: la base de datos, el gestor de tickets, la documentación interna. Grounding automatizado en lugar de copiar y pegar.

Grounding · código

El archivo de reglas del proyecto

Casi todas las herramientas leen un archivo de convenciones del repositorio y lo aplican a cada pedido. Es grounding permanente: se escribe una vez y sirve para todo el equipo.

# AGENTS.md · en la raíz del repositorio

# (Cursor lo llama .cursorrules · Claude Code, CLAUDE.md · Copilot,

# .github/copilot-instructions.md — el contenido es el mismo)

## Stack

.NET 8, EF Core 8, SQL Server. Tests con xUnit y NSubstitute.

## Arquitectura

Solución en capas: Presentation / Application / Domain / Infrastructure.

* Domain no referencia a ningún otro proyecto.
* La lógica de negocio va en Application, NUNCA en el controller.
* El acceso a datos pasa siempre por un repositorio, nunca DbContext directo.
* Hacia afuera viajan DTOs, jamás entidades del dominio.

## Convenciones

* Nombres de entidades y propiedades en español (Pedido, Cliente, Stock).
* Métodos async terminan en Async y devuelven Task.
* Las consultas de solo lectura usan AsNoTracking().

## Antes de dar por terminada una tarea

Ejecutar `dotnet build` y `dotnet test`. Si algo falla, corregirlo.

Parte 01 · segunda mitad

Metodología

Cinco formas de trabajar que separan a quien produce código mantenible de quien produce un desastre rápido

Metodología 1 · la más importante

Modo plan: plan antes que código

Casi todas las herramientas tienen un modo donde primero proponen un plan y no tocan nada hasta que lo aprobás. Cursor lo llama Plan, Claude Code plan mode, Copilot lo hace desde Workspace.

Por qué funciona

* Un plan malo se corrige en una línea; 400 líneas de código malo se corrigen tirándolas
* Te obliga a leer el enfoque antes de ver la solución, que es cuando todavía podés pensar
* Expone los supuestos que la herramienta hizo sola

Cuándo usarlo

* Siempre que la tarea toque más de un archivo
* Siempre que no tengas claro el diseño
* Siempre en un proyecto que no conocés

Para agregar un campo a un DTO, es exagerado

Modo plan · ejemplo real

El plan que devuelve, y qué mirar

// PEDIDO ─────────────────────────────────────────────────────────
Hay que aplicar descuentos al total del pedido según el tipo de cliente
(mayorista 15%, empleado 30%). No escribas código todavía: proponé un plan.

// PLAN QUE DEVUELVE ──────────────────────────────────────────────

1. Domain/Descuentos/IReglaDescuento.cs
   Nueva interfaz: AplicaA(Cliente) y Calcular(decimal).
2. Domain/Descuentos/DescuentoMayorista.cs, DescuentoEmpleado.cs
   Una clase por regla.
3. Domain/Pedido.cs
   Nuevo método AplicarDescuentos(CalculadorDescuentos).
   Supuesto: los descuentos NO se acumulan; se aplica solo el mayor.
4. Application/CrearPedidoCommandHandler.cs
   Llamar a AplicarDescuentos antes de persistir.
5. Program.cs
   Registrar las reglas como IEnumerable<IReglaDescuento> (Singleton, sin estado).
6. Tests/DescuentosTests.cs
   Casos: sin descuento, mayorista, empleado, cliente que es ambos.

Acá está el valor

El punto 3 tiene un supuesto de negocio que nadie pidió. Si aceptás el plan sin leerlo, ese supuesto entra al sistema y 
aparece en producción tres meses después. Detectarlo ahora cuesta una línea.

Metodología 2

Loop cerrado: que se corrija sola

Un agente con acceso a la terminal puede correr los tests, leer el error y volver a intentar. Eso convierte una conversación 
en un ciclo de verificación automática.

// 1 · Escribís el test PRIMERO — es la definición del trabajo terminado
[Fact]
public void Pedido_con_stock_insuficiente_no_se_confirma()
{
var producto = new Producto("Teclado", precio: 25000, stock: 1);
var pedido   = Pedido.Crear(clienteId: 1);

```
Assert.Throws<StockInsuficienteException>(
    () => pedido.AgregarItem(producto, cantidad: 5));

Assert.Equal(1, producto.Stock);   // el stock no se tocó
```

}

// 2 · Y le pedís:
"Este test falla. Implementá lo necesario para que pase.
No modifiques el test. Corré `dotnet test` hasta que esté en verde."

El test es grounding y es criterio de aceptación al mismo tiempo: define qué tiene que pasar sin decir cómo, que es exactamente lo que querés delegar.

Metodología 3

Tareas chicas y verificables

Lo que no funciona

«Hacé el sistema de pedidos completo con capas, repositorios, CQRS y tests.»

Devuelve treinta archivos. No sabés cuál mirar primero, todo compila, nada está probado, y si algo está mal no sabés dónde.

Lo que sí funciona

Una unidad por vez, cada una con su verificación:

* Las entidades del dominio → compila
* IPedidoRepository y su implementación → compila
* El handler de crear pedido → pasa su test
* El controller → responde 201

Regla práctica

Si el cambio no entra en un commit que puedas describir en una línea, la tarea era demasiado grande. 
Commiteá entre paso y paso: así podés volver atrás sin perder todo.

Metodología 4

Revisar el diff, no el archivo

Cuando un agente modifica seis archivos, leerlos enteros es imposible y leer nada es peor. Lo que se revisa es el cambio.

git diff --stat
Domain/Pedido.cs                    | 18 ++++++++++---
Domain/Descuentos/IReglaDescuento.cs | 12 +++++++++
Application/CrearPedidoCommandHandler.cs    |  9 ++++---
Program.cs                           |  4 +++

git diff Domain/Pedido.cs

Qué mirar en cada cambio

* ¿Tocó archivos que no correspondían?
* ¿Agregó una dependencia nueva sin avisar?
* ¿Borró validaciones o manejo de errores «de paso»?
* ¿El cambio hace lo que pediste, o algo parecido?

Señales de alarma

* Un catch vacío que apareció solo
* Un TODO donde había lógica
* Un test modificado para que pase
* Una cadena de conexión hardcodeada

Metodología 5

Pedile que explique antes de que cambie

Especialmente en código que no escribiste vos. Es la técnica con mejor relación esfuerzo/beneficio de todas, 
y la que más les va a servir cuando entren a trabajar.

// En vez de: "arreglá este método"

"Explicame qué hace CrearPedido paso a paso, qué responsabilidades
mezcla y qué pasaría si dos usuarios lo llaman al mismo tiempo.
No cambies nada todavía."

// Después de leer la respuesta, ya sabés qué pedir — y podés
// evaluar si la respuesta tiene sentido, que es la mitad del trabajo.

Por qué funciona

Te da vos el modelo mental del código antes de delegar el cambio. Sin ese paso, estás 
aprobando modificaciones sobre algo que no entendés — que es exactamente cómo se rompen los sistemas.

Prompting

Un pedido de trabajo tiene cuatro partes

// 1 · CONTEXTO — grounding: sobre qué trabajás
Proyecto .NET 8 con EF Core y SQL Server, arquitectura en capas.
Te paso Pedido.cs, IPedidoRepository.cs y AppDbContext.cs.

// 2 · OBJETIVO — qué querés lograr, no cómo
Registrar un pedido nuevo descontando stock de cada producto.

// 3 · RESTRICCIONES — las reglas que no se negocian

* La lógica va en un handler en Application, no en el controller.
* El acceso a datos, vía IPedidoRepository. Nada de DbContext directo.
* Si un producto no tiene stock suficiente, no se persiste nada.
* Sin librerías nuevas.

// 4 · FORMATO — qué querés recibir
Primero el plan. Cuando lo apruebe, solo el Command y su Handler.

La diferencia entre una respuesta inútil y una aprovechable casi nunca está en el modelo: está en el punto 1 y en el punto 3.

Anti-patrones

Cinco formas de arruinarlo

* Aceptar sin leer. El copiloto sugiere, vos apretás Tab, y a la semana hay tres formas distintas de hacer lo mismo en el proyecto.

* Conversación infinita. Cuarenta mensajes en el mismo hilo: el contexto se llena de intentos 
fallidos y la calidad cae. Empezá de nuevo con lo que aprendiste.

* Pedir sin verificar. Delegar una tarea sin tener cómo saber si salió bien — sin test, sin poder correrlo, sin criterio propio.

* Pelear con el modelo. Si tres intentos no lo resuelven, el problema es el pedido o la falta de contexto, no la insistencia.

* Delegar lo que no entendés. Está bien para explorar. Está mal para entregar.

Límites

Riesgos que hay que tener presentes

Técnicos

* Código plausible pero incorrecto: compila, corre y hace algo apenas distinto
* Dependencias inventadas o de una versión que no usás
* Sugerencias inseguras: SQL concatenado, secretos hardcodeados, validación ausente
* Soluciones genéricas que ignoran el contexto del proyecto

Profesionales

* Datos sensibles — nunca pegar credenciales, cadenas de conexión reales ni datos de clientes
* Licencias — el código generado puede parecerse a código con licencia restrictiva
* Responsabilidad — si falla en producción, el responsable sos vos
* Dependencia cognitiva — delegar tan temprano que nunca desarrollás criterio propio

Muchas empresas tienen políticas explícitas sobre qué se puede pegar en una herramienta de IA. 
Antes de empezar a trabajar, léanla — no es un trámite.

Regla de la materia

No entregás código que no puedas explicar línea por línea.

Usar IA está permitido y recomendado. Si no lo entendés, no es tuyo — y en la defensa se nota en treinta segundos.

Demo en vivo

Le pedimos el sistema, sin darle contexto

1. «Diseñá el modelo relacional para un sistema de pedidos con clientes, productos e ítems. 
Dame el DER y el DDL de SQL Server.» → sale bien

2. «Ahora las entidades POCO y el DbContext de EF Core.» → sale bien, 
es un patrón visto un millón de veces

3. «¿El precio del ítem lo guardo o lo leo del producto?» → respuesta razonable
y genérica. La decisión correcta depende del negocio, y eso no lo puede saber.

4. «Hacé un endpoint que cree un pedido y descuente stock.» → miremos qué devuelve sin 
grounding y sin restricciones

Demo · resultado

Esto es lo que devuelve

[HttpPost]
public IActionResult CrearPedido(PedidoDto dto)
{
var ctx = new AppDbContext();                     // ① instancia su dependencia

```
var total = 0m;
foreach (var item in dto.Items)                     // ② regla de negocio
{
    var prod = ctx.Productos.Find(item.ProductoId);
    if (prod.Stock < item.Cantidad) return BadRequest("Sin stock");
    prod.Stock -= item.Cantidad;
    total += prod.Precio * item.Cantidad;
}

if (dto.TipoCliente == "Mayorista") total *= 0.85m;  // ③ más negocio

ctx.Pedidos.Add(new Pedido { ... });                 // ④ acceso a datos
ctx.SaveChanges();

new SmtpClient("smtp.tienda.com").Send("confirmado");  // ⑤ infraestructura
return Ok();
```

}

### Domain y Application

├── 📁 2 · Application                 — casos de uso, depende solo de Domain
│   ├── 📁 Interfaces
│   │   ├── 📄 IPedidoRepository.cs    contrato de acceso a datos
│   │   └── 📄 IUnitOfWork.cs          quien confirma la transacción
│   ├── 📁 DTOs
│   │   └── 📄 PedidoResponseDto.cs    dedicado exclusivamente a la salida
│   ├── 📁 Mappings
│   │   └── 📄 PedidoMappings.cs       extensiones: Pedido → PedidoResponseDto
│   └── 📁 UseCases
│       └── 📁 Pedidos
│           ├── 📁 Commands            intenciones de escritura (solo datos)
│           │   ├── 📄 CrearPedidoCommand.cs
│           │   └── 📄 CancelarPedidoCommand.cs
│           ├── 📁 Queries             intenciones de lectura (solo datos)
│           │   ├── 📄 GetPedidoByIdQuery.cs
│           │   └── 📄 GetPedidosDelClienteQuery.cs
│           └── 📁 Handlers            un handler por caso de uso
│               ├── 📄 CrearPedidoCommandHandler.cs
│               ├── 📄 CancelarPedidoCommandHandler.cs
│               ├── 📄 GetPedidoByIdQueryHandler.cs
│               └── 📄 GetPedidosDelClienteQueryHandler.cs

### Infrastructure y Presentation

📁 3 · Infrastructure
│   ├── 📁 Persistence
│   │   ├── 📄 AppDbContext.cs
│   │   ├── 📁 Configurations
│   │   │   ├── 📄 PedidoConfiguration.cs
│   │   │   └── 📄 ProductoConfiguration.cs
│   │   └── 📁 Repositories
│   │       └── 📄 PedidoRepository.cs    implementa IPedidoRepository
│   └── 📁 Notifications
│       └── 📄 EmailNotificador.cs        implementa INotificador

└── 📁 4 · Presentation
├── 📁 Controllers
│   └── 📄 PedidosController.cs
├── 📁 Middlewares
│   └── 📄 DomainExceptionMiddleware.cs
└── 📄 Program.cs

### La entidad rica

No es un DTO con métodos

Una entidad de dominio no debería ser una bolsa de propiedades públicas donde cualquiera 
puede poner cualquier cosa. Tiene estado + comportamiento + invariantes.

```csharp
public class Producto
{
    public int Id { get; private set; }
    public string Nombre { get; private set; }
    public decimal Precio { get; private set; }
    public int Stock { get; private set; }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor a cero.");

        if (Stock < cantidad)
            throw new DomainException(
                $"Stock insuficiente. Disponible: {Stock}.");

        Stock -= cantidad;
    }
}
```

La regla vive donde está el dato que necesita para cumplirse.

El handler no pregunta cuánto stock hay para decidir qué hacer: le pide al Producto 
que descuente stock. Si la regla cambia, cambia Producto — no todos los lugares que lo usan.

### Excepciones de dominio

Errores que son reglas del negocio

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

```csharp
public class Pedido
{
    public decimal Total { get; private set; }

    public void AplicarDescuento(decimal porcentaje)
    {
        if (porcentaje < 0 || porcentaje > 100)
            throw new DomainException(
                "El porcentaje debe estar entre 0 y 100.");

        Total -= Total * porcentaje / 100;
    }
}
```

No son excepciones «técnicas». Son situaciones que el negocio considera inválidas.

Un middleware puede capturarlas y convertirlas en una respuesta HTTP 400 sin llenar cada controller de try/catch.

### Middleware de excepciones

Un solo lugar para traducir errores de dominio a HTTP

```csharp
public class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public DomainExceptionMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ex.Message
            });
        }
    }
}
```

```csharp
// Program.cs
app.UseMiddleware<DomainExceptionMiddleware>();
```

El dominio no sabe que existe HTTP.

Ese detalle importa: si mañana este mismo caso de uso se ejecuta desde un worker o una consola, la regla sigue funcionando exactamente igual.

### Validación

Dos problemas distintos que no hay que mezclar

**Input validation**

«¿Los datos que llegaron tienen la forma correcta?»

* Email con formato válido
* Cantidad > 0
* Campos obligatorios
* String no demasiado largo
* JSON bien formado

Puede vivir en Presentation o con FluentValidation en Application.

**Business validation**

«¿La operación tiene sentido según las reglas del negocio?»

* ¿Hay stock suficiente?
* ¿El cliente puede cancelar este pedido?
* ¿Este descuento se puede aplicar?
* ¿El pedido está en un estado que permite agregar ítems?

Vive en Domain / Application según quién tenga la información necesaria.

### Arquitectura en capas

Características principales

* Separación de responsabilidades
* Cada capa tiene un propósito bien definido
* Bajo acoplamiento entre componentes
* Alta cohesión dentro de cada capa
* Facilita mantenimiento y evolución
* Permite reemplazar implementaciones

### Ventajas

¿Por qué usar capas?

**Mantenibilidad**

Los cambios quedan acotados a la capa correspondiente.

**Testabilidad**

Las dependencias pueden reemplazarse por mocks/fakes.

**Reutilización**

La lógica de negocio no depende de una interfaz particular.

**Evolución**

Podés cambiar SQL Server por PostgreSQL, o SMTP por otro proveedor, sin tocar el dominio.

### Dependency Injection

¿Quién construye los objetos?

Volvamos al problema que vimos antes:

```csharp
public class PedidoService
{
    private readonly SmtpClient _mail =
        new SmtpClient("smtp...");

    public void Confirmar(Pedido p)
    {
        // ...
        _mail.Send("confirmado");
    }
}
```

`PedidoService` decide qué implementación concreta usar.

Eso genera acoplamiento.

Con Dependency Injection:

```csharp
public interface INotificador
{
    void Notificar(Pedido p);
}

public class PedidoService
{
    private readonly INotificador _notificador;

    public PedidoService(INotificador notificador)
    {
        _notificador = notificador;
    }

    public void Confirmar(Pedido p)
    {
        // ...
        _notificador.Notificar(p);
    }
}
```

Ahora `PedidoService` solo conoce la abstracción.

Quién construye `EmailNotificador` y quién lo conecta con `PedidoService` es responsabilidad de otro lugar.

### DI · el contenedor

.NET trae un contenedor de dependencias incorporado

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INotificador, EmailNotificador>();

builder.Services.AddControllers();

var app = builder.Build();
```

Cuando alguien pide:

```csharp
public CrearPedidoCommandHandler(
    IPedidoRepository repository,
    IUnitOfWork unitOfWork)
```

el contenedor sabe qué implementación entregar.

No hay `new PedidoRepository()` dentro del handler.

No hay `new UnitOfWork()` dentro del handler.

El objeto recibe lo que necesita.

### Lifetimes

¿Cuánto tiempo vive cada dependencia?

**Transient**

Una instancia nueva cada vez que se solicita.

```csharp
services.AddTransient<IValidador, Validador>();
```

Útil para objetos pequeños y sin estado.

**Scoped**

Una instancia por request HTTP.

```csharp
services.AddScoped<AppDbContext>();
```

Es el lifetime habitual para `DbContext` y repositorios.

**Singleton**

Una sola instancia durante toda la vida de la aplicación.

```csharp
services.AddSingleton<IConfiguracion, Configuracion>();
```

Debe ser thread-safe y no depender de servicios Scoped.

### Una regla que evita muchos problemas

No hagas que una dependencia de vida larga capture una de vida corta.

```text
Singleton
    ↓
Scoped       ← PROBLEMA
    ↓
Transient
```

Un Singleton que depende de un Scoped puede terminar reteniendo una instancia más tiempo del debido y generar comportamientos inesperados.

El `DbContext` normalmente es Scoped justamente porque representa una unidad de trabajo asociada al request.

### Composition Root

El lugar donde se conectan las piezas

`Program.cs` es el Composition Root de la aplicación.

Es el único lugar donde debería aparecer la configuración concreta:

```csharp
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INotificador, EmailNotificador>();
```

El resto del sistema trabaja con abstracciones.

```text
Program.cs
    │
    ├── IPedidoRepository → PedidoRepository
    ├── IUnitOfWork        → UnitOfWork
    └── INotificador       → EmailNotificador
```

Si mañana cambiás Email por WhatsApp:

```csharp
builder.Services.AddScoped<INotificador, WhatsAppNotificador>();
```

No tocás `PedidoService`.

### Qué problema resuelve realmente DI

No es «para no escribir new».

Es para que una clase no sea responsable de decidir qué implementación concreta necesita.

**Sin DI**

```text
PedidoService
    │
    └──────→ SmtpClient
```

**Con DI**

```text
PedidoService
       │
       ▼
 INotificador
       ▲
       │
EmailNotificador
```

El código de alto nivel depende de una abstracción.

Eso es Dependency Inversion Principle + Dependency Injection.

### Acoplamiento

Qué tan atadas están dos piezas de código

**Alto acoplamiento**

```csharp
public class PedidoService
{
    private readonly SqlServerPedidoRepository _repo;
}
```

Si cambiás SQL Server, esta clase cambia.

**Bajo acoplamiento**

```csharp
public class PedidoService
{
    private readonly IPedidoRepository _repo;
}
```

Podés tener:

```text
IPedidoRepository
   ├── SqlServerPedidoRepository
   ├── InMemoryPedidoRepository
   └── MockPedidoRepository
```

La clase de negocio no necesita saber cuál está usando.

### Composition Root · ejemplo completo

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INotificador, EmailNotificador>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<CrearPedidoCommand>());

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();

app.MapControllers();

app.Run();
```

### Repository Pattern

El contrato entre Application y Infrastructure

Application necesita guardar y consultar pedidos.

Pero no debería saber si los datos vienen de SQL Server, PostgreSQL, memoria o una API.

Define un contrato:

```csharp
public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Pedido>> ObtenerDelClienteAsync(int clienteId);
    Task AgregarAsync(Pedido pedido);
}
```

Infrastructure lo implementa:

```csharp
public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _ctx;

    public PedidoRepository(AppDbContext ctx)
        => _ctx = ctx;

    public async Task<Pedido?> ObtenerPorIdAsync(int id)
        => await _ctx.Pedidos
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Pedido>>
        ObtenerDelClienteAsync(int clienteId)
        => await _ctx.Pedidos
            .Where(p => p.ClienteId == clienteId)
            .AsNoTracking()
            .ToListAsync();

    public async Task AgregarAsync(Pedido pedido)
        => await _ctx.Pedidos.AddAsync(pedido);
}
```

### ¿Por qué no usar DbContext directamente?

Podríamos hacer esto:

```csharp
public class CrearPedidoCommandHandler
{
    private readonly AppDbContext _ctx;

    // ...
}
```

Pero entonces Application depende de Infrastructure.

Y perdemos la dirección de dependencias:

```text
❌ Application → Infrastructure
```

Con Repository:

```text
✅ Application → abstracción
                 ↑
          Infrastructure
```

Infrastructure implementa el contrato que Application necesita.

### Repository · cuidado

No todo acceso a datos necesita un Repository genérico.

Esto:

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
```

puede terminar ocultando demasiado lo que realmente hace la base.

Un repositorio debería representar una necesidad del dominio o del caso de uso, no simplemente envolver cada método de `DbSet`.

### Unit of Work

Una sola confirmación para varios cambios

Al confirmar un pedido podemos modificar:

* Pedido
* ItemsPedido
* Stock de varios Productos

Queremos que todo se confirme junto.

```text
┌──────────────────────────────┐
│          UnitOfWork          │
│                              │
│ Pedido      → INSERT         │
│ ItemPedido  → INSERT         │
│ Producto    → UPDATE         │
│ Producto    → UPDATE         │
│                              │
│        SaveChanges()         │
└──────────────────────────────┘
```

Si algo falla:

```text
ROLLBACK
```

Si todo funciona:

```text
COMMIT
```

### IUnitOfWork

El contrato vive en Application

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

Infrastructure lo implementa:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public UnitOfWork(AppDbContext ctx)
        => _ctx = ctx;

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => _ctx.SaveChangesAsync(cancellationToken);
}
```

El handler no conoce `DbContext`.

Solo sabe que existe una operación que confirma los cambios.

### Repository + Unit of Work

Responsabilidades diferentes

**Repository**

Se ocupa de consultar y agregar entidades.

```text
IPedidoRepository
    ↓
«¿Cómo obtengo o agrego pedidos?»
```

**Unit of Work**

Se ocupa de confirmar los cambios.

```text
IUnitOfWork
    ↓
«¿Cuándo se persisten todos los cambios?»
```

No son lo mismo.

### CQRS

Command Query Responsibility Segregation

Separar las operaciones que:

* **modifican** el sistema
* **leen** información del sistema

No significa necesariamente tener dos bases de datos.

Es una separación conceptual de responsabilidades.

### Command

Una intención de modificar el sistema

```csharp
public record CrearPedidoCommand(
    int ClienteId,
    List<ItemPedidoDto> Items);
```

El Command solo transporta datos.

No hace nada.

No contiene lógica de negocio.

Es una representación de:

> «Quiero crear este pedido».

### Command Handler

El que ejecuta la intención

```csharp
public class CrearPedidoCommandHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork _unitOfWork;

    public CrearPedidoCommandHandler(
        IPedidoRepository pedidos,
        IUnitOfWork unitOfWork)
    {
        _pedidos = pedidos;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        CrearPedidoCommand command)
    {
        var pedido = Pedido.Crear(command.ClienteId);

        foreach (var item in command.Items)
        {
            pedido.AgregarItem(
                item.ProductoId,
                item.Cantidad);
        }

        await _pedidos.AgregarAsync(pedido);
        await _unitOfWork.SaveChangesAsync();

        return pedido.Id;
    }
}
```

El Handler coordina.

El dominio decide las reglas.

Infrastructure persiste.

### Query

Una intención de lectura

```csharp
public record GetPedidoByIdQuery(int Id);
```

Y su Handler:

```csharp
public class GetPedidoByIdQueryHandler
{
    private readonly IPedidoRepository _pedidos;

    public GetPedidoByIdQueryHandler(
        IPedidoRepository pedidos)
    {
        _pedidos = pedidos;
    }

    public async Task<PedidoResponseDto?> Handle(
        GetPedidoByIdQuery query)
    {
        var pedido =
            await _pedidos.ObtenerPorIdAsync(query.Id);

        return pedido?.ToResponseDto();
    }
}
```

Una Query no debería modificar el estado.

### Commands vs Queries

| Command                                           | Query               |
| ------------------------------------------------- | ------------------- |
| Cambia el estado                                  | Solo lee            |
| Puede generar efectos secundarios                 | No debería tenerlos |
| Devuelve normalmente un resultado de la operación | Devuelve datos      |
| Crear pedido                                      | Obtener pedido      |
| Cancelar pedido                                   | Listar pedidos      |
| Actualizar stock                                  | Buscar productos    |

### ¿Por qué CQRS?

Porque las necesidades de lectura y escritura suelen ser diferentes.

Una escritura necesita:

* Validar reglas
* Cambiar estado
* Mantener invariantes
* Persistir

Una lectura necesita:

* Consultar
* Proyectar
* Optimizar
* No modificar nada

Separarlas permite que cada lado sea más simple.

### Controller

El controller debería ser aburrido

```csharp
[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PedidosController(IMediator mediator)
        => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Crear(
        CrearPedidoCommand command)
    {
        var id = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(Obtener),
            new { id },
            new { id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var pedido =
            await _mediator.Send(
                new GetPedidoByIdQuery(id));

        return pedido is null
            ? NotFound()
            : Ok(pedido);
    }
}
```

El controller:

* recibe HTTP
* transforma/recibe datos
* invoca el caso de uso
* devuelve HTTP

Nada más.

### La película completa

¿Qué pasa cuando llega POST /api/pedidos?

```text
HTTP POST
   │
   ▼
┌───────────────────┐
│    Controller     │
└─────────┬─────────┘
          │ Command
          ▼
┌───────────────────┐
│      Handler      │
└─────────┬─────────┘
          │
          ├──────────────→ Repository
          │                    │
          │                    ▼
          │               DbContext
          │                    │
          │                    ▼
          │                SQL Server
          │
          └──────────────→ Domain
                              │
                              ▼
                           Reglas
```

Y al final:

```text
UnitOfWork.SaveChangesAsync()
           │
           ▼
        COMMIT
```

### MediatR

Un mediador entre Controller y Handler

En lugar de que el controller conozca directamente cada handler:

```text
Controller
    │
    ▼
 IMediator
    │
    ├── CrearPedidoCommandHandler
    ├── CancelarPedidoCommandHandler
    ├── GetPedidoByIdQueryHandler
    └── GetPedidosDelClienteQueryHandler
```

El controller solo necesita conocer `IMediator`.

El mediador encuentra el handler correspondiente.

### ¿Es obligatorio usar MediatR?

No.

CQRS es el concepto.

MediatR es una herramienta que facilita implementarlo.

Podrías perfectamente hacer:

```csharp
var resultado =
    await _crearPedidoHandler.Handle(command);
```

sin ninguna librería.

La herramienta no es la arquitectura.

### La arquitectura completa

```text
                    ┌─────────────────────┐
                    │    PRESENTATION     │
                    │                     │
                    │ Controllers         │
                    │ Middlewares         │
                    │ Program.cs          │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │    APPLICATION      │
                    │                     │
                    │ Commands / Queries  │
                    │ Handlers             │
                    │ Interfaces           │
                    │ DTOs                 │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │       DOMAIN        │
                    │                     │
                    │ Entities             │
                    │ Rules                │
                    │ Exceptions           │
                    └─────────────────────┘
                               ▲
                               │
                    ┌──────────┴──────────┐
                    │   INFRASTRUCTURE    │
                    │                     │
                    │ EF Core              │
                    │ Repositories         │
                    │ UnitOfWork            │
                    │ Email                │
                    └─────────────────────┘
```

### La regla de oro

El centro no sabe nada de lo que está afuera.

Domain no sabe:

* qué base usamos
* qué ORM usamos
* si existe HTTP
* si mandamos mails
* si usamos MediatR
* si la aplicación corre en Azure

El centro conoce solamente el negocio.

### ¿Qué ganamos con todo esto?

Cuando el sistema crece:

```text
Sin arquitectura

Controller
    ├── SQL
    ├── reglas
    ├── mails
    ├── validaciones
    ├── descuentos
    └── todo lo demás
```

termina siendo difícil de probar y modificar.

Con la separación:

```text
Presentation
      ↓
Application
      ↓
Domain
      ↑
Infrastructure
```

cada parte tiene un motivo claro para existir.

### El precio

Nada es gratis.

Esta arquitectura agrega:

* más proyectos
* más archivos
* más interfaces
* más clases
* más configuración
* más conceptos

Para una aplicación de veinte líneas puede ser absurdo.

Para un sistema que va a vivir cinco años y ser mantenido por diez personas, puede ahorrar muchísimo trabajo.

### La pregunta que hay que hacerse

No:

> «¿Estoy usando Clean Architecture correctamente?»

Sino:

> «¿Este diseño hace que el próximo cambio sea más fácil o más difícil?»

### Cierre

Hoy vimos seis piezas que parecen separadas, pero en realidad forman una cadena:

**IA**

Grounding, plan, tareas chicas, diff y tests.

↓

**Datos**

Modelo relacional, ACID, SQL.

↓

**ORM**

EF Core, LINQ, tracking, migrations.

↓

**SOLID**

Responsabilidades, extensibilidad y dependencias.

↓

**Capas + DI**

Separar responsabilidades y depender de abstracciones.

↓

**Repository + Unit of Work + CQRS**

Separar acceso a datos, persistencia y casos de uso.

### La idea que deberían llevarse

La herramienta puede escribir el código.

Pero ustedes tienen que decidir:

* qué código escribir
* dónde ponerlo
* de qué depende
* qué reglas debe cumplir
* cómo comprobar que funciona

### Y una última vez, el ejemplo

Un pedido llega:

```text
POST /api/pedidos
        │
        ▼
   Controller
        │
        ▼
 CreatePedidoCommand
        │
        ▼
      Handler
        │
        ├──────→ Cliente
        │
        ├──────→ Producto.DescontarStock()
        │
        ├──────→ Pedido.AgregarItem()
        │
        ├──────→ Repository
        │
        └──────→ UnitOfWork
                       │
                       ▼
                  SaveChanges()
                       │
                       ▼
                   SQL Server
```

Y si algo sale mal:

```text
DomainException
       │
       ▼
   Middleware
       │
       ▼
HTTP 400
```

### Fin

No se trata de memorizar patrones.

Se trata de poder mirar un problema y reconocer:

> «Esta responsabilidad no pertenece acá.»

> «Esta clase está demasiado acoplada.»

> «Esta regla debería vivir en el dominio.»

> «Esta consulta no debería trackear entidades.»

> «Este cambio debería poder agregarse sin tocar lo que ya funciona.»

> «Necesito darle más contexto a la IA antes de pedirle código.»

## La solución · 2 de 2

# Infrastructure y Presentation

├── 📁 3 · Infrastructure — el detalle técnico
│   └── 📁 Persistence
│       ├── 📄 AppDbContext.cs
│       ├── 📄 UnitOfWork.cs — implementa IUnitOfWork
│       ├── 📁 Configurations
│       │   └── 📄 PedidoConfiguration.cs — IEntityTypeConfiguration<Pedido>
│       ├── 📁 Migrations — el esquema versionado en git
│       └── 📁 Repositories
│           └── 📄 PedidoRepository.cs — único punto de contacto con EF Core
│                                      NUNCA llama SaveChanges

└── 📁 4 · Presentation (Api) — punto de entrada HTTP
├── 📁 Middlewares
│   └── 📄 ExceptionMiddleware.cs — DomainException → 400
│                                  Exception       → 500
├── 📁 Controllers
│   └── 📄 PedidosController.cs — inyecta los Handlers que necesita
│                                  NO hay Service intermedio
├── 📄 appsettings.Development.json
└── 📄 Program.cs — COMPOSITION ROOT
único lugar que conoce Infrastructure

Esta estructura es la que hay que entregar en el trabajo práctico. 
Sirve igual para cualquier dominio: cambian los nombres de las entidades, no la organización.

---

## Domain · el centro

# Entidad rica, no bolsa de propiedades

```csharp
// ANÉMICA: cualquiera la modifica desde afuera y nadie valida nada
public class Producto
{
    public int Stock { get; set; }     // producto.Stock = -50;  ✓ compila
}

// RICA: la entidad protege sus propias reglas
public class Producto
{
    public int Stock { get; private set; }   // solo la clase se modifica

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser positiva");
        if (cantidad > Stock)
            throw new DomainException($"Stock insuficiente para {Nombre}");

        Stock -= cantidad;
    }
}
```

### Por qué es la decisión más importante

Si las entidades son solo `get; set;` públicos, la lógica se escapa a los handlers y 
la arquitectura en capas queda decorativa: podés tener cuatro proyectos y toda la lógica igual de apilada. 
**La regla vive donde vive el dato.**

---

## Domain → Presentation

# Cómo devuelve un 400 el dominio sin conocer HTTP

El dominio no puede devolver `BadRequest()`: no sabe que existe HTTP. Lo que hace es 
**lanzar una excepción de negocio**, y alguien afuera la traduce.

```csharp
// Domain/Exceptions/DomainException.cs
public class DomainException : Exception
{
    public DomainException(string mensaje) : base(mensaje) { }
}

// Presentation/Middlewares/ExceptionMiddleware.cs
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }

        catch (DomainException ex)              // regla de negocio violada
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)                    // algo que no previmos
        {
            _logger.LogError(ex, "Error no controlado");
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { error = "Error interno" });
        }
    }
}
```

Sin esto, cada controller repite el mismo `try/catch`. Con esto, se escribe una vez y aplica a toda la API.

---

## Un tema que se suele mezclar

# Hay dos validaciones distintas

|                         | De entrada                                       | De negocio                             |
| ----------------------- | ------------------------------------------------ | -------------------------------------- |
| Qué pregunta            | ¿El request está bien formado?                   | ¿La operación es válida?               |
| Ejemplo                 | `Cantidad` mayor a cero, lista de ítems no vacía | «No hay stock suficiente para Teclado» |
| ¿Necesita ir a la base? | No: se resuelve mirando el request               | Sí: hay que consultar el estado actual |
| Dónde vive              | En el **Command**, con DataAnnotations           | Dentro de la **entidad** del dominio   |
| Cómo falla              | `ValidationException` → 400                      | `DomainException` → 400                |
| Cuándo corre            | Antes de entrar al handler                       | Adentro del handler                    |

```csharp
// ▸ Application/UseCases/Pedidos/Commands — validación de ENTRADA
public class CrearPedidoCommand
{
    [Required]
    public int ClienteId { get; init; }

    [MinLength(1, ErrorMessage = "El pedido debe tener al menos un ítem")]
    public List<ItemDto> Items { get; init; } = [];
}

// Con [ApiController] en el controller, ASP.NET valida esto solo
// y devuelve 400 con el detalle. No hay que escribir una línea.
```

Para proyectos grandes existe **FluentValidation**, que saca las reglas del Command 
a una clase aparte. Para el trabajo práctico, DataAnnotations alcanza y no agrega dependencias.

---

## Características

# Qué define a una arquitectura en capas

* **Abstracción** — abstrae la vista del modelo como un todo, con suficiente detalle para entender las relaciones entre capas.
* **Encapsulamiento** — el diseño no hace asunciones sobre tipos de datos, métodos o implementación.
* **Funcionalidad definida** — separa claramente la funcionalidad de cada capa.
* **Alta cohesión** — cada capa contiene funcionalidad directamente relacionada con su tarea.
* **Reutilizable** — las capas inferiores no dependen de las superiores, así que pueden reutilizarse en otros escenarios.
* **Desacople** — la comunicación entre capas se basa en abstracciones.

---

## Ventajas

# Qué se gana

* **Abstracción** — permite hacer cambios a nivel abstracto sin propagarlos a todo el sistema.
* **Aislamiento** — aísla los cambios de tecnología en ciertas capas, reduciendo el impacto en el total.
* **Rendimiento** — distribuir las capas entre múltiples sistemas físicos puede aumentar escalabilidad, tolerancia a fallos y rendimiento.
* **Mejoras en pruebas** — con interfaces bien definidas por capa se puede cambiar a implementaciones alternativas para testear.
* **Independencia** — reduce la necesidad de considerar hardware, instalación y dependencias externas en todas las capas.

---

## Dos reglas que hacen que funcione

# Lo que más se equivoca en la práctica

### La dependencia va en una sola dirección

Domain no referencia a nadie. Si el caso de uso necesita guardar, **Application declara 
la interfaz** `IPedidoRepository` y es Infrastructure quien la implementa.

Por eso el principio se llama «inversión».

### Las entidades no cruzan hacia afuera

Hacia Presentation viajan **DTOs**, no entidades del dominio. Si exponés la entidad, 
cualquier cambio interno rompe tu API pública — y encima filtrás campos que no querías exponer.

---

## Dependency Injection

# Inyección de dependencias

Un patrón de diseño que consiste en **delegar la creación de objetos en tiempo de ejecución** a un componente específico.

Un objeto se encarga de construir las dependencias que una clase necesita y se las suministra — 
de ahí el término «inyección». La clase ya no crea los objetos que necesita: los **recibe**.

### Ventajas

* Bajo acoplamiento de los componentes
* Al ser un patrón, se aplica en diversos lenguajes
* Facilita los tests, permitiendo TDD
* Software mantenible

### Formas de inyectar

* Por **constructor** — la más usada y la recomendada
* Por **propiedad**
* Por **método**

---

## Ejemplo

# Fuerte acoplamiento y bajo acoplamiento

### Fuerte acoplamiento

```csharp
public class PedidoService
{
    private AppDbContext _ctx
        = new AppDbContext();

    public void Crear(Pedido p)
    {
        _ctx.Pedidos.Add(p);
        _ctx.SaveChanges();
    }
}

// Para testear esto necesitás
// una base de datos real.
```

### Bajo acoplamiento

```csharp
public class PedidoService
{
    private readonly IPedidoRepository _repo;

    public PedidoService(IPedidoRepository repo)
        => _repo = repo;

    public void Crear(Pedido p)
        => _repo.Agregar(p);
}

// En el test le pasás un mock
// y no hace falta base de datos.
```

---

## DI en .NET Core

# IServiceCollection y los tres lifetimes

```csharp
// Program.cs — el contenedor que instancia los objetos

builder.Services.AddSingleton<IConfiguracion, Configuracion>();
// Se crea la primera vez que se solicita. Cada solicitud posterior
// usa la MISMA instancia, durante toda la vida de la aplicación.

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
// Se crea UNA VEZ POR SOLICITUD del cliente (por request HTTP).

builder.Services.AddTransient<INotificador, EmailNotificador>();
// Se crea CADA VEZ que se solicita desde el contenedor.
// Funciona mejor para servicios livianos y sin estado.

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
// AddDbContext registra como Scoped por defecto. Con razón:
// un DbContext compartido entre requests corrompe datos.
```

---

## Composition root

# El Program.cs completo del proyecto

```csharp
var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ───────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();      // ← Scoped, sí o sí
builder.Services.AddTransient<INotificador, EmailNotificador>();

// ── Application: un registro por handler ─────────────────────────
builder.Services.AddScoped<CrearPedidoCommandHandler>();
builder.Services.AddScoped<CancelarPedidoCommandHandler>();
builder.Services.AddScoped<GetPedidoByIdQueryHandler>();
builder.Services.AddScoped<GetPedidosDelClienteQueryHandler>();

// ── Domain: las reglas de descuento ──────────────────────────────
builder.Services.AddSingleton<IReglaDescuento, DescuentoMayorista>();
builder.Services.AddSingleton<IReglaDescuento, DescuentoEmpleado>();
builder.Services.AddSingleton<CalculadorDescuentos>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();   // ← primero, para capturar todo
app.MapControllers();
app.Run();
```

### Este archivo es el único que conoce Infrastructure

Es el **composition root**: el punto donde se arma el grafo de objetos. 
Ningún controller ni handler debería referenciar `PedidoRepository` — solo 
`IPedidoRepository`. Si un controller inyecta `AppDbContext`, la arquitectura ya está rota.

---

# PARTE 06

# Repository

# & CQRS

Separar el acceso a datos, y separar la lectura de la escritura

---

## Repository Pattern

# Un mediador entre el dominio y los datos

Consiste en añadir una capa, en forma de mediador, entre nuestro dominio y la capa de datos. 
Su responsabilidad es realizar los procesos de **CRUD** para las entidades de la aplicación.

### Single entity repository

Repositorios que destinan su lógica a **una entidad específica**. Pueden tener consultas propias del negocio: `ObtenerPendientesDeEnvio()`.

### Generics repository

Un repositorio genérico cuyas tareas no están relacionadas a una entidad específica, s
ino que sirve a **todas las entidades** del sistema. Se construye con generics.

---

## Repository · código

# Genérico y específico

```csharp
// ▸ Application/Interfaces — el contrato
// Genérico: sirve para cualquier entidad
public interface IRepository<T> where T : class
{
    Task<T?>      ObtenerAsync(int id);
    Task<List<T>> ListarAsync();
    Task          AgregarAsync(T entidad);
    void          Eliminar(T entidad);
}

// ▸ Infrastructure/Persistence/Repositories — la implementación
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _ctx;
    public Repository(AppDbContext ctx) => _ctx = ctx;

    public Task<T?> ObtenerAsync(int id) => _ctx.Set<T>().FindAsync(id).AsTask();
    public Task<List<T>> ListarAsync() => _ctx.Set<T>().ToListAsync();
    public Task AgregarAsync(T e) => _ctx.Set<T>().AddAsync(e).AsTask();
    public void Eliminar(T e) => _ctx.Set<T>().Remove(e);
}

// Específico: hereda el CRUD y agrega las consultas del negocio
public interface IPedidoRepository : IRepository<Pedido>
{
    Task<List<Pedido>> PendientesDeEnvioAsync();
    Task<decimal>      FacturadoDelMesAsync(int clienteId);
}
```

---

## Unit of Work

# El complemento del Repository

Agrupa operaciones de **distintos repositorios** en una única transacción, para que se confirmen o se descarten juntas.

```csharp
_pedidos.AgregarAsync(pedido);        // nada viajó todavía
_productos.Actualizar(producto);      // nada viajó todavía

await _uow.SaveChangesAsync();         // ← una sola transacción, todo o nada
```

### Conexión con ACID

Esto es exactamente la **atomicidad** que vimos al principio: o se guardan el pedido y el descuento de stock, o no se guarda ninguno de los dos.

En EF Core el `DbContext` ya *es* un Unit of Work. Igual declaramos la interfaz, por dos razones: 
que Application no dependa de EF, y que quede explícito en el código dónde termina la unidad de trabajo.

---

## Unit of Work · el error que van a cometer

# El repositorio nunca commitea

```csharp
// MAL: cada método persiste por su cuenta
public async Task AgregarAsync(Pedido p)
{
    _ctx.Pedidos.Add(p);
    await _ctx.SaveChangesAsync();   // ← acá se rompió la atomicidad
}

// Con eso, este handler guarda el pedido…
await _pedidos.AgregarAsync(pedido);
producto.DescontarStock(cantidad);   // …y si ESTO falla, el pedido ya está guardado.

// BIEN: el repositorio prepara, el caso de uso decide cuándo confirmar
public interface IPedidoRepository
{
    Task<Pedido?> ObtenerAsync(int id);
    Task AgregarAsync(Pedido p);
    void Actualizar(Pedido p);
    // NO hay SaveChangesAsync acá
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### Por qué es tan peligroso

El código **parece** correcto: compila, anda en las pruebas manuales, y falla el día 
que algo revienta a la mitad. Solo el caso de uso sabe dónde termina la unidad de trabajo — el repositorio no puede saberlo.

---

## Unit of Work · la trampa de DI

# Los dos tienen que compartir el mismo DbContext

```csharp
// Infrastructure/Persistence/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;
    public UnitOfWork(AppDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}

// Program.cs — los tres Scoped: comparten la MISMA instancia por request
builder.Services.AddDbContext<AppDbContext>(...);         // Scoped
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Si registrás el UnitOfWork como Transient…
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
// …recibe un DbContext DISTINTO del que usó el repositorio.
// SaveChanges no ve los cambios y guarda 0 filas.
// Sin excepción. Sin error. Sin nada.
```

### Bug silencioso

El endpoint devuelve 201 Created, el log no dice nada, y en la base no hay ninguna fila. 
Es el tipo de error que se lleva una tarde entera si no sabés que existe.

---

## CQRS

# Command and Query Responsibility Segregation

Un patrón en el que se separa la responsabilidad de **lectura** y **escritura** de datos en Queries y Commands.

### Commands

Objetos que tienen la responsabilidad de realizar los procesos de **escritura** — INSERT, UPDATE, DELETE — en la base de datos.

Cuidan las reglas de negocio.

### Queries

Tienen la responsabilidad de **leer**: buscar por nombre, armar un reporte, paginar. Devuelven un **DTO** y **nunca** deben modificar un dato.

Cuidan la performance.

---

## CQRS · escritura

# El Command y su Handler

```csharp
// Vive en Application. No sabe de HTTP ni de SQL.
public record CrearPedidoCommand(int ClienteId, List<ItemDto> Items);

public class CrearPedidoCommandHandler
{
    private readonly IPedidoRepository    _pedidos;
    private readonly IProductoRepository  _productos;
    private readonly CalculadorDescuentos _descuentos;
    private readonly INotificador         _notificador;
    private readonly IUnitOfWork          _uow;

    public CrearPedidoCommandHandler(IPedidoRepository p, IProductoRepository pr,
        CalculadorDescuentos d, INotificador n, IUnitOfWork uow)
        => (_pedidos, _productos, _descuentos, _notificador, _uow) = (p, pr, d, n, uow);

    public async Task<int> Handle(CrearPedidoCommand cmd)
    {
        var pedido = Pedido.Crear(cmd.ClienteId);       // la regla vive en el dominio

        foreach (var i in cmd.Items)
        {
            var producto = await _productos.ObtenerAsync(i.ProductoId)
                ?? throw new ProductoInexistente(i.ProductoId);

            producto.DescontarStock(i.Cantidad);         // valida adentro, no afuera
            pedido.AgregarItem(producto, i.Cantidad);
        }

        pedido.AplicarDescuentos(_descuentos);
        await _pedidos.AgregarAsync(pedido);
        await _uow.SaveChangesAsync();                  // una sola transacción
        await _notificador.NotificarAsync(pedido);

        return pedido.Id;
    }
}
```

---

## CQRS · lectura

# La Query devuelve un DTO

```csharp
public record PedidosDelClienteQuery(int ClienteId, int Pagina = 1);

public record PedidoResumenDto(int Id, DateTime Fecha, decimal Total, int Items);

public class GetPedidosDelClienteQueryHandler
{
    private readonly AppDbContext _ctx;

    public async Task<List<PedidoResumenDto>> Handle(PedidosDelClienteQuery q)
        => await _ctx.Pedidos
            .AsNoTracking()                              // solo lectura: sin fotocopias
            .Where(p => p.ClienteId == q.ClienteId)
            .OrderByDescending(p => p.Fecha)
            .Skip((q.Pagina - 1) * 20).Take(20)
            .Select(p => new PedidoResumenDto(       // proyecta, no trae la entidad
                p.Id, p.Fecha, p.Total, p.Items.Count))
            .ToListAsync();
}
```

Del lado de lectura se puede saltear el Repository y consultar directo: no 
hay reglas de negocio que proteger, y sí hay performance que cuidar. Esa asimetría es justamente el punto de CQRS.

---

## El resultado

# El controller inyecta los handlers y nada más

```csharp
[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    // Un handler por caso de uso. Sin Service intermedio, sin librerías.
    private readonly CrearPedidoCommandHandler       _crear;
    private readonly GetPedidoByIdQueryHandler       _obtener;

    public PedidosController(CrearPedidoCommandHandler crear,
                              GetPedidoByIdQueryHandler obtener)
        => (_crear, _obtener) = (crear, obtener);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearPedidoCommand cmd)
    {
        var id = await _crear.Handle(cmd);            // una línea
        return CreatedAtAction(nameof(Obtener), new { id }, null);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var dto = await _obtener.Handle(new GetPedidoByIdQuery(id));
        return dto is null ? NotFound() : Ok(dto);
    }
}
```

### Comparen con el del principio

Cinco responsabilidades pasaron a una: **traducir HTTP**. No valida negocio, no calcula, no persiste, no notifica. 
Y el caso de uso se puede testear **sin base de datos y sin servidor de mail**.

Notá que no hay ningún `try/catch`: si el dominio lanza `DomainException`, la atrapa el `ExceptionMiddleware` y la traduce a 400.

---

## Cómo saber si la arquitectura está bien

# Una sola prueba, y es objetiva

```csharp
[Fact]
public async Task No_persiste_nada_si_un_producto_no_tiene_stock()
{
    // Mocks: ni base de datos, ni servidor de mail, ni API levantada
    var pedidos   = Substitute.For<IPedidoRepository>();
    var productos = Substitute.For<IProductoRepository>();
    var uow       = Substitute.For<IUnitOfWork>();
    var notif     = Substitute.For<INotificador>();

    productos.ObtenerAsync(1).Returns(new Producto("Teclado", 25000, stock: 1));

    var handler = new CrearPedidoCommandHandler(pedidos, productos, uow, notif);
    var cmd     = new CrearPedidoCommand(clienteId: 1,
                      items: [new ItemDto(ProductoId: 1, Cantidad: 5)]);

    await Assert.ThrowsAsync<DomainException>(() => handler.Handle(cmd));

    await uow.DidNotReceive().SaveChangesAsync();   // no se guardó nada
    await notif.DidNotReceive().NotificarAsync(Arg.Any<Pedido>());
}
```

### El criterio de corrección del trabajo práctico

Si este test sale, la arquitectura está bien puesta: las dependencias están invertidas y 
las capas separadas. **Si no sale, hay algo concreto acoplado** — y el test te dice exactamente 
qué, porque es justamente lo que no podés reemplazar por un mock.

---

## Para saber que existe

# MediatR, el paso siguiente

Con veinte casos de uso, el controller termina inyectando cinco handlers por constructor. 
Hay una librería que resuelve eso, y la van a encontrar en casi cualquier proyecto .NET real.

### Qué hace

El controller deja de conocer los handlers y depende de un único **mediador**, que busca solo a quién le corresponde cada mensaje.

```csharp
var id = await _mediator.Send(cmd);
```

Implementa el patrón **Mediator** del GoF: los objetos no se comunican entre sí directamente, sino a través de un intermediario.

### Por qué hoy no lo usamos

* Agrega una dependencia y una capa de indirección
* Con pocos endpoints no resuelve ningún problema real
* Esconde justo lo que queremos que vean: **quién llama a quién**

Primero el patrón. La librería, después.

Si en una entrevista les preguntan por CQRS en .NET, va a aparecer MediatR. Conviene que sepan que existe — 
y más todavía que sepan que **CQRS se hace perfectamente sin él**, como lo hicimos hoy.

---

## Cierre de la parte 06

# Repository, Unit of Work y CQRS son tres formas del mismo principio.

Poner una abstracción entre dos partes que no deberían conocerse. Eso es la D de SOLID, aplicada tres veces.

---

# Trabajo práctico

## Sistema de Pedidos en capas

1. Diseñar el **DER** del dominio asignado y llevarlo a diagrama de tablas.
2. Implementarlo con **EF Core Code First**, con migrations versionadas en el repositorio.
3. Estructurar la solución en los **cuatro proyectos**: Domain, Application, Infrastructure y Api — con Domain sin referencias a nadie.
4. Registrar todas las dependencias en el contenedor, con el **lifetime justificado** por escrito.
5. Implementar al menos un **Command** y una **Query** con sus Handlers, usando Repository e **IUnitOfWork**.
6. Incluir `DomainException` y un `ExceptionMiddleware` que la traduzca a 400.
7. Incluir un **test unitario** del caso de uso que corra sin base de datos.
8. Versionar un `AGENTS.md` con las convenciones del proyecto.
9. Entregar un `PROMPTS.md` con los prompts usados, qué devolvió la IA y qué corrigieron a mano.

---

## Proyecto de Software · Clase 4

# Fin

IA para programar · Base de datos · ORM · Entity Framework Core · SOLID · Layers · Dependency Injection · Repository · CQRS

* Prof. Leonardo Julián Cabral
"
El flujo de una puja es: "Inicio: El proceso arranca con la acción principal Usuario Puja.

Condición 1 - ¿Activa?: Si la validación es NO, el flujo se desvía y termina en un Error 400. Si es SÍ, el proceso continúa.

Condición 2 - ¿Monto OK?: Si el monto NO es válido, el flujo termina en un Error 400. Si es SÍ, se avanza a la siguiente validación.

Condición 3 - ¿Saldo OK?: Si el saldo NO es suficiente o válido, el flujo termina en un Error 422. Si es SÍ, el sistema entra en una fase de transacción.

Proceso - Transacción Atómica: En este bloque se ejecutan cuatro pasos secuenciales: 1. Congelar saldo nuevo. 2. Liberar saldo anterior. 3. Registrar Puja líder. 4. Escribir en Ledger.

Condición 4 - ¿< 60s al cierre?: (Nota: El diagrama usa el código HTML &lt;, que significa "menor que"). Se evalúa si falta menos de un minuto. Si la respuesta es SÍ, se ejecuta la acción Extender +2 min y Log Auditoría, para luego finalizar en OK 200. Si la respuesta es NO, el flujo avanza directamente al final con OK 200."

Flujo del Proceso: Cierre de Subastas (Worker Job)
Inicio: El proceso se dispara automáticamente a través de un Worker Job.

Acción Inicial: Se ejecuta la tarea Buscar subastas vencidas.

Condición 1 - ¿Quedan?: El sistema verifica si hay subastas vencidas en la lista para procesar.

Si la respuesta es NO (ya no hay subastas que procesar), el flujo termina en Fin.

Si la respuesta es SÍ, el proceso toma una subasta y avanza a la siguiente validación.

Condición 2 - ¿Tiene Pujas?: Se evalúa si la subasta vencida actual recibió alguna oferta.

Camino "NO" (Sin pujas): Se ejecuta la acción Pasar a DESIERTA y Log Auditoría. Una vez completado este paso, el flujo regresa al inicio del ciclo en la Condición 1 (¿Quedan?) para evaluar la siguiente subasta.

Camino "SÍ" (Con pujas): Se ejecuta la acción Pasar a FINALIZADA y el flujo continúa hacia el proceso de liquidación.

Proceso - Liquidación Atómica: Al finalizar con éxito una subasta con pujas, se ejecuta este bloque que contiene tres pasos secuenciales:

Debitar a Comprador

Acreditar a Vendedor

Escribir en Ledger

Acción Final del Ciclo: Una vez terminada la Liquidación Atómica, se ejecuta el Log Auditoría: Venta. Tras registrar esto, el flujo regresa al inicio del ciclo en la Condición 1 (¿Quedan?) para procesar la siguiente subasta en cola, repitiendo el proceso hasta que no quede ninguna.


Este es el diagrama de base de datos SUGERIDO: "Aquí tienes toda la información de los tres diagramas recopilada en un solo texto para que puedas copiarla fácilmente:

1. Flujo del Proceso: Usuario Puja
Inicio: El proceso arranca con la acción principal Usuario Puja.

Condición 1 - ¿Activa?: Si la validación es NO, el flujo se desvía y termina en un Error 400. Si es SÍ, el proceso continúa.

Condición 2 - ¿Monto OK?: Si el monto NO es válido, el flujo termina en un Error 400. Si es SÍ, se avanza a la siguiente validación.

Condición 3 - ¿Saldo OK?: Si el saldo NO es suficiente o válido, el flujo termina en un Error 422. Si es SÍ, el sistema entra en una fase de transacción.

Proceso - Transacción Atómica (Registro de Puja): En este bloque se ejecutan cuatro pasos secuenciales:

Congelar saldo nuevo.

Liberar saldo anterior.

Registrar Puja líder.

Escribir en Ledger: Puja.

Condición 4 - ¿< 60s al cierre?: Se evalúa si falta menos de 60 segundos para el cierre de la subasta.

Si la respuesta es SÍ, se ejecuta la acción Extender +2 min y Log Auditoría, para luego finalizar en OK 200.

Si la respuesta es NO, el flujo avanza directamente al final con OK 200 (Subasta Actualizada con Puja Líder).

2. Flujo del Proceso: Cierre de Subastas (Worker Job Asíncrono)
Inicio: El proceso se dispara automáticamente a través de un Worker Job.

Acción Inicial: Se ejecuta la tarea Buscar subastas vencidas.

Condición 1 - ¿Quedan?: El sistema verifica si hay subastas vencidas en la lista para procesar.

Si la respuesta es NO (ya no hay subastas que procesar), el flujo termina en Fin.

Si la respuesta es SÍ, el proceso toma una subasta y avanza a la siguiente validación.

Condición 2 - ¿Tiene Pujas?: Se evalúa si la subasta vencida actual recibió alguna oferta.

Camino "NO" (Sin pujas): Se ejecuta la acción Pasar a DESIERTA y Log Auditoría. Una vez completado este paso, el flujo regresa al inicio del ciclo en la Condición 1 (¿Quedan?) para evaluar la siguiente subasta.

Camino "SÍ" (Con pujas): Se ejecuta la acción Pasar a FINALIZADA y el flujo continúa hacia el proceso de liquidación.

Proceso - Liquidación Atómica (Liquidación de Venta): Al finalizar con éxito una subasta con pujas, se ejecuta este bloque que contiene tres pasos secuenciales:

Debitar a Comprador.

Acreditar a Vendedor.

Escribir en Ledger: Venta.

Acción Final del Ciclo: Una vez terminada la Liquidación Atómica, se ejecuta el Log Auditoría: Venta. Tras registrar esto, el flujo regresa al inicio del ciclo en la Condición 1 (¿Quedan?) para procesar la siguiente subasta en cola, repitiendo el proceso hasta que no quede ninguna.

3. Diagrama Entidad-Relación (Base de Datos)
Tablas y Columnas
USUARIO

int id (PK)

string email

string nombre

string password_hash

datetime fecha_registro

CATEGORIA

int id (PK)

string nombre

string url_icono

SUBASTA

int id (PK)

int vendedor_id (FK)

int categoria_id (FK)

string titulo

string descripcion

string url_imagen

decimal precio_base

decimal incremento_minimo

datetime fecha_inicio

datetime fecha_fin

string estado (PROGRAMADA, ACTIVA, FINALIZADA, DESIERTA)

int version (Para Optimistic Locking)

BILLETERA

int id (PK)

int usuario_id (FK)

decimal saldo_total

decimal saldo_retenido

decimal saldo_disponible

int version (Para Optimistic Locking)

PUJA

int id (PK)

int subasta_id (FK)

int comprador_id (FK)

decimal monto

datetime fecha_puja

TRANSACCION_LEDGER

int id (PK)

int billetera_id (FK)

string tipo (DEPOSITO, RETENCION, LIBERACION, PAGO, COBRO)

decimal monto

datetime fecha

int subasta_id (FK) - Opcional (trazabilidad)

AUDITORIA_LOG

int id (PK)

string entidad (Ej: SUBASTA, BILLETERA, SISTEMA)

int entidad_id (ID del registro afectado)

string accion (Ej: EXTENSION_TIEMPO, CIERRE_WORKER)

int usuario_id (FK) - Opcional (Null si fue el Worker)

string detalle_json (Payload con los cambios)

datetime fecha

Relaciones entre Entidades
USUARIO → BILLETERA: posee (1:1) - Un Usuario posee exactamente una Billetera.

USUARIO → SUBASTA: publica - Un Usuario puede publicar muchas Subastas (relacionado por vendedor_id).

USUARIO → PUJA: realiza - Un Usuario puede realizar muchas Pujas (relacionado por comprador_id).

USUARIO → AUDITORIA_LOG: gatilla accion (opcional) - Las acciones de un Usuario pueden generar múltiples registros de auditoría.

CATEGORIA → SUBASTA: clasifica - Una Categoría clasifica múltiples Subastas.

SUBASTA → PUJA: recibe - Una Subasta recibe múltiples Pujas.

SUBASTA → TRANSACCION_LEDGER: justifica - Una Subasta justifica uno o múltiples registros en el Ledger.

BILLETERA → TRANSACCION_LEDGER: registra movimientos - Una Billetera registra múltiples movimientos en el Ledger."
