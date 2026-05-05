#!/usr/bin/env bash
#
# Bumps Semantic Version in Snappyup.Aspire.Hosting.Dragonfly.csproj, optionally packs and pushes to NuGet.
#
# Usage:
#   ./UpdateVersion.sh              # bump patch + optional pack/push prompts
#   ./UpdateVersion.sh 1.2.3      # set explicit version
#
# Environment:
#   NUGET_SOURCE   – feed URL (default: https://api.nuget.org/v3/index.json)
#   NUGET_API_KEY  – passed to `dotnet nuget push --api-key` when set
#
# Pack output is written to ./artifacts (same as CI workflows).

set -euo pipefail

new_version="${1:-}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
csproj_file="${repo_root}/src/Snappyup.Aspire.Hosting.Dragonfly/Snappyup.Aspire.Hosting.Dragonfly.csproj"

if [[ ! -f "$csproj_file" ]]; then
  echo "error: csproj not found: $csproj_file" >&2
  exit 1
fi

new_assembly_version="$(date +"%Y.%m.%d.%H%M")"

if [[ "$(uname -s)" == "Darwin" ]]; then
  sed_inplace() { sed -i '' "$@"; }
else
  sed_inplace() { sed -i "$@"; }
fi

echo "Building project..."
dotnet build "$csproj_file" -c Release

if [[ -z "$new_version" ]]; then
  current_version="$(grep -oE '<Version>[0-9]+\.[0-9]+\.[0-9]+</Version>' "$csproj_file" | head -n1 | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')"
  IFS='.' read -r major minor patch <<< "$current_version"
  patch=$((patch + 1))
  new_version="${major}.${minor}.${patch}"
fi

sed_inplace -E "s|<Version>[0-9]+\.[0-9]+\.[0-9]+</Version>|<Version>${new_version}</Version>|" "$csproj_file"
sed_inplace -E "s|<AssemblyVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+</AssemblyVersion>|<AssemblyVersion>${new_assembly_version}</AssemblyVersion>|" "$csproj_file"
sed_inplace -E "s|<FileVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+</FileVersion>|<FileVersion>${new_assembly_version}</FileVersion>|" "$csproj_file"

echo "Version updated to NuGet '${new_version}' and assembly/file '${new_assembly_version}'"

nupkg_name="Snappyup.Aspire.Hosting.Dragonfly.${new_version}.nupkg"
artifacts_dir="${repo_root}/artifacts"
nupkg_path="${artifacts_dir}/${nupkg_name}"

read -r -p "Do you want to run 'dotnet pack'? (y/n) " pack_response
if [[ "$pack_response" == "y" ]]; then
  echo "Running 'dotnet pack'..."
  mkdir -p "${artifacts_dir}"
  dotnet pack "$csproj_file" -c Release --output "${artifacts_dir}"
fi

read -r -p "Do you want to push the package to NuGet? (y/n) " push_response
if [[ "$push_response" == "y" ]]; then
  nuget_source="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

  if [[ ! -f "$nupkg_path" ]]; then
    echo "error: package not found: $nupkg_path (pack first with matching version)" >&2
    exit 1
  fi

  echo "Pushing ${nupkg_path} to '${nuget_source}' ..."

  push_args=(dotnet nuget push "$nupkg_path" --source "$nuget_source" --skip-duplicate)
  if [[ -n "${NUGET_API_KEY:-}" ]]; then
    push_args+=(--api-key "$NUGET_API_KEY")
  fi
  "${push_args[@]}"

  snupkg_path="${artifacts_dir}/Snappyup.Aspire.Hosting.Dragonfly.${new_version}.snupkg"
  if [[ -f "$snupkg_path" ]]; then
    echo "Pushing symbol package..."
    sym_args=(dotnet nuget push "$snupkg_path" --source "$nuget_source" --skip-duplicate)
    if [[ -n "${NUGET_API_KEY:-}" ]]; then
      sym_args+=(--api-key "$NUGET_API_KEY")
    fi
    "${sym_args[@]}"
  fi
fi
