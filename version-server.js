const express = require('express');
const fs = require('fs');
const path = require('path');
const app = express();
const PORT = process.env.PORT || 4569;

// Middleware para JSON y archivos estáticos
app.use(express.json());
app.use(express.static(path.join(__dirname, 'web-interface')));

// Middleware para CORS
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
    res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
    next();
});

// Cargar configuración desde archivo
const configPath = path.join(__dirname, 'config.json');
let config = {
    version: "1.0.0",
    allowedVersions: ["1.0.0"],
    adminKey: "admin123",
    updateTimestamp: new Date().toISOString()
};

// Leer configuración existente
try {
    if (fs.existsSync(configPath)) {
        config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
    }
} catch (error) {
    console.warn('⚠️  Error leyendo config.json, usando valores por defecto');
}

// Versión actual desde archivo o variable de entorno
let currentVersion = process.env.CURRENT_VERSION || config.version;

// Función para comparar versiones semánticas
function compareVersions(version1, version2) {
    const v1Parts = version1.split('.').map(Number);
    const v2Parts = version2.split('.').map(Number);
    
    for (let i = 0; i < Math.max(v1Parts.length, v2Parts.length); i++) {
        const v1 = v1Parts[i] || 0;
        const v2 = v2Parts[i] || 0;
        
        if (v1 > v2) return 1;
        if (v1 < v2) return -1;
    }
    return 0;
}

// Función para validar formato de versión
function isValidVersion(version) {
    return /^\d+\.\d+\.\d+$/.test(version);
}

// Función para guardar configuración
function saveConfig() {
    try {
        config.version = currentVersion;
        config.updateTimestamp = new Date().toISOString();
        fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
        return true;
    } catch (error) {
        console.error('❌ Error guardando configuración:', error);
        return false;
    }
}

// Ruta principal - Servir interfaz web
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'web-interface', 'index.html'));
});

// GET: Obtener versión actual
app.get('/api/version', (req, res) => {
    res.json({
        success: true,
        version: currentVersion,
        timestamp: new Date().toISOString()
    });
});

// POST: Validar versión del inyector
app.post('/api/validate', (req, res) => {
    const { version } = req.body;
    
    // Validar formato
    if (!version || !isValidVersion(version)) {
        return res.status(400).json({
            success: false,
            allowed: false,
            message: "Formato de versión inválido. Use formato x.y.z"
        });
    }
    
    const comparison = compareVersions(version, currentVersion);
    
    let result = {
        success: true,
        clientVersion: version,
        serverVersion: currentVersion,
        allowed: false,
        message: ""
    };
    
    if (comparison === 0) {
        result.allowed = true;
        result.message = "Versión válida. Acceso permitido.";
    } else if (comparison < 0) {
        result.message = "Outdated version detected.";
    } else {
        result.message = "Versión del cliente más nueva que la del servidor.";
    }
    
    res.json(result);
});

// PUT: Actualizar versión del servidor (admin)
app.put('/api/version', (req, res) => {
    const { version, adminKey } = req.body;
    
    // Validación básica de admin
    if (adminKey !== process.env.ADMIN_KEY && adminKey !== config.adminKey) {
        return res.status(401).json({
            success: false,
            message: "Clave de administrador inválida"
        });
    }
    
    if (!version || !isValidVersion(version)) {
        return res.status(400).json({
            success: false,
            message: "Formato de versión inválido"
        });
    }
    
    const oldVersion = currentVersion;
    currentVersion = version;
    
    // Guardar en archivo de configuración
    if (saveConfig()) {
        res.json({
            success: true,
            message: "Versión actualizada y guardada correctamente",
            oldVersion,
            newVersion: currentVersion,
            savedToFile: true
        });
    } else {
        res.json({
            success: true,
            message: "Versión actualizada en memoria (error guardando archivo)",
            oldVersion,
            newVersion: currentVersion,
            savedToFile: false
        });
    }
});

// Middleware de manejo de errores
app.use((err, req, res, next) => {
    console.error(err.stack);
    res.status(500).json({
        success: false,
        message: "Error interno del servidor"
    });
});

// Ruta 404
app.use('*', (req, res) => {
    res.status(404).json({
        success: false,
        message: "Endpoint no encontrado"
    });
});

// Iniciar servidor
app.listen(PORT, '0.0.0.0', () => {
    console.log(`🚀 Servidor de versiones iniciado en puerto ${PORT}`);
    console.log(`📋 Versión actual: ${currentVersion}`);
    console.log(`🌐 Panel web disponible en: http://0.0.0.0:${PORT}`);
    console.log(`🔗 Endpoints API disponibles:`);
    console.log(`   GET  /api/version - Obtener versión actual`);
    console.log(`   POST /api/validate - Validar versión del inyector`);
    console.log(`   PUT  /api/version - Actualizar versión (admin)`);
});

module.exports = app;