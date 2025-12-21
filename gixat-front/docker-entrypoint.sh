#!/bin/sh

# Generate env-config.js from environment variables
cat <<EOF > /usr/share/nginx/html/assets/env-config.js
window.__env = window.__env || {};
window.__env.apiUrl = '${API_URL:-http://localhost:8002}';
EOF

# Start nginx
exec nginx -g "daemon off;"
