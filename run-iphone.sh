#!/bin/zsh
# Builds LiftLife for a connected iPhone, installs it, and opens it.
# Usage (from the repo folder):  ./run-iphone.sh
# Needs: iPhone plugged in and unlocked, Developer Mode on, and a signing profile from Xcode
# (free Apple ID profiles last 7 days — see docs/PROGRESS_NOTES.md if signing fails).
set -e
cd "$(dirname "$0")/LiftLife"

# Find the first connected physical iPhone.
UDID=$(xcrun devicectl list devices 2>/dev/null | awk '/physical/ && /connected|available/ { for (i = 1; i <= NF; i++) if ($i ~ /^[0-9A-F]{8}-[0-9A-F]{16}$/) { print $i; exit } }')
if [ -z "$UDID" ]; then
  echo "No iPhone found. Plug it in, unlock it, and tap Trust if asked."
  exit 1
fi
echo "iPhone: $UDID"

echo "Building..."
dotnet build -f net10.0-ios -c Debug -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Development" -v:q -nologo

echo "Installing..."
xcrun devicectl device install app --device "$UDID" bin/Debug/net10.0-ios/ios-arm64/LiftLife.app >/dev/null

echo "Opening LiftLife..."
xcrun devicectl device process launch --device "$UDID" com.hilmanashraf.liftlife >/dev/null
echo "Done."
