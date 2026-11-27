# iOS frameworks

Six bundles belong here and **none of them are in git**. `python3 run_ios.py` downloads
`runtime.zip` + `bridge.zip` and slices the ios-arm64 `.framework` copies the binding links.

| Bundle | What it is | Source |
| ------ | ---------- | ------ |
| `FaceLivenessBridge.framework` | The Objective-C face of the native liveness screens | `bridge.zip` on the [FaceLivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS/releases/latest) |
| `FaceLivenessKit.framework` | The engine lifecycle the bridge calls | the same `bridge.zip` |
| `facerecognitionsdk.framework` | The engine wrapper | `runtime.zip` on the [FaceRecognition-LivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-iOS/releases/tag/latest) |
| `FacePluginSDK.framework` | Vendor runtime the wrapper links | the same `runtime.zip` |
| `FacePPlugin.framework` | Vendor runtime the wrapper links (from `FacePlugin.xcframework`) | the same `runtime.zip` |
| `FaceDetect.framework` | Vendor detector the wrapper links | the same `runtime.zip` |

Archives also drop the parent `.xcframework` trees; `fp_releases.py` always re-slices the
device frameworks the binding project references (same pattern as Face-Recognition MAUI).

A usable bridge must contain a real Mach-O (`FaceLivenessBridge.framework/FaceLivenessBridge`).
An Info.plist-only stub (from a failed `xcodebuild -create-xcframework` without
`-allow-internal-distribution`) causes MT158 at link time — delete `Frameworks/` and re-fetch
after a good `bridge.zip` is published.

## Why a bridge at all

The liveness screens in
[FaceLivenessDetection-iOS](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS) are
Swift, and .NET for iOS can only bind Objective-C. That repository therefore publishes
**FaceLivenessBridge**, a framework target whose only job is to put an `@objc` face on the same
screens — it compiles the very same source files the app does, so the two cannot drift.

<details>
<summary>Rebuilding the bridge yourself instead</summary>

Requires macOS with Xcode:

```bash
cd FaceLivenessDetection-iOS
./scripts/build_bridge.sh
cp -R build/xcframeworks/FaceLivenessBridge.xcframework \
      build/xcframeworks/FaceLivenessKit.xcframework \
      <this repo>/src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/
# then re-run python3 run_ios.py so fp_releases.py slices the device .framework copies
```
</details>

**Device only** (`ios-arm64`). The engine ships no simulator slice, which is the same reason the
app only runs on a physical phone.

`src/FaceLivenessBridge.Binding/` binds the bridge; the app's build fails with a named list if any
of the six is missing, so a missing runtime is reported before the compiler runs.
