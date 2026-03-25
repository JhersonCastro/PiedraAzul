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

Antes de comenzar debes tener instalado:

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

