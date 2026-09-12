#!/usr/bin/env bash
# Rebuild + redeploy một service Docker Compose (mặc định: api).
# Dùng khi đã sửa code backend/frontend và muốn áp dụng lên container đang chạy
# mà không đổ full log build (xem ghi chú dự án mục 7 — nguyên tắc chạy lệnh).
#
# Cách dùng: ./scripts/redeploy.sh [service...]   (mặc định: api)
#   ./scripts/redeploy.sh          # rebuild + up lại api
#   ./scripts/redeploy.sh web      # rebuild + up lại web
#   ./scripts/redeploy.sh api web  # cả hai

cd "$(dirname "$0")/.."

services="${*:-api}"

log=$(docker compose up -d --build $services 2>&1)
status=$?

echo "$log" | grep -Ei 'error|fail|denied|cannot|Recreated|Started|Running' || echo "$log" | tail -n 5

exit $status
