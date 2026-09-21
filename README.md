# SmartPantry

# Desarrollo de Software 2026
Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Buet Mia buetmia-sudo
- Sigales Maria Juliana mjulianasig1-cloud

## Cómo ejecutar
### Requisitos Previos
* [.NET 10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 o 20](https://nodejs.org/en)

### Configuración Inicial
1. Revisar y modificar la cadena de conexión (`ConnectionStrings`) en los archivos `appsettings.json` de los proyectos `SmartPantry.HttpApi.Host` y `SmartPantry DbMigrator`.
2. En la carpeta raíz del proyecto, ejecutar el comando `abp install-libs` para instalar las dependencias de los paquetes del lado del cliente.
3. Ejecutar el proyecto `SmartPantry.DbMigrator` para crear la base de datos inicial y aplicar las migraciones. Este paso es obligatorio en la primera ejecución.

### URLs Locales Resultantes
*   **Backend (API):** `https://localhost:44391`
*   **Frontend (Angular):** `http://localhost:4200`

### Cómo detener los procesos
Para detener cualquiera de los procesos (tanto la API en .NET como el servidor de Angular), dirígete a la terminal donde se está ejecutando y presiona `Ctrl + C`.