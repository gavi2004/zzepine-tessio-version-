# Usar imagen base de Node.js
FROM node:18-alpine

# Establecer directorio de trabajo
WORKDIR /app

# Copiar package.json y lock
COPY package*.json ./

# Instalar dependencias (ci si hay lock)
RUN npm ci || npm i

# Copiar el resto del proyecto
COPY . .

# Exponer puerto
EXPOSE 4569

# Iniciar
CMD ["node", "version-server.js"]
