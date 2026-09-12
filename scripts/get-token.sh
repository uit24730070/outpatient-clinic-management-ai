#!/usr/bin/env bash
# Đăng nhập và in ra accessToken (không in nguyên response) — thay cho việc
# tự viết curl + sed trích token mỗi lần test API có auth.
# Cách dùng: scripts/get-token.sh <username> <password> [base_url]

username="$1"
password="$2"
base_url="${3:-http://localhost:5006}"

if [ -z "$username" ] || [ -z "$password" ]; then
  echo "Usage: get-token.sh <username> <password> [base_url]" >&2
  exit 2
fi

response=$(curl -s -X POST "${base_url%/}/api/auth/login" \
  -H "Content-Type: application/json" \
  --data-binary "{\"username\":\"${username}\",\"password\":\"${password}\"}")

token=$(printf '%s' "$response" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

if [ -z "$token" ]; then
  echo "Không lấy được accessToken. Response: $response" >&2
  exit 1
fi

echo "$token"
