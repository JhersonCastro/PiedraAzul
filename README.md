# PostgreSQL con Docker (PiedraAzul)

Este proyecto utiliza **PostgreSQL ejecutándose en Docker** para el entorno de desarrollo.

## Requisitos

Antes de comenzar debes tener instalado:

* [Docker](https://www.docker.com/)
* Docker Desktop ejecutándose

---

# Levantar la base de datos

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

# Connection String

Usa la siguiente cadena de conexión en el proyecto:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=PiedraAzulDB;Username=postgres;Password=postgres"
```

---

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

⚠ Esto eliminará el contenedor pero **no el volumen de datos**.

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
