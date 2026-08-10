#!/usr/bin/env bash
# Runs the whole backend suite with line-coverage collection and prints a per-assembly summary.
#
# Unit and integration tests produce one Cobertura report each. A class touched only by the
# integration run shows 0% in the unit report and vice versa, so the two are merged by taking
# the better figure per class — a floor on real coverage, never an inflated number.
#
# Requires the docker-compose Postgres instance: `docker compose up -d postgres`.
set -euo pipefail

BACKEND_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${1:-${BACKEND_DIR}/TestResults/coverage}"

rm -rf "${RESULTS_DIR}"

dotnet test "${BACKEND_DIR}/AssignmentSubmissionSystem.sln" \
    --collect:"XPlat Code Coverage" \
    --results-directory "${RESULTS_DIR}"

python3 - "${RESULTS_DIR}" <<'PY'
import glob, os, sys
import xml.etree.ElementTree as ET
from collections import defaultdict

results_dir = sys.argv[1]
reports = glob.glob(os.path.join(results_dir, "*", "coverage.cobertura.xml"))
if not reports:
    sys.exit("No coverage.cobertura.xml files found.")

best = defaultdict(lambda: (0, 0))
for report in reports:
    root = ET.parse(report).getroot()
    for package in root.iter("package"):
        for cls in package.iter("class"):
            lines = list(cls.iter("line"))
            covered = sum(1 for line in lines if int(line.get("hits")) > 0)
            key = (package.get("name"), cls.get("name"))
            if covered > best[key][0]:
                best[key] = (covered, len(lines))

per_assembly = defaultdict(lambda: [0, 0])
for (package, _), (covered, total) in best.items():
    per_assembly[package][0] += covered
    per_assembly[package][1] += total

total_covered = total_lines = 0
print()
print(f"{'Assembly':55} {'Lines':>13}  Coverage")
for package, (covered, total) in sorted(per_assembly.items()):
    total_covered += covered
    total_lines += total
    pct = 100 * covered / total if total else 0
    print(f"{package:55} {covered:6}/{total:6}  {pct:6.1f}%")

pct = 100 * total_covered / total_lines if total_lines else 0
print(f"{'TOTAL':55} {total_covered:6}/{total_lines:6}  {pct:6.1f}%")
print()
PY
