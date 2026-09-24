# Stage 1: build the static SPA bundle
FROM node:24-alpine AS build

WORKDIR /app

# Install dependencies first for layer caching
COPY package.json package-lock.json ./

RUN npm ci

COPY . .

RUN npm run build

# Stage 2: serve the bundle and proxy API calls to the AstroLab API
FROM nginx:1.30-alpine AS runtime

# The nginx image renders /etc/nginx/templates/*.template into /etc/nginx/conf.d/ at start-up,
# substituting environment variables such as ASTROLAB_API_URL.
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

COPY --from=build /app/dist /usr/share/nginx/html

ENV ASTROLAB_API_URL=http://astrolab:8080

EXPOSE 80
