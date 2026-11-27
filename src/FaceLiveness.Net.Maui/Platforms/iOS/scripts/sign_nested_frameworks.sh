#!/bin/bash
# Re-sign nested Face Liveness frameworks with the app identity (Recognition MAUI pattern).
set -euo pipefail

APP_BUNDLE="${1:?app bundle path required}"
SIGN_IDENTITY="${2:?codesign identity required}"

if [ -z "${SIGN_IDENTITY}" ] || [ "${SIGN_IDENTITY}" = "-" ]; then
  echo "warning: codesign identity unset; skipping nested framework codesign" >&2
  exit 0
fi

sign_framework() {
  local fw="$1"
  if [ ! -d "${fw}" ]; then
    return 0
  fi
  echo "Signing $(basename "${fw}")"
  /usr/bin/codesign --remove-signature "${fw}" 2>/dev/null || true
  /usr/bin/codesign --force --sign "${SIGN_IDENTITY}" \
    --preserve-metadata=identifier,entitlements,flags \
    --timestamp=none \
    "${fw}"
}

FRAMEWORKS="${APP_BUNDLE}/Frameworks"
for name in \
  FaceLivenessBridge \
  FaceLivenessKit \
  facerecognitionsdk \
  FacePluginSDK \
  FacePPlugin \
  FaceDetect
do
  sign_framework "${FRAMEWORKS}/${name}.framework"
done
