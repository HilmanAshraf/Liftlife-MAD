#!/bin/zsh
# Rebuilds LiftLife and reopens it on the iOS simulator (keeps the app's data).
# Usage (from the repo folder):  ./run-sim.sh
# Use it after every code change — a small change rebuilds in a few seconds.
set -e
cd "$(dirname "$0")/LiftLife"
APP_ID=com.hilmanashraf.liftlife

# Pick a simulator: one that's already running, else one that has LiftLife (and its data), else any iPhone.
SIM=$(xcrun simctl list devices available | awk '/iPhone/ && /Booted/ { match($0, /[0-9A-F-]{36}/); print substr($0, RSTART, RLENGTH); exit }')
if [ -z "$SIM" ]; then
  for id in $(xcrun simctl list devices available | awk '/iPhone/ { match($0, /[0-9A-F-]{36}/); print substr($0, RSTART, RLENGTH) }'); do
    if xcrun simctl get_app_container "$id" "$APP_ID" >/dev/null 2>&1; then SIM=$id; break; fi
  done
fi
[ -z "$SIM" ] && SIM=$(xcrun simctl list devices available | awk '/iPhone/ { match($0, /[0-9A-F-]{36}/); print substr($0, RSTART, RLENGTH); exit }')
if [ -z "$SIM" ]; then
  echo "No iPhone simulator found. Install one with: xcodebuild -downloadPlatform iOS"
  exit 1
fi

# Boot it (if needed) and show the simulator window.
xcrun simctl boot "$SIM" 2>/dev/null || true
open /Applications/Xcode.app/Contents/Applications/DeviceHub.app 2>/dev/null || open -a Simulator 2>/dev/null || true
echo "Simulator: $SIM"

echo "Building..."
dotnet build -f net10.0-ios -v:q -nologo

echo "Installing..."
xcrun simctl terminate "$SIM" "$APP_ID" 2>/dev/null || true
xcrun simctl install "$SIM" bin/Debug/net10.0-ios/iossimulator-arm64/LiftLife.app

echo "Opening LiftLife..."
xcrun simctl launch "$SIM" "$APP_ID" >/dev/null
echo "Done."
