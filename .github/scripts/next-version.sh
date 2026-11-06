#!/usr/bin/env bash
# Prints the version for the next release.
#
#   next-version.sh <change> [version]
#
# <change> is "Bug fixes", "New features" or "Breaking changes", which bump the patch, minor or major number
# of the latest vX.Y.Z tag (v0.0.0 when there is none). A [version] such as 1.4.0 or v1.4.0 is used as is
# instead, as long as it's newer than the latest tag.
set -euo pipefail

change=${1:?usage: next-version.sh <change> [version]}
requested=${2:-}

latest=$(git tag --list 'v*' | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | sort -V | tail -n 1 || true)
latest=${latest:-v0.0.0}
latest=${latest#v}

if [ -n "$requested" ]; then
  version=${requested#v}
  if ! [[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Version '$requested' isn't of the form 1.2.3." >&2
    exit 1
  fi
  if [ "$version" = "$latest" ] || [ "$(printf '%s\n%s\n' "$latest" "$version" | sort -V | tail -n 1)" != "$version" ]; then
    echo "Version $version isn't newer than the latest release, $latest." >&2
    exit 1
  fi
  echo "$version"
  exit 0
fi

IFS=. read -r major minor patch <<< "$latest"
case "$change" in
  "Bug fixes")        patch=$((10#$patch + 1)) ;;
  "New features")     minor=$((10#$minor + 1)); patch=0 ;;
  "Breaking changes") major=$((10#$major + 1)); minor=0; patch=0 ;;
  *)
    echo "Unknown change '$change'. Use 'Bug fixes', 'New features' or 'Breaking changes'." >&2
    exit 1
    ;;
esac
echo "$major.$minor.$patch"
