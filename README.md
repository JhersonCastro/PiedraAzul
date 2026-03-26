# PiedraAzul

Sistema de gestión y reserva de citas médicas diseñado para optimizar el agendamiento de turnos con médicos y terapeutas. La aplicación permite según el rol dentro del sistema:

**Médico:** Visualizar la lista de pacientes pendientes

**Agendador:** 
* Listar las citas médicas de un médico/terapista en una fecha determinada, visualiza el listado y cantidad de citas.
* Crear citas de pacientes que se contacten por whatsApp con sus respectivos datos

**Paciente:** 
* Agendar citas mediante la web de manera fácil sin necesidad de usar whastsApp

**Administrador:** 
* Configurara parámetros del sistema para que el sistema de citas autónomo funcione de acuerdo a la disponibilidad de los médicos y terapistas

## Tecnologías

| Tecnología | Versión | Uso |
|------------|---------|-----|
| .NET | 8.0 | Backend API y lógica de negocio |
| Blazor | 8.0 | Frontend SPA (WebAssembly o Server) |
| Entity Framework Core | 8.0 | ORM para acceso a datos |
| PostgreSQL | 16 | Base de datos relacional |
| Docker | - | Contenedor para base de datos |
| Tailwind CSS | 3.x | Estilos del frontend |
| xUnit | - | Pruebas unitarias |
| Git | - | Control de versiones |

## Requisitos previos

- [Visual Studio 2026](https://visualstudio.microsoft.com/) con las cargas de trabajo:
  - Desarrollo de ASP.NET y web
  - Almacenamiento y procesamiento de datos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) En caso de estar en windows es recomendable usar la terminal de WSL instalando WSL e internamente instalar Ubuntu
- [Git](https://git-scm.com/)

## Diseño

Los prototipos de interfaz fueron diseñados en [Figma](https://www.figma.com/es-la/downloads/). y luego se hizo la transición directa a tailwindCSS con -[Tailwind Play](https://play.tailwindcss.com/)

## Base de datos

El proyecto utiliza PostgreSQL en dos modalidades:

### Opción 1: Base de datos en AWS
La base de datos está alojada en AWS. Para desarrollo en equipo.

### Opción 2: Base de datos local con Docker
Para desarrollo sin conexión a internet o pruebas locales:

## 🛠️ 1. Requisitos Previos

Antes de comenzar, debes tener instalado:
1. **[.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** (Verificar que se instale la versión para tu sistema operativo).
2. **[Docker Desktop](https://www.docker.com/)** (Debe estar abierto y en ejecución antes de seguir al siguiente paso).

Ejecuta el siguiente comando en **CMD, PowerShell o terminal**:

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

## Connection String

Usa la siguiente cadena de conexión según la opción elegida:

**Opción AWS:**
```json
"DefaultConnection": "Host=ep-purple-tree-acshudb6-pooler.sa-east-1.aws.neon.tech; Database=piedraazuldb; Username=neondb_owner; Password=npg_jZeBbRzS5G3i; SSL Mode=VerifyFull; Channel Binding=Require;"
```

**Opción local con docker:**

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=PiedraAzulDB;Username=postgres;Password=postgres"
```

# Verificar que el contenedor está corriendo

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

Esto eliminará el contenedor pero **no el volumen de datos**.

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

---
# Estructura Global del proyecto

El proyecto está organizado en una solución .NET con los siguientes proyectos:

| Proyecto | Descripción |
|----------|-------------|
| `PiedraAzul/` | API principal. Contiene controladores, lógica de negocio, servicios y configuración del backend |
| `PiedraAzul.Client/` | Frontend desarrollado con Blazor. Contiene páginas, componentes y la interfaz de usuario |
| `PiedraAzul.Shared/` | Modelos compartidos, DTOs y clases que se utilizan tanto en backend como frontend |
| `PiedraAzul.Test/` | Pruebas unitarias con xUnit. Cubre la lógica de negocio y servicios del dominio |

## Documentación

La documentación completa del proyecto se encuentra disponible en la carpeta compartida de Google Drive:

[Documentación del Proyecto](link-del-drive)

**Contenido:**
- **Historias de Usuario y Atributos de Calidad** - Definición de requisitos y escenarios de calidad
- **DiagramasC4.drawio** - Diagramas de arquitectura en modelo C4
- **Atributos de calidad.docx** - Especificación detallada de atributos de calidad priorizados (usabilidad y seguridad)

**Recursos adicionales:**
- [Prototipos en Figma](https://www.figma.com/design/FlGdsvvoSdX8jRe8qSrxtq/Software-III?node-id=0-1&t=SnDIq7n63B9LUn5B-1)
- [Documento atributos de calidad](https://docs.google.com/document/d/1UkVKK1_C14V3rVn2_EX44BS6jK1bsTZ_/edit)
- [Diagramas Modelo C4](https://app.diagrams.net/#G17fLFngDYyHThrFKKZ8bIZPwn7_HlTWcR#%7B%22pageId%22%3A%22zNMGI6wU0Mi8Qe2H5Q59%22%7D)

## Equipo

| Integrante | tareas |
|------------|-----|
| Jherson Andres Castro | Desarrollo, integración, diseño |
| Edier Fabian Dorado | Desarrollo, modelado |
| Juan Fernando Portilla | Desarrollo, modelado, diseño |
| Yezid Esteban Hernandez | Desarrollo, documentación |
