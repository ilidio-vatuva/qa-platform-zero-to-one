#!/usr/bin/env bash
# Run the API test suite for the configured shard.
#
# Inputs (env):
#   SHARD_INDEX  \u2014 0-based index of this shard
#   SHARD_TOTAL  \u2014 total number of shards
#   QA_ENVIRONMENT, QA_APIBASEURL, ... \u2014 standard EnvironmentConfig keys
#
# Test discovery + sharding is delegated to the test runner via a
# trait-based filter. Test classes are tagged with [Trait("Shard","N")]
# in a follow-up; for now this script demonstrates the shape of the
# entrypoint without pretending to do more than it does.

set -euo pipefail

: "${SHARD_INDEX:=0}"
: "${SHARD_TOTAL:=1}"

echo "[$(date -Is)] Running shard ${SHARD_INDEX}/${SHARD_TOTAL} against ${QA_ENVIRONMENT:-Local}"

# Skip UI tests in API shards; UI shards override this filter.
FILTER="Category!=UI"
if [[ "${SHARD_KIND:-api}" == "ui" ]]; then
  FILTER="Category=UI"
fi

dotnet test \
  --no-build \
  -c Release \
  --filter "$FILTER" \
  --logger "trx;LogFileName=shard-${SHARD_INDEX}.trx" \
  --results-directory /tests/results
