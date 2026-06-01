#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Upstream sync triage for Maka ERP — READ ONLY (no fetch-merge, no checkout).
#
# Lists upstream commits that are NOT in our HEAD and classifies each as:
#   SAFE   → touches only files we have NOT customized since the fork point.
#            These cherry-pick / `git checkout <commit> -- <file>` cleanly.
#   REVIEW → touches at least one file we changed → resolve manually.
#
# Heuristic: a file is "ours/customized" when it differs between the fork point
# (merge-base) and our HEAD. If an upstream commit only touches files outside
# that set, applying it cannot conflict with our work.
#
# Usage:  bash scripts/upstream-report.sh [remote] [branch]
#         (defaults: upstream main).  Run it from a PowerShell prompt via git-bash:
#         bash scripts/upstream-report.sh
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

REMOTE="${1:-upstream}"
BRANCH="${2:-main}"
REF="$REMOTE/$BRANCH"

echo "Fetching $REMOTE ..."
git fetch "$REMOTE" --quiet

BASE="$(git merge-base HEAD "$REF")"
echo "merge-base : $(git rev-parse --short "$BASE")"
echo "our HEAD   : $(git rev-parse --short HEAD)  ($(git branch --show-current))"
echo "upstream   : $(git rev-parse --short "$REF")  ($REF)"
echo

# Files WE changed since the fork point = our conflict surface.
declare -A CHANGED=()
while IFS= read -r f; do
  [ -n "$f" ] && CHANGED["$f"]=1
done < <(git diff --name-only "$BASE" HEAD)
echo "Files we changed since the fork point: ${#CHANGED[@]}"
echo

SAFE_LIST=()
REVIEW_LIST=()
total=0

# Newest first; skip merge commits (no useful single diff to cherry-pick).
while IFS= read -r c; do
  [ -z "$c" ] && continue
  total=$((total + 1))
  short="$(git rev-parse --short "$c")"
  subject="$(git show -s --format='%s' "$c")"
  conflicts=()
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    [ -n "${CHANGED[$f]:-}" ] && conflicts+=("$f")
  done < <(git show --name-only --pretty=format: "$c")
  if [ "${#conflicts[@]}" -eq 0 ]; then
    SAFE_LIST+=("$short  $subject")
  else
    REVIEW_LIST+=("$short  $subject")
    for cf in "${conflicts[@]}"; do REVIEW_LIST+=("           ↳ overlaps: $cf"); done
  fi
done < <(git rev-list --no-merges "$REF" "^HEAD")

echo "=== ✅ SAFE to bring (${#SAFE_LIST[@]} commits) — touch only files we have NOT customized ==="
if [ "${#SAFE_LIST[@]}" -gt 0 ]; then printf '  %s\n' "${SAFE_LIST[@]}"; else echo "  (none)"; fi
echo
echo "=== ⚠️  REVIEW manually — touch files we changed ==="
if [ "${#REVIEW_LIST[@]}" -gt 0 ]; then printf '  %s\n' "${REVIEW_LIST[@]}"; else echo "  (none)"; fi
echo
echo "Total new upstream commits (non-merge): $total"
echo
echo "Next: for a SAFE commit that only touches one FSH file, apply it surgically:"
echo "  git tag -f safety/pre-pick HEAD"
echo "  git checkout <commit> -- <path/to/file>     # only if our file == merge-base version"
echo "  dotnet build src/FSH.Starter.slnx           # (stop the API first to avoid file locks)"
echo "  git commit -m \"fix(<area>): <desc> (cherry-pick from FSH <commit>)\""
