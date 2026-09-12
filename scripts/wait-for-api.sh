#!/usr/bin/env bash
# Poll URL mỗi giây đến khi nhận HTTP 200 hoặc hết timeout.
# Cách dùng: scripts/wait-for-api.sh <url> <timeout_giay>

url="$1"
timeout="$2"

if [ -z "$url" ] || [ -z "$timeout" ]; then
  echo "Usage: wait-for-api.sh <url> <timeout_giay>" >&2
  exit 2
fi

elapsed=0
while [ "$elapsed" -lt "$timeout" ]; do
  code=$(curl -s -o /dev/null -w '%{http_code}' "$url" || true)
  if [ "$code" = "200" ]; then
    echo "ready"
    exit 0
  fi
  sleep 1
  elapsed=$((elapsed + 1))
done

echo "timeout after ${timeout}s"
exit 1
