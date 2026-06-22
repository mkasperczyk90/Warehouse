# Warehouse operator terminal (Expo, React Native Web) — static web export served by nginx.
#
# app.json sets web.output = "single", so `expo export` produces a single-page web
# build in dist/. The backend is mocked with MSW (ADR-0006): the bundled worker
# answers the app's `fetch` calls with fixtures, so this image runs standalone.
# Build context is this directory:  docker build -t warehouse-terminal src/web/terminal

# --- build stage ----------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app
ENV EXPO_NO_TELEMETRY=1 CI=1

# Install deps first (cached unless the lockfile changes).
COPY package.json package-lock.json ./
RUN npm ci

# Export the web build. `mock:init` generates public/mockServiceWorker.js
# (gitignored); Expo copies the public/ dir into the export so MSW can start.
COPY . .
RUN npm run mock:init && npm run build:web

# --- runtime stage --------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
