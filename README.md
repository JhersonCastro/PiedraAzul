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
| .NET | 10 | Backend API y lógica de negocio |
| Blazor | 10 | Frontend SPA (WebAssembly o Server) |
| Entity Framework Core | 10 | ORM para acceso a datos |
| PostgreSQL | 17 | Base de datos relacional |
| Docker | - | Contenedor para base de datos |
| Tailwind CSS | 4.x | Estilos del frontend |
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

Antes de comenzar debes tener instalado:

* [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [Docker](https://www.docker.com/)
* Docker Desktop ejecutándose

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

También se crea un **volumen persistente** para no perder los datos.

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
docker ps
```

Deberías ver algo similar a:

```
CONTAINER ID   IMAGE      NAME                 PORTS
xxxxxxx        postgres   piedraazul-postgres  0.0.0.0:5432->5432/tcp
```

---

# Entrar a PostgreSQL desde la terminal

```bash
docker exec -it piedraazul-postgres psql -U postgres -d PiedraAzulDB
```

---

# Detener el contenedor

```bash
docker stop piedraazul-postgres
```

---

# Iniciar nuevamente

```bash
docker start piedraazul-postgres
```

---

# Eliminar el contenedor

Esto eliminará el contenedor pero **no el volumen de datos**.

```bash
docker rm piedraazul-postgres
```

---

# Eliminar también los datos

```bash
docker volume rm postgres_data
```

---

# Notas

* El puerto **5432 debe estar libre** en tu máquina.
* Si cambias usuario o contraseña debes actualizar el **Connection String**.
* El volumen `postgres_data` mantiene los datos aunque se borre el contenedor.

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
- **Documento General del proyecto** El documento con toda la información del proyecto y los requisitos que se cumplierón

**Recursos adicionales:**
- [Historias de usuario](https://docs.google.com/spreadsheets/d/1pCTmF3Cr3cpM7gBGFOtzZ1qOcyH3zOh-8rqVREQ-S7M/edit?gid=209375213#gid=209375213)
- [Prototipos en Figma](https://www.figma.com/design/FlGdsvvoSdX8jRe8qSrxtq/Software-III?node-id=0-1&t=SnDIq7n63B9LUn5B-1)
- [Atributos de calidad](https://docs.google.com/document/d/1UkVKK1_C14V3rVn2_EX44BS6jK1bsTZ_/edit)
- [Diagramas Modelo C4](https://app.diagrams.net/#G17fLFngDYyHThrFKKZ8bIZPwn7_HlTWcR#%7B%22pageId%22%3A%22zNMGI6wU0Mi8Qe2H5Q59%22%7D)
- [Documento del proyecto](https://docs.google.com/document/d/1ULFJibJQNiNmlmFxS50KrworANlOQIKJMVJlV2dhxR4/edit?tab=t.0)
- 

## Equipo

| Integrante | tareas |
|------------|-----|
| Jherson Andres Castro | Desarrollo, integración, diseño |
| Edier Fabian Dorado | Desarrollo, modelado |
| Juan Fernando Portilla | Desarrollo, modelado, diseño |
| Yezid Esteban Hernandez | Desarrollo, documentación |
