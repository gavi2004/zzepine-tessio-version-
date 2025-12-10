# Usar imagen base de Node.js
FROM node:18-alpine

# Establecer directorio de trabajo
WORKDIR /app

# Crear package.json básico
RUN echo '{"name":"gtav-injector-server","version":"1.0.7","main":"version-server.js","scripts":{"start":"node version-server.js"},"dependencies":{"express":"^4.18.2","cors":"^2.8.5"}}' > package.json

# Instalar dependencias
RUN npm install

# Copiar archivos del servidor
COPY version-server.js ./

# Crear config.json por defecto si no existe
RUN if [ ! -f config.json ]; then \
    echo '{"version":"1.0.7","allowedVersions":["1.0.7"],"adminKey":"admin123","updateTimestamp":"2025-12-10T03:18:00.000Z"}' > config.json; \
    fi

# Copiar config.json si existe (opcional)
COPY config.json* ./

# Crear directorio para la interfaz web
RUN mkdir -p web-interface

# Crear archivos básicos de la interfaz web si no existen
RUN echo '<!DOCTYPE html><html><head><title>GTAV Version Server</title></head><body><h1>GTAV Version Server</h1><p>Server is running on port 4569</p></body></html>' > web-interface/index.html

# Copiar interfaz web si existe (opcional)
COPY web-interface* ./web-interface/ || true

# Exponer puerto 4569
EXPOSE 4569

# Comando para iniciar el servidor
CMD ["npm", "start"]