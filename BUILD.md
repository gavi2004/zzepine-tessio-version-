# GTAV Injector Enhanced - Build Instructions

## 🛠️ Compilación

### Opción 1: Visual Studio

1. Abre `GTAV-Injector.csproj` en Visual Studio 2022
2. Selecciona configuración **Release**
3. Click derecho en el proyecto → **Publish**
4. Selecciona **Folder** como target
5. Click en **Publish**

### Opción 2: Línea de Comandos

```powershell
# Restaurar dependencias
dotnet restore

# Compilar en modo Release
dotnet build -c Release

# Publicar como ejecutable independiente
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable se generará en:
```
bin/Release/net8.0-windows/win-x64/publish/GTAV-Injector.exe
```

---

## 📦 Dependencias

El proyecto usa las siguientes librerías NuGet:

- **Newtonsoft.Json** (13.0.3): Serialización de configuración
- **Octokit** (9.0.0): Verificación de actualizaciones desde GitHub

---

## 🎨 Recursos

### Logo
El logo se encuentra en `Resources/logo.svg` (o `.png`). Puedes reemplazarlo con tu propio diseño.

Dimensiones recomendadas: 200x200px

### Iconos
Para agregar un icono a la aplicación:

1. Coloca tu archivo `.ico` en `Resources/icon.ico`
2. El proyecto ya está configurado para usarlo

---

## 🔧 Configuración del Proyecto

### Cambiar Versión

Edita `GTAV-Injector.csproj`:
```xml
<Version>2.0.0</Version>
```

Y también en `Core/VersionChecker.cs`:
```csharp
private const string CURRENT_VERSION = "2.0.0";
```

### Cambiar Repositorio de GitHub

Edita `Core/VersionChecker.cs`:
```csharp
private const string GITHUB_OWNER = "tu-usuario";
private const string GITHUB_REPO = "tu-repo";
```

---

## 🚀 Crear Release

1. Compila el proyecto en modo Release
2. Crea un archivo ZIP con:
   - `GTAV-Injector.exe`
   - `README.md`
   - Carpeta `Resources/` (si es necesario)

3. Sube a GitHub Releases con tag `v2.0.0`

---

## 🐛 Debugging

Para depurar la inyección:

1. Ejecuta GTA V
2. Inicia el proyecto en modo Debug desde Visual Studio
3. Coloca breakpoints en `InjectionManager.cs`
4. Intenta inyectar una DLL de prueba

---

## ⚠️ Notas Importantes

- **Ejecutar como Administrador**: Necesario para inyectar en procesos
- **Antivirus**: Puede detectar el inyector como falso positivo
- **Windows Defender**: Agregar excepción si es necesario

---

## 📝 TODO / Mejoras Futuras

- [ ] Agregar logs detallados de inyección
- [ ] Soporte para más idiomas
- [ ] Temas personalizables (claro/oscuro)
- [ ] Perfiles de DLLs (guardar/cargar conjuntos)
- [ ] Detección automática de versión del juego
- [ ] Integración con Discord Rich Presence
