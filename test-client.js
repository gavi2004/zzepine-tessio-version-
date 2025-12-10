const http = require('http');

// Configuración
const SERVER_URL = 'http://localhost:3000';
const TEST_VERSION = '1.0.7'; // Versión a probar

console.log('🧪 CLIENTE DE PRUEBA - VALIDACIÓN DE VERSIONES');
console.log('='.repeat(50));
console.log();

// Función para hacer petición HTTP
function makeRequest(options, data = null) {
    return new Promise((resolve, reject) => {
        const req = http.request(options, (res) => {
            let body = '';
            res.on('data', chunk => body += chunk);
            res.on('end', () => {
                try {
                    resolve(JSON.parse(body));
                } catch (e) {
                    resolve({ error: 'Invalid JSON response', body });
                }
            });
        });
        
        req.on('error', reject);
        
        if (data) {
            req.write(JSON.stringify(data));
        }
        req.end();
    });
}

// Test 1: Obtener versión del servidor
async function testGetVersion() {
    console.log('📋 Test 1: Obtener versión del servidor');
    try {
        const options = {
            hostname: 'localhost',
            port: 3000,
            path: '/api/version',
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        };
        
        const result = await makeRequest(options);
        console.log('✅ Respuesta:', result);
        return result;
    } catch (error) {
        console.log('❌ Error:', error.message);
        return null;
    }
}

// Test 2: Validar versión
async function testValidateVersion() {
    console.log('\n🔍 Test 2: Validar versión del cliente');
    try {
        const options = {
            hostname: 'localhost',
            port: 3000,
            path: '/api/validate',
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        };
        
        const data = { version: TEST_VERSION };
        const result = await makeRequest(options);
        console.log('✅ Respuesta:', result);
        return result;
    } catch (error) {
        console.log('❌ Error:', error.message);
        return null;
    }
}

// Test 3: Probar diferentes versiones
async function testDifferentVersions() {
    console.log('\n🔄 Test 3: Probar diferentes versiones');
    
    const versions = ['1.0.5', '1.0.7', '1.0.8'];
    
    for (const version of versions) {
        console.log(`\n   Probando versión: ${version}`);
        try {
            const options = {
                hostname: 'localhost',
                port: 3000,
                path: '/api/validate',
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            };
            
            const data = { version };
            const result = await makeRequest(options);
            
            const status = result.allowed ? '✅ PERMITIDO' : '❌ RECHAZADO';
            console.log(`   ${status}: ${result.message}`);
        } catch (error) {
            console.log(`   ❌ Error: ${error.message}`);
        }
    }
}

// Ejecutar todos los tests
async function runAllTests() {
    console.log('Esperando que el servidor esté disponible...\n');
    
    // Intentar conectar varias veces
    let serverInfo = null;
    for (let i = 0; i < 5; i++) {
        serverInfo = await testGetVersion();
        if (serverInfo && serverInfo.success) break;
        
        console.log('⏳ Reintentando en 2 segundos...\n');
        await new Promise(resolve => setTimeout(resolve, 2000));
    }
    
    if (!serverInfo || !serverInfo.success) {
        console.log('❌ No se pudo conectar al servidor.');
        console.log('💡 Asegúrate de que el servidor esté ejecutándose con: node version-server.js');
        return;
    }
    
    await testValidateVersion();
    await testDifferentVersions();
    
    console.log('\n🏁 TESTS COMPLETADOS');
    console.log('='.repeat(50));
}

// Ejecutar
runAllTests().catch(console.error);