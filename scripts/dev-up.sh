#!/usr/bin/env bash
# Bật stack Docker dev (api + web, kéo theo db qua depends_on) nếu chưa chạy sẵn.
# Khớp cả tên container do `docker compose` sinh ra (clinic-management-ai-db-1)
# lẫn container test thủ công (clinic-pg-test, xem mục 7 ghi chú dự án).

cd "$(dirname "$0")/.."

running=$(docker ps --format '{{.Names}}' | grep -Ei 'clinic-pg|clinic-management-ai-db' || true)

if [ -n "$running" ]; then
  echo "already running, skipping ($running)"
else
  docker compose up -d api web
fi
