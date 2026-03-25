# Proyecto PiedraAzul - Guía de Inicio Rápido (Desde Cero)

Esta guía te ayudará a configurar y ejecutar el proyecto localmente sin problemas, incluso si nunca habías ejecutado el código en tu computadora.

---

## 🛠️ 1. Requisitos Previos

Antes de comenzar, debes tener instalado:
1. **[.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** (Verificar que se instale la versión para tu sistema operativo).
2. **[Docker Desktop](https://www.docker.com/)** (Debe estar abierto y en ejecución antes de seguir al siguiente paso).

---

## 🐳 2. Levantar la Base de Datos (PostgreSQL en Docker)

El proyecto requiere una base de datos PostgreSQL ejecutándose en tu computadora. Para mantener la consistencia con el resto del equipo, usaremos la configuración oficial, puerto y volumen compartido predeterminados.

Abre tu terminal (PowerShell o CMD) en la carpeta raíz del proyecto y ejecuta la creación del contenedor:

```bash
docker run -d --name piedraazul-postgres \
-e POSTGRES_DB=PiedraAzulDB \
-e POSTGRES_USER=postgres \
-e POSTGRES_PASSWORD=postgres \
-p 5432:5432 \
-v postgres_data:/var/lib/postgresql/data \
postgres
```

Esto creará un contenedor con:
* **Base de datos:** PiedraAzulDB
* **Usuario:** postgres
* **Contraseña:** postgres
* **Puerto:** 5432

*(También se crea un volumen permanente `postgres_data` para no perder los datos)*.

---

## ⚙️ 3. Instalar la Herramienta de Entity Framework

Para que .NET pueda comunicarse con la base de datos y crear las tablas automáticamente desde el código, debes asegurarte de tener la herramienta global del CLI instalada:

```bash
dotnet tool install --global dotnet-ef
```
*(Si ya la tenías instalada, la terminal te lo indicará, lo cual está perfecto)*.

---

## 🏗️ 4. Restaurar Dependencias y Migrar Base de Datos

Ahora debes ubicar tu terminal **exactamente en la carpeta del servidor backend**, donde se encuentra el archivo principal del proyecto. 

Navega a la subcarpeta interna:
```bash
cd PiedraAzul/PiedraAzul
```
*(Asegúrate de estar en la ruta donde se encuentra el archivo `PiedraAzul.csproj`)*.

**Restaurar los paquetes de NuGet:**  
Esto descargará todas las librerías necesarias (.dlls) para que el proyecto funcione:
```bash
dotnet restore
```

**Crear y actualizar la Base de Datos:**  
Aplicaremos las migraciones iniciales para que la base de datos quede lista:
```bash
dotnet ef database update
```
*(Debe terminar con un mensaje de "Done" o "Applying migration...")*.

---

## 🚀 5. Ejecutar la Aplicación

Con la base de datos estructurada y los paquetes restaurados, ¡iniciamos el servidor web!

En la misma carpeta (`PiedraAzul/PiedraAzul`), ejecuta:
```bash
dotnet run
```

Una vez termine de compilar, la terminal mostrará un mensaje indicando el puerto, por ejemplo: `Now listening on: http://localhost:5023`.  
**Abre esa dirección en tu navegador web** y verás la aplicación PiedraAzul en pleno funcionamiento.

*(Nota: Para detener el servidor, presiona `Ctrl + C` en esa misma terminal)*.

---

## 🔄 Resumen de Comandos Útiles

Para tus próximas sesiones de programación, el proceso es mucho más simple. Ya no necesitas hacer lo anterior, solo debes:

1. Iniciar Docker Desktop.
2. Encender la base de datos (si la habías apagado):
   ```bash
   docker start piedraazul-postgres
   ```
3. Ejecutar tu servidor en la carpeta del backend (`cd PiedraAzul/PiedraAzul`):
   ```bash
   dotnet run
   ```

### Mantenimiento de contenedores:
- Detener contenedor para liberar memoria: `docker stop piedraazul-postgres`
- Eliminar el contenedor: `docker rm piedraazul-postgres`
- Eliminar toda la base de datos para borrar registros de prueba (⚠ ¡Cuidado!): `docker volume rm postgres_data`
