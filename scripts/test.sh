#!/usr/bin/env bash
# Chạy unit test .NET, chỉ in dòng tín hiệu (Failed/Passed!/error/Total tests).
# Không đổ full log ra output mặc định — fail thì xem backend/TestResults/
# hoặc chạy lại `dotnet test` không có --verbosity quiet để xem chi tiết.

cd "$(dirname "$0")/.."

output=$(dotnet test backend/tests/UnitTests/UnitTests.csproj --verbosity quiet 2>&1)
status=$?

filtered=$(printf '%s\n' "$output" | grep -E 'Failed|Passed!|error|Total tests' || true)

if [ -n "$filtered" ]; then
  printf '%s\n' "$filtered"
else
  echo "Passed!"
fi

exit "$status"
