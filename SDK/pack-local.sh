#!/usr/bin/env sh
set -e

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

config=${1:-Debug}

dotnet build Toolchain/Iona -c "$config" --nologo
dotnet build SDK/Iona.Build.Tasks -c "$config" --nologo

dotnet pack SDK/StandardLibrary/Builtins/Builtins.csproj -c "$config" -o local-feed --nologo
dotnet pack SDK/Iona.Sdk/Iona.Sdk.csproj -c "$config" -o local-feed --nologo
dotnet pack SDK/Iona.Templates/Iona.Templates.csproj -o local-feed --nologo

rm -rf "$HOME/.nuget/packages/iona.sdk/0.1.0" "$HOME/.nuget/packages/iona.standardlibrary/0.1.0"

echo "Local feed refreshed at $root/local-feed"
