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
COPY . .
RUN npm run mock:init && npm run build

# --- runtime stage --------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
