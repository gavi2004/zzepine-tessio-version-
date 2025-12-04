# 🔧 Solución de Problemas de Compilación

## Errores Comunes y Soluciones

### ❌ Error: "No se puede cargar el proyecto" o "SDK no encontrado"
**Solución:**
1. Instalar .NET 8.0 SDK desde: https://dotnet.microsoft.com/download/dotnet/8.0
2. Reiniciar Visual Studio

### ❌ Error: "No se pueden restaurar los paquetes NuGet"
**Solución:**
1. Click derecho en la solución → "Restore NuGet Packages"
2. O desde consola: `dotnet restore`

### ❌ Error: "WPF no está disponible"
**Solución:**
1. Abrir Visual Studio Installer
2. Modificar instalación
3. Agregar ".NET Desktop Development" workload

### ❌ Error: "Cannot resolve assembly or namespace"
**Solución:**
1. Limpiar solución: Build → Clean Solution
2. Reconstruir: Build → Rebuild Solution

## 🚀 Compilación Manual (Alternativa)

Si Visual Studio da problemas, usar línea de comandos:

```bash
# Navegar a la carpeta del proyecto
cd ruta/del/proyecto

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build --configuration Release

# O generar ejecutable standalone
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./dist
```

## 📋 Requisitos del Sistema

- **Windows 10/11**
- **.NET 8.0 SDK** instalado
- **Visual Studio 2022** (Community, Professional, o Enterprise) 
- **Workload**: .NET Desktop Development

## 📞 Si nada funciona

1. Verificar que tienen la versión correcta de Visual Studio
2. Descargar el ejecutable precompilado del repositorio
3. O usar el comando `dotnet publish` desde línea de comandos