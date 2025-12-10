# 🐳 GTAV Injector - Servidor Docker

## 🚀 Inicio Rápido

### Construcción y ejecución:
```bash
# Construir y levantar el servidor
docker-compose up --build

# En segundo plano
docker-compose up -d --build
```

### 🔗 Acceso al servidor:
- **API Base**: `http://localhost:4569/api`
- **Panel Web**: `http://localhost:4569`
- **Endpoint Version**: `http://localhost:4569/api/version`
- **Endpoint Validate**: `http://localhost:4569/api/validate`

## 📊 Endpoints disponibles:

### GET `/api/version`
```json
{
  "success": true,
  "version": "1.0.7",
  "timestamp": "2024-12-06T22:30:00.000Z"
}
```

### POST `/api/validate`
**Request:**
```json
{
  "version": "1.0.7"
}
```

**Response:**
```json
{
  "success": true,
  "allowed": true,
  "message": "Versión válida. Acceso permitido.",
  "clientVersion": "1.0.7",
  "serverVersion": "1.0.7"
}
```

## 🔧 Comandos útiles:

```bash
# Ver logs del servidor
docker-compose logs -f

# Parar el servidor
docker-compose down

# Reconstruir completamente
docker-compose down && docker-compose up --build

# Ver estado del contenedor
docker ps
```

## 📁 Archivos importantes:
- `Dockerfile` - Configuración del contenedor
- `docker-compose.yml` - Orquestación del servicio
- `version-server.js` - Servidor Express
- `config.json` - Configuración de versiones