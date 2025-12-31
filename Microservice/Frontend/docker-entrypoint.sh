#!/bin/bash

# Replace API URLs in the built files if API_URL environment variable is set
if [ -n "$API_URL" ]; then
    echo "Setting API URL to: $API_URL"
    # Replace various localhost URLs and IP addresses with the configured API_URL
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://localhost:5010/api|$API_URL|g" {} +
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://localhost:5000/api|$API_URL|g" {} +
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://localhost:5001/api|$API_URL|g" {} +
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://103.82.26.211:5010/api|$API_URL|g" {} +
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://103.82.26.211:5000/api|$API_URL|g" {} +
    find /usr/share/nginx/html -type f \( -name "*.js" -o -name "*.html" \) -exec sed -i "s|http://103.82.26.211:5001/api|$API_URL|g" {} +
    echo "API URL replacement completed"
fi

# Start nginx
exec nginx -g "daemon off;"

