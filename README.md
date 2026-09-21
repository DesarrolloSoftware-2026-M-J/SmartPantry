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
    yarn test --watch=false
