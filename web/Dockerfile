FROM node:26-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Unprivileged nginx: runs as a non-root user and listens on 8080.
FROM nginxinc/nginx-unprivileged:1.29-alpine
# The base image can lag behind Alpine security fixes; apply them at build time, then drop back to the non-root user.
USER root
RUN apk upgrade --no-cache
USER 101
# Only these variables are substituted in the templates, so nginx's own $variables stay untouched.
ENV NGINX_ENVSUBST_FILTER="^(API_KEY|API_UPSTREAM)$"
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY nginx/default.conf.template nginx/proxy-api.inc.template /etc/nginx/templates/
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
