# Warehouse operator terminal (Expo, React Native Web) — static web export served by nginx.
#
# app.json sets web.output = "single", so `expo export` produces a single-page web
# build in dist/. The terminal always calls the real Gateway: nginx proxies `/api`
# to ${GATEWAY_UPSTREAM} (the operator badges in to get a token).
# Build context is this directory:  docker build -t warehouse-terminal src/web/terminal

# --- build stage ----------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app
ENV EXPO_NO_TELEMETRY=1 CI=1

# Install deps first (cached unless the lockfile changes).
COPY package.json package-lock.json ./
RUN npm ci

# Build version (image tag), available to the app as process.env.EXPO_PUBLIC_APP_VERSION.
ARG VERSION=0.1.0
ENV EXPO_PUBLIC_APP_VERSION=$VERSION
COPY . .
RUN npm run build:web

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
