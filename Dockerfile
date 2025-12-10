# Usar imagen base de Node.js
FROM node:18-alpine

# Establecer directorio de trabajo
WORKDIR /app

# Crear package.json
RUN echo '{"name":"gtav-injector-server","version":"1.0.7","main":"version-server.js","scripts":{"start":"node version-server.js"},"dependencies":{"express":"^4.18.2","cors":"^2.8.5"}}' > package.json

# Instalar dependencias
RUN npm install

# Copiar archivos necesarios
COPY version-server.js ./
COPY config.json ./

# Crear directorio y archivos de interfaz web
RUN mkdir -p web-interface && \
    echo '<!DOCTYPE html><html><head><title>GTAV Version Server</title></head><body><h1>GTAV Version Server</h1><p>Server running on port 4569</p><p>API: /api/version | /api/validate</p></body></html>' > web-interface/index.html

# Exponer puerto 4569
EXPOSE 4569

# Comando para iniciar el servidor
CMD ["node", "version-server.js"]