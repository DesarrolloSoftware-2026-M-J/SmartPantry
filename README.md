# SmartPantry

# Desarrollo de Software 2026
Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Buet Mia (buetmia-sudo)
- Sigales Maria Juliana (mjulianasig1-cloud)

## Requisitos
Para poder obtener este repositorio y ejecutar la aplicación desde cero, es necesario contar con:
* Visual Studio 2022 o 2026 (con la carga de trabajo "Desarrollo de ASP.NET y web").
* Node.js 24.15.0 o superior.
* Yarn 1.22.x.
* SQL Server Developer o Express.
* SQL Server Management Studio (SSMS).
* ABP Studio.
* Git.

## Configuración local
Antes de la primera ejecución, es obligatorio apuntar la solución a tu base de datos local. Debes revisar y modificar el valor de `ConnectionStrings:Default` en los siguientes dos archivos:
1. `src/SmartPantry.HttpApi.Host/appsettings.json`
2. `src/SmartPantry.DbMigrator/appsettings.json`

**Cadena de conexión local utilizada:**
```json
"ConnectionStrings": {
  "Default": "Server=LAPTOP-5SVGC5EI;Database=SmartPantry;Trusted_Connection=True;TrustServerCertificate=True;"
} 
```

# Cómo arrancar el proyecto:

## Restaurar paquetes de .NET y librerías de cliente:
En la carpeta raíz de la solución, ejecuta: 
    dotnet restore
    abp install-libs

## Instalar paquetes de Angular:
Navega a la carpeta del frontend y utiliza Yarn:
    cd angular
    yarn install  

## Crear la base de datos y aplicar migraciones:
Puedes correr el proyecto migrador desde Visual Studio o por terminal:
    cd src/SmartPantry.DbMigrator
    dotnet run

## Iniciar el Backend (API):
    cd src/SmartPantry.HttpApi.Host
    dotnet run

## Iniciar el Frontend (Angular):
Abre una nueva terminal, ve a la carpeta de Angular y arranca el servidor web:
    cd angular
    yarn start

### URLs Locales Resultantes
*   **Backend (API):** `https://localhost:44391`
*   **Frontend (Angular):** `http://localhost:4200`

### Cómo detener los procesos
Para detener cualquiera de los procesos (tanto la API en .NET como el servidor de Angular), dirígete a la terminal donde se está ejecutando y presiona `Ctrl + C`.

## Verificación
Comandos utilizados por el grupo para comprobar la correcta compilación y ejecución de las pruebas automatizadas:

* Backend (.NET): (Ejecutados desde la raíz del repositorio)
    dotnet build ./SmartPantry.slnx --configuration Release --no-restore
    dotnet test ./SmartPantry.slnx --configuration Release --no-build

* Frontend (Angular): (Ejecutados desde la carpeta /angular)
    yarn build
    yarn test --no-watch

## TP10 - Pantalla "Mi despensa" (RF-11)

### Ítems consumidos
Los ítems consumidos **no se eliminan automáticamente**: siguen apareciendo en la lista con el estado "Consumido", como historial. Un ítem consumido no se puede editar ni volver a marcar como consumido (la pantalla oculta esos botones), pero sí se puede eliminar. El BackEnd conserva el ítem con su estado cambiado.

### Uso de `ListService`
Se usó `ListService` de ABP para el listado. Maneja la paginación (`skipCount` y `maxResultCount`), vuelve a pedir los datos con `get()` después de editar, consumir o eliminar, y cancela una consulta anterior si se pide otra (`switchMap`). El error se captura dentro de la función que crea la consulta (`catchError`), para que un fallo no corte el flujo y el botón "Reintentar" pueda volver a consultar.

### Criterio de fechas
El vencimiento es una fecha sin hora (`DateOnly`) y no tiene zona horaria. En la pantalla se muestra tal como se guardó (`dd/MM/yyyy`), sin conversiones. El worker de avisos de vencimiento calcula "hoy" en UTC y corre cada hora, por lo que un cambio de fecha o de estado se refleja en los avisos en la siguiente corrida.

### Manejo de errores
Las llamadas del listado, la edición, el consumo y la eliminación usan `skipHandleError`, para mostrar mensajes propios en lugar del cuadro genérico de ABP:
- Fallo al cargar: aviso con botón "Reintentar".
- Ítem inexistente o de otro usuario (404): aviso "El ítem ya no existe" y recarga de la lista.
- Otros fallos: aviso "No pudimos completar la acción".

Un ítem de otro usuario se responde con **404** (no se revela que existe).
