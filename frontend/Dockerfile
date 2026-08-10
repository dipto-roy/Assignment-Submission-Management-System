# ---- Dependencies ----
FROM node:22-alpine AS deps
WORKDIR /app

# Copied on their own so the install layer is reused whenever only source files change.
COPY package.json package-lock.json ./
RUN npm ci

# ---- Build ----
FROM node:22-alpine AS build
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

# NEXT_PUBLIC_* values are inlined into the client bundle at build time, so the API URL has
# to be supplied here rather than as a runtime environment variable. It is the URL the
# *browser* uses, which is the published host port — not the compose service name.
ARG NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}

RUN npm run build

# ---- Runtime ----
FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN addgroup -S nodegroup && adduser -S nodeuser -G nodegroup

# The standalone output carries its own trimmed node_modules; static assets and public/
# are not included in it and must be copied alongside.
COPY --from=build --chown=nodeuser:nodegroup /app/.next/standalone ./
COPY --from=build --chown=nodeuser:nodegroup /app/.next/static ./.next/static
COPY --from=build --chown=nodeuser:nodegroup /app/public ./public

USER nodeuser
EXPOSE 3000

CMD ["node", "server.js"]
