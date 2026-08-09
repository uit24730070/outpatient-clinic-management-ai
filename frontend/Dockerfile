# =============================================================================
# Frontend — build tĩnh (Vite) rồi phục vụ bằng nginx (kèm reverse proxy /api).
# Build context: thư mục frontend/
# =============================================================================

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Để trống → apiClient dùng URL tương đối, nginx reverse-proxy /api tới dịch vụ api.
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
