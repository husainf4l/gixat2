# Build stage
FROM --platform=linux/amd64 node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM --platform=linux/amd64 nginx:alpine

# Copy custom nginx config
COPY nginx.conf /etc/nginx/nginx.conf

# Copy entrypoint script
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Copy built application from build stage
COPY --from=build /app/dist/gixat-front/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx with entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
