@echo off
title GTA V Injector - Version Server
echo.
echo =============================================
echo   GTA V INJECTOR - SERVIDOR DE VERSIONES
echo =============================================
echo.
echo Iniciando servidor de validacion de versiones...
echo.

REM Verificar si Node.js está instalado
node --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Node.js no esta instalado o no esta en el PATH
    echo.
    echo Por favor instala Node.js desde: https://nodejs.org/
    echo.
    pause
    exit /b 1
)

REM Verificar si existe package.json
if not exist "package.json" (
    echo [ERROR] package.json no encontrado
    echo Asegurate de ejecutar desde el directorio correcto
    echo.
    pause
    exit /b 1
)

REM Instalar dependencias si no existen
if not exist "node_modules" (
    echo Instalando dependencias...
    echo.
    npm install
    echo.
)

REM Iniciar servidor
echo Servidor iniciando en puerto 4569...
echo.
echo URLs disponibles:
echo  - Panel web: http://localhost:4569
echo  - API Version: http://localhost:4569/api/version
echo.
echo Presiona Ctrl+C para detener el servidor
echo.
node version-server.js

pause