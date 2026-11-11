# ==============================================================================
# Multi-stage Dockerfile for School Admission System Frontend (React 18 + Vite)
# ==============================================================================

# Stage 1: Build Production Static Bundle
FROM node:20-alpine AS build
WORKDIR /app

# Build-time argument for backend API URL
ARG VITE_API_BASE_URL=http://localhost:8080
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Copy package descriptors first for Docker layer caching
COPY package.json package-lock.json ./
RUN npm ci

# Copy application source files
COPY . .

# Compile TypeScript and bundle with Vite
RUN npm run build

# Stage 2: Serve Bundle with High-Performance Nginx Alpine
FROM nginx:alpine AS final

# Replace default Nginx configuration with custom SPA config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled static assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Expose port 3000
EXPOSE 3000

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://127.0.0.1:3000/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
