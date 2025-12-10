# 🚀 Deployment en Coolify - GTAV Injector Server

## 🔧 **Correcciones aplicadas para Coolify:**

### ❌ **Problema original:**
```
Error: unable to start container process: error mounting "/data/coolify/applications/.../config.json" to rootfs at "/app/config.json": not a directory
```

### ✅ **Solución implementada:**

#### 1. **docker-compose.yml actualizado:**
- **Eliminado** volumen problemático: `./config.json:/app/config.json:ro`
- **Mantenido** solo configuración esencial
- **Puerto**: 4569 expuesto correctamente

#### 2. **Dockerfile mejorado:**
- **Auto-genera** `config.json` por defecto si no existe
- **Crea** interfaz web básica automáticamente
- **Instala** dependencias automáticamente
- **No depende** de archivos externos montados

#### 3. **.dockerignore optimizado:**
- **Excluye** todo el código C# innecesario
- **Incluye** solo archivos esenciales del servidor
- **Reduce** tamaño del contexto Docker

## 📊 **Configuración por defecto:**

### **config.json automático:**
```json
{
  "version": "1.0.7",
  "allowedVersions": ["1.0.7"],
  "adminKey": "admin123",
  "updateTimestamp": "2025-12-10T03:18:00.000Z"
}
```

### **Endpoints disponibles:**
- **Health**: `https://version-check.bitforges.com/`
- **Version**: `https://version-check.bitforges.com/api/version`
- **Validate**: `https://version-check.bitforges.com/api/validate`

## 🚀 **Para deployar en Coolify:**

1. **Push** los cambios al repositorio
2. **Coolify** detectará automáticamente:
   - `Dockerfile`
   - `docker-compose.yml`
   - Puerto `4569`
3. **Build** será exitoso sin errores de montaje
4. **Servidor** iniciará automáticamente

## ✅ **Verificación post-deployment:**

```bash
# Verificar health
curl https://version-check.bitforges.com/

# Verificar API version
curl https://version-check.bitforges.com/api/version

# Verificar validación
curl -X POST https://version-check.bitforges.com/api/validate \
     -H "Content-Type: application/json" \
     -d '{"version":"1.0.7"}'
```

## 📋 **Respuestas esperadas:**

### `/api/version`:
```json
{
  "success": true,
  "version": "1.0.7",
  "timestamp": "2025-12-10T03:18:00.000Z"
}
```

### `/api/validate`:
```json
{
  "success": true,
  "allowed": true,
  "message": "Versión válida. Acceso permitido.",
  "clientVersion": "1.0.7",
  "serverVersion": "1.0.7"
}
```

## 🔄 **Ahora el auto-inyector:**
- ✅ Se conectará a `https://version-check.bitforges.com/api`
- ✅ Validará versión 1.0.7
- ✅ Auto-inyectará cuando detecte `GTA5_enhanced.exe` o `GTA5.exe`