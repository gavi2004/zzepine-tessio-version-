# 🚀 Guía Rápida de Instalación y Compilación

## ⚡ INICIO RÁPIDO (5 minutos)

### Paso 1: Instalar .NET 8.0 SDK

**Opción A - Descarga Directa:**
1. Ve a: https://dotnet.microsoft.com/download/dotnet/8.0
2. Descarga ".NET 8.0 SDK" para Windows x64
3. Ejecuta el instalador
4. Reinicia tu terminal/PowerShell

**Opción B - Usando winget (Windows 11):**
```powershell
winget install Microsoft.DotNet.SDK.8
```

**Opción C - Usando Chocolatey:**
```powershell
choco install dotnet-8.0-sdk
```

### Paso 2: Verificar Instalación

Abre PowerShell y ejecuta:
```powershell
dotnet --version
```

Deberías ver algo como: `8.0.xxx`

### Paso 3: Compilar el Proyecto

```powershell
# Navega a la carpeta del proyecto
cd "e:\gta v imyector\GTAV-Injector\GTAV-Injector\new"

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build -c Release

# El ejecutable estará en:
# bin\Release\net8.0-windows\GTAV-Injector.exe
```

### Paso 4: Ejecutar

```powershell
# Ejecutar directamente
dotnet run

# O navegar a la carpeta y ejecutar el .exe
cd bin\Release\net8.0-windows
.\GTAV-Injector.exe
```

---

## 🎨 ALTERNATIVA: Usar Visual Studio

Si prefieres usar Visual Studio (más fácil):

1. **Descargar Visual Studio 2022 Community** (gratis)
   - https://visualstudio.microsoft.com/downloads/

2. Durante la instalación, selecciona:
   - ✅ ".NET desktop development"

3. Abrir el proyecto:
   - File → Open → Project/Solution
   - Selecciona `GTAV-Injector.csproj`

4. Compilar:
   - Build → Build Solution (o presiona F6)

5. Ejecutar:
   - Debug → Start Without Debugging (o presiona Ctrl+F5)

---

## 📦 Crear Ejecutable Portable

Para crear un ejecutable que funcione sin instalar .NET:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

El ejecutable estará en:
```
bin\Release\net8.0-windows\win-x64\publish\GTAV-Injector.exe
```

Este archivo es **portable** y puede ejecutarse en cualquier Windows sin necesidad de instalar .NET.

---

## ⚠️ Solución de Problemas

### Error: "dotnet no se reconoce"
**Solución**: Reinicia PowerShell después de instalar .NET SDK

### Error: "No se puede cargar el archivo o ensamblado"
**Solución**: Ejecuta como Administrador

### Error al compilar
**Solución**: 
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Antivirus bloquea el ejecutable
**Solución**: Agregar excepción en Windows Defender o tu antivirus

---

## 🎯 Checklist de Compilación

- [ ] .NET 8.0 SDK instalado
- [ ] `dotnet --version` funciona
- [ ] `dotnet restore` ejecutado sin errores
- [ ] `dotnet build -c Release` completado
- [ ] Ejecutable generado en `bin\Release\net8.0-windows\`
- [ ] Aplicación se ejecuta correctamente
- [ ] Todas las características funcionan

---

## 📞 Si Tienes Problemas

1. Verifica que .NET 8.0 esté instalado: `dotnet --version`
2. Revisa los errores en la consola
3. Asegúrate de estar en la carpeta correcta
4. Ejecuta como Administrador si es necesario

---

## 🎉 ¡Listo!

Una vez compilado, tendrás el **GTAV Injector Enhanced** completamente funcional con:
- ✅ Todas las características solicitadas
- ✅ UI moderna con parallax
- ✅ Auto-inyector
- ✅ Sistema de actualizaciones
- ✅ Traducción ES/EN

**¡A impresionar a Tessio!** 🚀
