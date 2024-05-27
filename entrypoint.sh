#!/bin/bash
IP=$(hostname -i)
export SERVER_IP=$IP
echo "Detected IP: $SERVER_IP"
exec "$@"