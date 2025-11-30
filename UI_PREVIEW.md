# 🎨 Visualización de la Interfaz - GTAV Injector Enhanced

## Vista Principal de la Aplicación

```
╔═══════════════════════════════════════════════════════════════════════════════════════╗
║  [🎮 GTAV INJECTOR]  v2.0.0                         [🇺🇸 English ▼]  [─] [☐] [✕]    ║
╠═══════════════════════════════════════════════════════════════════════════════════════╣
║                                                                                       ║
║  ┌─────────────────────────────────────────┐  ┌──────────────────────────────────┐  ║
║  │  📋 Lista de DLLs                       │  │  ⚙️ Configuración                │  ║
║  │  [+ Agregar DLL]                        │  │                                  │  ║
║  │                                         │  │  ┌────────────────────────────┐  │  ║
║  │  ☑ Auto-inyectar al iniciar el juego   │  │  │ 📊 Estado de Versión       │  │  ║
║  │                                         │  │  │ ✅ Actualizado             │  │  ║
║  │  ┌───────────────────────────────────┐ │  │  └────────────────────────────┘  │  ║
║  │  │ ☑  script.dll      🟢 Inyectado   │ │  │                                  │  ║
║  │  │ ☑  menu.dll        🔴 No Inyectado│ │  │  🎮 Tipo de Juego:              │  ║
║  │  │ ☐  trainer.dll     🔴 No Inyectado│ │  │  ○ Legacy (GTA5.exe)            │  ║
║  │  │                                   │ │  │  ● Enhanced (GTA5_Enhanced)     │  ║
║  │  │                                   │ │  │                                  │  ║
║  │  └───────────────────────────────────┘ │  │  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │  ║
║  │                                         │  │                                  │  ║
║  │  [🚀 Iniciar Juego] [💉 Inyectar DLLs] │  │  🎯 Lanzador:                   │  ║
║  │  [❌ Cerrar Juego]                      │  │  ● Rockstar Games               │  ║
║  │                                         │  │  ○ Epic Games                   │  ║
║  └─────────────────────────────────────────┘  │  ○ Steam                        │  ║
║                                                │                                  │  ║
║  ┌─────────────────────────────────────────┐  │  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │  ║
║  │ 📊 Estado: Listo                        │  │                                  │  ║
║  │ 🎮 Juego: ✅ En Ejecución               │  │  📋 Requisitos:                 │  ║
║  └─────────────────────────────────────────┘  │  • Windows 10/11 (64-bit)       │  ║
║                                                │  • .NET 8.0 Runtime             │  ║
║                                                │  • GTA V instalado              │  ║
║                                                │  • Derechos de administrador    │  ║
║                                                └──────────────────────────────────┘  ║
╚═══════════════════════════════════════════════════════════════════════════════════════╝
```

---

## 🎨 Paleta de Colores

### Fondo
- **Gradiente Principal**: `#0F0F0F` → `#1A1A2E` → `#16213E`
- **Capas Parallax**: Tonos azules con opacidad (`#0E4C92`, `#2E5EAA`, `#4A90E2`)

### Elementos de UI
- **Fondo Paneles**: `#252525`
- **Fondo Secundario**: `#1E1E1E`
- **Texto Principal**: `#FFFFFF`
- **Texto Secundario**: `#AAAAAA`

### Botones
- **Primario**: `#4A90E2` → Hover: `#5BA3F5`
- **Éxito**: `#4CAF50` → Hover: `#66BB6A`
- **Peligro**: `#F44336` → Hover: `#EF5350`
- **Advertencia**: `#FF9800` → Hover: `#FFA726`

### Estados
- **Inyectado**: `#4CAF50` (Verde)
- **No Inyectado**: `#F44336` (Rojo)
- **Actualizado**: `#4CAF50` (Verde)
- **Desactualizado**: `#FF9800` (Naranja)

---

## 🎬 Animaciones

### Parallax
```
Capa 1: Movimiento horizontal -100px en 20s (AutoReverse)
Capa 2: Movimiento horizontal -150px en 30s (AutoReverse)
```

### Botones
```
Hover: Cambio de color suave (0.2s)
Click: Efecto de presión
```

---

## 📱 Responsive Design

### Tamaño Mínimo
- **Ancho**: 800px
- **Alto**: 600px

### Tamaño Inicial
- **Ancho**: 1000px
- **Alto**: 700px

### Distribución
- **Panel Izquierdo**: 2/3 del ancho (Lista de DLLs)
- **Panel Derecho**: 1/3 del ancho (Configuración)

---

## 🔄 Estados de la Aplicación

### 1. Juego No Ejecutándose
```
[🚀 Iniciar Juego]  ← Habilitado
[💉 Inyectar DLLs]  ← Deshabilitado
[❌ Cerrar Juego]   ← Deshabilitado

Estado: "Juego: No Ejecutándose" (Rojo)
```

### 2. Juego Ejecutándose
```
[🚀 Iniciar Juego]  ← Deshabilitado
[💉 Inyectar DLLs]  ← Habilitado
[❌ Cerrar Juego]   ← Habilitado

Estado: "Juego: En Ejecución" (Verde)
```

### 3. Auto-Inyector Activo
```
☑ Auto-inyectar al iniciar el juego

Cuando el juego inicia:
→ Espera 2 segundos
→ Inyecta automáticamente DLLs habilitadas
→ Actualiza estados a "Inyectado" (Verde)
```

---

## 🌐 Traducción

### Selector de Idioma
```
┌─────────────────┐
│ 🇺🇸 English  ▼ │  ← Click para cambiar
│ 🇪🇸 Español  ▼ │
└─────────────────┘
```

### Textos Traducidos
- Todos los botones
- Todos los labels
- Mensajes de estado
- Diálogos de confirmación
- Mensajes de error

---

## 🎯 Interacciones del Usuario

### Agregar DLL
1. Click en `[+ Agregar DLL]`
2. Se abre diálogo de selección de archivos
3. Seleccionar uno o más archivos `.dll`
4. Aparecen en la lista con checkbox habilitado

### Remover DLL
1. Click derecho en una DLL (o botón Remove)
2. Se elimina de la lista
3. Configuración se guarda automáticamente

### Inyectar DLLs
1. Click en `[💉 Inyectar DLLs]`
2. Barra de progreso (opcional)
3. Estados cambian a "Inyectado" (Verde)
4. Mensaje de confirmación

### Auto-Inyección
1. Activar checkbox `☑ Auto-inyectar`
2. Timer detecta cuando GTA V inicia
3. Espera 2 segundos (carga del juego)
4. Inyecta automáticamente
5. Notificación visual

---

## 📊 Indicadores Visuales

### DLL en Lista
```
┌──────────────────────────────────────┐
│ ☑  script.dll      🟢 Inyectado     │  ← Verde = Inyectado
│ ☑  menu.dll        🔴 No Inyectado  │  ← Rojo = No Inyectado
│ ☐  trainer.dll     🔴 No Inyectado  │  ← Checkbox deshabilitado
└──────────────────────────────────────┘
```

### Estado de Versión
```
┌────────────────────────────┐
│ 📊 Estado de Versión       │
│ ✅ Actualizado             │  ← Verde, sin botón
└────────────────────────────┘

┌────────────────────────────┐
│ 📊 Estado de Versión       │
│ ⚠️ Desactualizado          │  ← Naranja
│ [🔄 Actualizar]            │  ← Botón visible
└────────────────────────────┘
```

---

## 🎨 Efectos Visuales

### Sombras
- Paneles: `box-shadow: 0 4px 8px rgba(0,0,0,0.3)`
- Botones hover: `box-shadow: 0 2px 4px rgba(74,144,226,0.5)`

### Bordes Redondeados
- Ventana principal: `10px`
- Paneles: `8px`
- Botones: `5px`

### Opacidad
- Parallax Capa 1: `0.3`
- Parallax Capa 2: `0.2`
- Elementos deshabilitados: `0.5`

---

## 🚀 Flujo de Trabajo Típico

1. **Inicio de la aplicación**
   - Carga configuración guardada
   - Verifica actualizaciones
   - Detecta si GTA V está corriendo

2. **Usuario agrega DLLs**
   - Click en "+ Agregar DLL"
   - Selecciona archivos
   - Se guardan en configuración

3. **Usuario inicia el juego**
   - Click en "Iniciar Juego"
   - Se abre el launcher correspondiente
   - Timer detecta cuando GTA V inicia

4. **Inyección (Manual o Auto)**
   - Si auto-inject: inyecta automáticamente
   - Si manual: usuario hace click en "Inyectar DLLs"
   - Estados se actualizan a "Inyectado"

5. **Cierre**
   - Configuración se guarda automáticamente
   - DLLs y preferencias se mantienen para próxima sesión

---

**Nota**: Esta es una representación textual de la interfaz. La aplicación real tendrá:
- Animaciones suaves de parallax
- Transiciones de color en botones
- Efectos hover interactivos
- Diseño completamente responsive
