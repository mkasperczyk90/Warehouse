# Warehouse admin panel (Vite React SPA, ADR-0004) — static build served by nginx.
#
# The backend is mocked at the network boundary with MSW (ADR-0006), so this image
# is a self-contained, runnable preview: the bundled worker answers the `fetch`
# calls with fixtures. Going live is turning the worker off, not rebuilding the image.
# Build context is this directory:  docker build -t warehouse-admin src/web/admin

# --- build stage ----------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

# Install deps first (cached unless the lockfile changes).
COPY package.json package-lock.json ./
RUN npm ci

# Build the SPA. `mock:init` generates public/mockServiceWorker.js (gitignored),
# which Vite then copies into dist so MSW can start in the served app.
# VITE_USE_MOCKS defaults to true (standalone, MSW-mocked preview). Build with
# --build-arg VITE_USE_MOCKS=false to turn MSW off and have the app call the real Gateway.
ARG VITE_USE_MOCKS=true
ENV VITE_USE_MOCKS=$VITE_USE_MOCKS
COPY . .
RUN npm run mock:init && npm run build

# --- runtime stage --------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime
# nginx.conf is a template: the image's entrypoint runs envsubst at startup, filling ${LISTEN_PORT}
# and ${GATEWAY_UPSTREAM}. Defaults suit the local standalone image; the AWS task overrides them so
# /api proxies to the Gateway in the same task.
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
ENV LISTEN_PORT=80 \
    GATEWAY_UPSTREAM=http://127.0.0.1:8081 \
    NGINX_ENVSUBST_FILTER="^(LISTEN_PORT|GATEWAY_UPSTREAM)$"
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
