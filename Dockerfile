# Usar imagen base de Node.js
FROM node:18-alpine

# Establecer directorio de trabajo
WORKDIR /app

# Copiar package.json si existe, sino crear uno básico
COPY package.json* ./

# Si no existe package.json, crear uno básico
RUN if [ ! -f package.json ]; then \
    echo '{"name":"gtav-injector-server","version":"1.0.7","main":"version-server.js","scripts":{"start":"node version-server.js"},"dependencies":{"express":"^4.18.2","cors":"^2.8.5"}}' > package.json; \
    fi

# Instalar dependencias
RUN npm install

# Copiar archivos del servidor
COPY version-server.js ./
COPY config.json ./

# Exponer puerto 4569
EXPOSE 4569

# Comando para iniciar el servidor
CMD ["npm", "start"]