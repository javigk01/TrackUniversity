# ── Stage 1: build ───────────────────────────────────────────────────────────
FROM node:20-alpine AS builder

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install

COPY . .

# La variable PUBLIC_API_URL se inyecta en build time (ver docker-compose.yml)
ARG PUBLIC_API_URL=http://localhost:5000
ENV PUBLIC_API_URL=$PUBLIC_API_URL

RUN npm run build

# ── Stage 2: serve static files ──────────────────────────────────────────────
FROM nginx:1.27-alpine AS runner

COPY --from=builder /app/dist /usr/share/nginx/html

# Config de nginx: para SPA no es estrictamente necesario pero sí útil
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
